using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
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
          
            DtdProcessing = DtdProcessing.Parse
        };
        public SafeBooru(string[] args, int count, string out_dir, string alternateBaseUrl = "")
        {
            if (count > 1000)
            {
                for (int i = count; i > 0; i-=0)
                {
                    if (i > 1000)
                    {
                        new SafeBooru(args, 1000, out_dir, alternateBaseUrl).Dispose();
                        i -= 1000;
                    }
                    else
                    {
                        new SafeBooru(args, i, out_dir, alternateBaseUrl).Dispose();
                        i -= i;
                    }
                }
                return;
            }
            Client = new();
            Pool = new(8);
            if (alternateBaseUrl == "")
            {
                alternateBaseUrl = "https://safebooru.org/";
            }
            var lst = new SBC(this, alternateBaseUrl).GetPosts(String.Join("+", args), count);
            Parallel.ForEach( lst, post =>
                {
                    // Console.WriteLine($"\rDownloading {post.Split("/")[post.Split("/").Length - 1]}");
                    string fileUrl = post;
                    string filePath = Path.Combine(out_dir, Path.GetFileName(fileUrl));
                    if (File.Exists(filePath))
                        return;
                    Pool.AddToQueue(fileUrl, filePath);
                }
            );
            
            Pool.WaitToFinish().GetAwaiter().GetResult();

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
                parent.Reader = XmlReader.Create(stream, _readerSettings);
                return parent.Reader.GetAttrsOfTypeNode("post", "file_url");
            }
        }
    }
}