#nullable enable
using System;
using Elffy.Imaging;
using System.IO;

namespace ImageSample
{
    class Program
    {
        static void Main(string[] args)
        {
            using var file = File.OpenRead("haru.png");
            var image = PngParser.Parse(file);
        }
    }
}
