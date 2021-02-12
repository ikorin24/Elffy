#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using Elffy.Effective.Unsafes;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    public static unsafe class IcoParser
    {
        public static Icon Parse(Stream stream)
        {
            UnsafeEx.SkipInitIfPossible(out ICONDIR icondir);   // 6 bytes
            stream.SafeRead(UnsafeEx.AsBytes(ref icondir));

            if(icondir.idCount < 0) {
                ThrowHelper.ThrowFormatException();
            }

            using var entries = new UnsafeRawArray<ICONDIRENTRY>(icondir.idCount);
            stream.SafeRead(entries.AsBytes());

            var icon = new Icon(icondir.idCount);
            try {
                var images = icon.GetImagesSpan();
                for(int i = 0; i < images.Length; i++) {
                    images[i] = ParseImage(stream, entries[i]);
                }
                return icon;
            }
            catch {
                icon.Dispose();
                throw;
            }
        }

        private static ImageRef ParseImage(Stream stream, in ICONDIRENTRY entry)
        {
            UnsafeEx.SkipInitIfPossible(out BITMAPINFOHEADER header);
            stream.SafeRead(UnsafeEx.AsBytes(ref header));

            var isPng = (entry.bWidth == 0 && entry.bHeight == 0) ||
                        new Span<byte>(&header, PngParser.PngSignature.Length).SequenceEqual(PngParser.PngSignature);
            if(isPng) {
                if((int)entry.dwBytesInRes < 0) {
                    throw new InvalidDataException();
                }
                using var pngData = new UnsafeRawArray<byte>((int)entry.dwBytesInRes);
                *(BITMAPINFOHEADER*)pngData.Ptr = header;
                stream.SafeRead(pngData.AsSpan().Slice(sizeof(BITMAPINFOHEADER)));
                using var pngStream = PointerStream.Create(pngData.Ptr.ToPointer(), pngData.Length);
                return PngParser.Parse(pngStream);
            }

            var hasPalette = header.biBitCount <= 16 || header.biClrUsed > 0;
            using var palette = hasPalette ? ReadToUnsafeRawArray<RGBQUAD>(stream, (int)header.biClrUsed) : UnsafeRawArray<RGBQUAD>.Empty;
            var height = Math.Abs(header.biHeight) / 2;
            var width = header.biWidth;

            //Determine the XOR array Size
            int xorMaskByteSize = checked(
                checked((((width * header.biBitCount) + 31) & ~31) >> 3)   // mask bytes per line
                * height);
            using var xorMaskData = xorMaskByteSize != 0 ? ReadToUnsafeRawArray<byte>(stream, xorMaskByteSize) : UnsafeRawArray<byte>.Empty;
            var xorMask = xorMaskData.AsSpan();

            //Determine the AND array size
            int andMaskByteSize = checked(
                checked((((header.biWidth) + 31) & ~31) >> 3)
                * height);
            using var andMaskData = andMaskByteSize != 0 ? ReadToUnsafeRawArray<byte>(stream, andMaskByteSize) : UnsafeRawArray<byte>.Empty;
            var andMask = andMaskData.AsSpan();

            //var pixels = new UnsafeRawArray<ColorByte>(width * height);
            var image = new ImageRef(width, height);
            try {
                var pixels = image.GetPixels();
                for(int d = 0; d < height; d++) {
                    var y = (header.biHeight < 0) ? d : height - d - 1;
                    var imod = y * (xorMask.Length / height) * 8 / header.biBitCount;
                    var mmod = y * (andMask.Length / height) * 8;
                    var rowOffset = d * width;
                    for(int x = 0; x < width; x++) {
                        var index = rowOffset + x;
                        ref var pixel = ref pixels[index];
                        var value = BitArrayOperation.GetBitsValue(xorMask, imod + x, header.biBitCount);
                        var color = hasPalette ? ((RGBQUAD*)palette.Ptr) + value : (RGBQUAD*)&value;
                        pixel.R = color->r;
                        pixel.G = color->g;
                        pixel.B = color->b;
                        pixel.A = header.biBitCount == 32 ?
                            color->reserved :
                            (byte)~(BitArrayOperation.GetBit(andMask, mmod + x) - 1);  // 0 or 255
                    }
                }
                return image;
            }
            catch {
                image.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UnsafeRawArray<T> ReadToUnsafeRawArray<T>(Stream stream, int count) where T : unmanaged
        {
            var array = new UnsafeRawArray<T>(count);
            try {
                stream.SafeRead(array.AsBytes());
                return array;
            }
            catch {
                array.Dispose();
                throw;
            }
        }
    }
}
