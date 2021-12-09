using System;
using dl_cs.Booru;


string[] tags = new[] {"1girl", "solo"};
int count = 50;
string out_dir = "./out";
string alternateBaseUrl = "";
// string alternateBaseUrl = "https://gelbooru.com/";
Console.WriteLine("Downloading {0} images from an image board with tags {1} to {2}", count, string.Join(", ", tags), out_dir);

new SafeBooru(tags,count,out_dir,alternateBaseUrl).Dispose();

