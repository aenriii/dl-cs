using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Threading.Tasks.Sources;

namespace dl_cs.Util
{
    public class ParallelDownloadPool : IDisposable
    {
        private List<DL> Pool;
        static HttpClient client = new();
        private static Task Holding = new TaskCompletionSource<object>().Task;
        private List<Tuple<string, string>> Queue = new();
        private Stopwatch timer;
        public ParallelDownloadPool(int MaxParallelism, Stopwatch timer = null)
        {
            if (timer != null) this.timer = timer;
            Pool = new();
        }

        public void AddToQueue(string uri, string filePath)
        {
            // if directory of filePath doesn't exist, create it
            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            Queue.Add(new Tuple<string, string>(uri, filePath));
        }

        public void Dispose()
        {
        }

        internal class DL
        {
            public string Uri { get; set; }
            public string FilePath { get; set; }
            public Task DoneMarker { get; internal set; }
            public DL (string uri, string filePath)
            {
                Uri = uri;
                FilePath = filePath;
                // ignorable task to make compiler happy
                DoneMarker = Holding;
                // Console.Write("\b".Times(70) + "\rDownloading: " + uri);
                doDl();
            }

            public void doDl()
            {
                Task.Run(() =>
                {
                    var responseResult= client.GetStreamAsync(Uri).Result;
                    using (var memStream = responseResult)
                    {
                        using (var fileStream =File.Create(FilePath))
                        {
                            memStream.CopyTo(fileStream);
                        }
                    }
                    DoneMarker = Task.CompletedTask;
                    // Console.Write("\b".Times(70) + "\rDownloaded: " + FilePath);
                });
                
            }
        }

        public async Task WaitToFinish()
        {
            // while pool count is above 8, wait for done
            // once done, add new to pool containing popped
            // value of Queue
            int done = 0;
            bool doBadTry = false;
            long currTime = 0;
            if (timer != null) currTime = timer.ElapsedMilliseconds;
            while (Queue.Count > 0){
                Console.Write("\b".Times(70) +"{0}/{1} ({2} in queue)", done, done+Queue.Count+Pool.Count, Pool.Count);
                if (timer != null) Console.Write("( {1} millis / {0} done  = {2} avg. TTD)", done, timer.ElapsedMilliseconds-currTime, Math.Round((double)(timer.ElapsedMilliseconds-currTime)/done, 2));
                while (Pool.Count >= 8)
                {
                    done += Pool.FindAll(x => x.DoneMarker.IsCompleted).Count();
                    Pool.RemoveAll(x => x.DoneMarker.IsCompleted);
                    

                }

                // add new dl to pool from top of queue, and remove from queue
                if (Queue.Count == 0) break;
                Queue.RemoveAll(x => x == null);
                if (Queue[0].Item1 == null || Queue[0].Item2 == null) {
                    if (doBadTry)
                    {
                        break;
                    }
                    doBadTry = true;
                    continue;
                }
                Pool.Add(new DL(Queue[0].Item1, Queue[0].Item2));
                Queue.RemoveAt(0);


            }
            // wait for all to finish
            while (Pool.Count > 0)
            {
                Console.Write("\b".Times(70) +"{0}/{1} ({2} in queue)", done, done+Queue.Count+Pool.Count, Pool.Count);
                    if (timer != null) Console.Write("( {1} millis / {0} done  = {2}ms avg. TTD)", done, timer.ElapsedMilliseconds-currTime, Math.Round((double)(timer.ElapsedMilliseconds-currTime)/done, 2));
                Pool.RemoveAll(x => x.DoneMarker.IsCompleted);
            }
            
            
            // while (Pool.Count > 0)
            // {
            //     await Task.WhenAny(Pool.Select(x => x.DoneMarker));
            //     Pool.RemoveAll(x => x.DoneMarker != Holding);
            // }
        }
    }
}