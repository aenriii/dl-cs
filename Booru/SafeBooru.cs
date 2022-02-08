using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics;

using dl_cs.Util;

namespace dl_cs.Booru
{
    public class SafeBooru : IDisposable
    {
        private HttpClient Client = new();
        private XmlReader Reader = XmlReader.Create(new MemoryStream(new byte[]{0}));
        private ParallelDownloadPool Pool = new(8);
        static XmlReaderSettings _readerSettings= new XmlReaderSettings()
        {
          
            DtdProcessing = DtdProcessing.Parse,
            Async = true
        };
        public SafeBooru(string[] args, int count, string out_dir, string alternateBaseUrl = "", Stopwatch timer = null)
        {
            if (timer != null) timer.Start();
            Client = new();
            Pool = new(8, timer);
            if (alternateBaseUrl == "")
            {
                alternateBaseUrl = "https://safebooru.org/";
            }
            

            var lst = new SBC(this, alternateBaseUrl).GetPosts(String.Join("+", args), count);
            Parallel.ForEach( lst, post =>
                {
                    // Console.WriteLine($"\rDownloading {post .Split("/")[post.Split("/").Length - 1]}");
                    string fileUrl = post;
                    // Console.WriteLine(post);
                    try {string filePath = Path.Combine(out_dir, Path.GetFileName(fileUrl));
                    if (File.Exists(filePath))
                        return;
                    Pool.AddToQueue(fileUrl, filePath);}
                    catch {}
                }
            );
            
            Pool.WaitToFinish().GetAwaiter().GetResult();
            if (timer != null) timer.Stop();
        }

        ~SafeBooru()
        {
            Dispose();
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                Client?.Dispose();
                Reader?.Dispose();
                Pool?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal class SBC
        {
            private SafeBooru parent;
            private string base_url;
            public SBC(SafeBooru parent, string base_url = "https://safebooru.org/")

            {
                this.parent = parent;
                this.base_url = base_url;
            }
            public List<string> GetPosts(string tags, int count)
            {
                var posts = new List<XmlReader>();
                var url = $"{base_url}index.php?page=dapi&s=post&q=index&tags={tags}&limit={count}";
                var response = parent.Client.GetAsync(url).Result;
                var stream = response.Content.ReadAsStreamAsync().Result;

                parent.Reader = XmlReader.Create(stream.Duplicate(), _readerSettings);
                var nodeTypeAttrs = parent.Reader.GetAttrsOfTypeNode("post", "file_url");

                if (nodeTypeAttrs.FindAll(x => x != null).Count() == 0)
                {
                    parent.Reader = XmlReader.Create(stream.Duplicate(), _readerSettings);
                    nodeTypeAttrs = parent.Reader.GetValAllNodeType("file_url");
                }
                return nodeTypeAttrs;
            }
        }
    }
}