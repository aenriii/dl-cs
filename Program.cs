using System;
using System.Diagnostics;
using dl_cs.Booru;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
string[] tags = new[] {"tail", "rating:safe"};
int count = 300;
string out_dir = "./out";
// string alternateBaseUrl = "";
string alternateBaseUrl = "https://gelbooru.com/";
Console.WriteLine("Downloading {0} images from an image board with tags {1} to {2}", count, string.Join(", ", tags), out_dir);
Stopwatch s = new();
int count_files_old = 0;
if (Directory.Exists(out_dir))
{
    count_files_old = Directory.GetFiles(out_dir).Length;
}

new SafeBooru(tags,count,out_dir,alternateBaseUrl, s).Dispose();

Console.WriteLine("");
Console.WriteLine("Getting benchmarking results...");
Task.Delay(2000).GetAwaiter().GetResult();
int count_files_new = Directory.GetFiles(out_dir).Length - count_files_old;
DirectoryInfo di = new DirectoryInfo(out_dir);
double size = di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length) / (1024*1024.0);

Console.WriteLine($"Downloaded {count_files_new} files in {s.ElapsedMilliseconds/1000.0}sec, meaning, including time to get data, each image took on average {Math.Round((s.ElapsedMilliseconds/1.0)/count_files_new, 2)}ms");
Console.WriteLine($"Total size of folder is {Math.Round(size, 2)}mb. With a time of {s.ElapsedMilliseconds/1000.0}s, this means that the speed of download was {Math.Round(size/(s.ElapsedMilliseconds/1000.0), 2)}MB/s, or {Math.Round(size/(s.ElapsedMilliseconds/1000.0)/8, 2)}MB/s per working pool.");