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
            using var file = File.OpenRead("haru_8.png");
            using var image = PngParser.Parse(file);

            // TODO: for debug
            {
                var b = new System.Drawing.Bitmap(image.Width, image.Height);
                for(int y = 0; y < image.Height; y++) {
                    for(int x = 0; x < image.Width; x++) {
                        var c = image[x, y];
                        b.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B));
                    }
                }
                b.Save("hoge.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
