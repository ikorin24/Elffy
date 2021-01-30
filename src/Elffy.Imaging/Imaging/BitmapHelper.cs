#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Elffy.Imaging
{
    public static class BitmapHelper
    {
        public static Bitmap StreamToBitmap(Stream stream, ReadOnlySpan<char> fileExtension) 
            => StreamToBitmap(stream, GetTypeFromExt(fileExtension));

        public static Bitmap StreamToBitmap(Stream stream, BitmapType type)
        {
            switch(type) {
                case BitmapType.Png:
                case BitmapType.Jpg:
                case BitmapType.Bmp:
                    return new Bitmap(stream);
                case BitmapType.Tga:
                    return TgaParser.Parse(stream);
                default:
                    throw new ArgumentException($"Unknown type : {type}");
            }
        }

        public static BitmapType GetTypeFromExt(ReadOnlySpan<char> ext)
        {
            static bool StringEquals(ReadOnlySpan<char> left, string right) 
                => left.Equals(right.AsSpan(), StringComparison.OrdinalIgnoreCase);

            if(StringEquals(ext, ".png")) {
                return BitmapType.Png;
            }
            else if(StringEquals(ext, ".jpg") || StringEquals(ext, ".jpeg")) {
                return BitmapType.Jpg;
            }
            else if(StringEquals(ext, ".tga")) {
                return BitmapType.Tga;
            }
            else if(StringEquals(ext, ".bmp")) {
                return BitmapType.Bmp;
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
