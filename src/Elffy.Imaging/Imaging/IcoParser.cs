#nullable enable

#if !NETCOREAPP3_1
#define CAN_SKIP_LOCALS_INIT
#endif

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    public static unsafe class IcoParser
    {
        private static RawImage ParseImage(Stream stream, in ICONDIRENTRY entry)
        {
#if CAN_SKIP_LOCALS_INIT
            Unsafe.SkipInit(out BITMAPINFOHEADER header);
#else
            BITMAPINFOHEADER header = default;
#endif
            ReadStruct(stream, &header);

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
            using var xorMask = xorMaskByteSize != 0 ? ReadToUnsafeRawArray<byte>(stream, xorMaskByteSize) : UnsafeRawArray<byte>.Empty;

            //Determine the AND array size
            int andMaskByteSize = checked(
                checked((((header.biWidth) + 31) & ~31) >> 3)
                * height);
            using var andMask = andMaskByteSize != 0 ? ReadToUnsafeRawArray<byte>(stream, andMaskByteSize) : UnsafeRawArray<byte>.Empty;

            var pixels = new UnsafeRawArray<ColorByte>(width * height);
            try {
                for(int d = 0; d < height; d++) {
                    var y = (header.biHeight < 0) ? d : height - d - 1;
                    var imod = y * (xorMask.Length / height) * 8 / header.biBitCount;
                    var mmod = y * (andMask.Length / height) * 8;
                    var rowOffset = d * width;
                    for(int x = 0; x < width; x++) {
                        var index = rowOffset + x;
                        ref var pixel = ref pixels[index];
                        var value = GetValueWithBitCount(xorMask, imod + x, header.biBitCount);
                        var color = hasPalette ? ((RGBQUAD*)palette.Ptr) + value : (RGBQUAD*)&value;
                        pixel.R = color->r;
                        pixel.G = color->g;
                        pixel.B = color->b;
                        pixel.A = header.biBitCount == 32 ?
                            color->reserved :
                            (byte)~(GetBitAt(andMask, mmod + x) - 1);  // 0 or 255
                    }
                }
                return new RawImage(width, height, pixels.Ptr);
            }
            catch {
                pixels.Dispose();
                throw;
            }
        }

        public static UnsafeRawArray<RawImage> Parse(Stream stream)
        {
#if CAN_SKIP_LOCALS_INIT
            Unsafe.SkipInit(out ICONDIR icondir);   // 6 bytes
#else
            ICONDIR icondir = default;
#endif
            ReadStruct(stream, &icondir);

            Span<ICONDIRENTRY> entries = stackalloc ICONDIRENTRY[icondir.idCount];         // 16 * n bytes
            ReadBytes(stream, MemoryMarshal.AsBytes(entries));

            var images = new UnsafeRawArray<RawImage>(icondir.idCount, true);
            try {
                for(int i = 0; i < icondir.idCount; i++) {
                    images[i] = ParseImage(stream, entries[i]);
                }
                return images;
            }
            catch {
                for(int i = 0; i < images.Length; i++) {
                    Marshal.FreeHGlobal((IntPtr)images[i].GetPtr());
                }
                images.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadBytes(Stream stream, Span<byte> span)
        {
            if(stream.Read(span) != span.Length) {
                ThrowEOS();
                static void ThrowEOS() => throw new EndOfStreamException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadStruct<T>(Stream stream, T* buf) where T : unmanaged
        {
            ReadBytes(stream, MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(buf), sizeof(T)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UnsafeRawArray<T> ReadToUnsafeRawArray<T>(Stream stream, int count) where T : unmanaged
        {
            var array = new UnsafeRawArray<T>(count);
            try {
                ReadBytes(stream, array.AsBytes());
                return array;
            }
            catch {
                array.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetValueWithBitCount(UnsafeRawArray<byte> array, int index, int bitCount)
        {
            return bitCount switch
            {
                32 => array[index << 2] | (array[(index << 2) + 1] << 8) | (array[(index << 2) + 2] << 16) | (array[(index << 2) + 3] << 24),
                8 => array[index],
                24 => array[index * 3] | (array[index * 3 + 1] << 8) | (array[index * 3 + 2] << 16),
                1 => (array[index >> 3] >> (7 - (index & 7))) & 1,            // (array[index / 8] >> ((7 - index % 8) * 1)) & 0b1
                2 => (array[index >> 2] >> ((3 - (index & 3)) << 1)) & 3,     // (array[index / 4] >> ((3 - index % 4) * 2)) & 0b11
                4 => (array[index >> 1] >> ((1 - (index & 1)) << 2)) & 15,    // (array[index / 2] >> ((1 - index % 2) * 4)) & 0b1111
                16 => array[index << 1] | (array[(index << 1) + 1] << 8),
                _ => 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBitAt(UnsafeRawArray<byte> array, int index)
        {
            return ((array[index >> 3] >> (7 - (index & 7))) & 1);
        }
    }
}
