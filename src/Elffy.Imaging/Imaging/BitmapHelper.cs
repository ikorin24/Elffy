#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Elffy.Imaging
{
    public static class BitmapHelper
    {
        public static Image StreamToImage(Stream stream, ReadOnlySpan<char> fileExtension)
        {
            return StreamToImage(stream, GetTypeFromExt(fileExtension));
        }

        public static Image StreamToImage(Stream stream, ImageType type)
        {
            return type switch
            {
                ImageType.Png => PngParser.Parse(stream),
                ImageType.Tga => TgaParser.Parse(stream),
                _ => throw new NotSupportedException($"Not supported type : {type}"),
            };
        }


        //public static Bitmap StreamToBitmap(Stream stream, ReadOnlySpan<char> fileExtension) 
        //    => StreamToBitmap(stream, GetTypeFromExt(fileExtension));

        //public static Bitmap StreamToBitmap(Stream stream, BitmapType type)
        //{
        //    switch(type) {
        //        case BitmapType.Png:
        //        case BitmapType.Jpg:
        //        case BitmapType.Bmp:
        //            return new Bitmap(stream);
        //        case BitmapType.Tga:
        //            return TgaParser.Parse(stream);
        //        default:
        //            throw new ArgumentException($"Unknown type : {type}");
        //    }
        //}

        public static ImageType GetTypeFromExt(ReadOnlySpan<char> ext)
        {
            static bool StringEquals(ReadOnlySpan<char> left, string right) 
                => left.Equals(right.AsSpan(), StringComparison.OrdinalIgnoreCase);

            if(StringEquals(ext, ".png")) {
                return ImageType.Png;
            }
            else if(StringEquals(ext, ".jpg") || StringEquals(ext, ".jpeg")) {
                return ImageType.Jpg;
            }
            else if(StringEquals(ext, ".tga")) {
                return ImageType.Tga;
            }
            else if(StringEquals(ext, ".bmp")) {
                return ImageType.Bmp;
            }
            else {
                throw new NotSupportedException($"Not supported extension. {ext.ToString()}");
            }
        }

        public static BitmapPixels GetPixels(this Bitmap bitmap, ImageLockMode lockMode, PixelFormat format)
        {
            return new BitmapPixels(bitmap, lockMode, format);
        }
    }
}
