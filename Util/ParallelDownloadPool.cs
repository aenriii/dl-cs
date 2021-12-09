using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace dl_cs.Util
{
    public class ParallelDownloadPool : IDisposable
    {
        private List<DL> Pool;
        private int index;
        static HttpClient client = new();
        private static Task Holding = new TaskCompletionSource<object>().Task;
        private List<Tuple<string, string>> Queue = new();
        public ParallelDownloadPool(int MaxParallelism)
        {
            Pool = new();
            
            index = 0;
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
                Console.Write("\rDownloading: " + uri);
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
                    Console.Write("\rDownloaded: " + FilePath);
                });
                
            }
        }

        public async Task WaitToFinish()
        {
            // while pool count is above 8, wait for done
            // once done, add new to pool containing popped
            // value of Queue

            while (Queue.Count > 0){
                while (Pool.Count >= 8)
                {
                    Pool.RemoveAll(x => x.DoneMarker.IsCompleted);
                }
                // add new dl to pool from top of queue, and remove from queue
                Pool.Add(new DL(Queue[0].Item1, Queue[0].Item2));
                Queue.RemoveAt(0);

            }
            // wait for all to finish
            while (Pool.Count > 0)
            {
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