#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using OpenToolkit.Graphics.OpenGL;
using UnmanageUtility;
using Elffy.Imaging;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Elffy.Effective;

namespace Elffy
{
    public static class ScreenCapture
    {
        public static Bitmap Capture(IHostScreen screen)
        {
            var rect = new Rectangle(0, 0, screen.ClientSize.X, screen.ClientSize.Y);
            var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppRgb);
            using(var pixels = bitmap.GetPixels(ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb)) {
                Capture(screen, rect, pixels.AsSpan());
            }
            return bitmap;
        }

        public static unsafe Span<(byte B, byte G, byte R, byte A)> Capture(IHostScreen screen,
                                                                           in Rectangle rect,
                                                                           Span<byte> buffer)
        {
            // TODO: OpenGL のコンテキストが複数ある場合はダメなのでは？
            //       現在実行中のコンテキストの RenderBuffer の表面からピクセルを取得する。

            const TKPixelFormat Format = TKPixelFormat.Bgra;    // pixel format for return
            const PixelType PixType = PixelType.UnsignedByte;   // color format in a pixel
            const int BytesPerPixel = 4;                        // 4 bytes of BGRA

            if(screen is null) { ThrowNullArg(); }

            var screenSize = screen!.ClientSize;
            if((uint)rect.X >= (uint)screenSize.X) { ThrowInvalidRect("rect X"); }
            if((uint)rect.Y >= (uint)screenSize.Y) { ThrowInvalidRect("rect Y"); }
            if((uint)rect.Width > (uint)(screenSize.X - rect.X)) { ThrowInvalidRect("rect Width"); }
            if((uint)rect.Height > (uint)(screenSize.Y - rect.Y)) { ThrowInvalidRect("rect Height"); }


            var byteSize = rect.Width * rect.Height * BytesPerPixel;
            var span = buffer.Slice(0, byteSize);               // An exception is thrown if buffer is too short.
            fixed(byte* ptr = span) {
                GL.ReadBuffer(ReadBufferMode.Front);
                GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, Format, PixType, (IntPtr)ptr);
            }
            ReverseAxis(span, rect.Width, rect.Height);

            return span.MarshalCast<byte, (byte B, byte G, byte R, byte A)>();

            // local func
            static void ThrowNullArg() => throw new ArgumentNullException(nameof(screen));
            static void ThrowInvalidRect(string msg) => throw new ArgumentException(msg);
        }

        private static void ReverseAxis(Span<byte> span, int width, int height)
        {
            // Origin is left-bottom coordinate in 2D OpenGL.
            // Convert pixels into left-top based image.

            ref var pixels = ref Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(span));
            var half = height / 2;
            using var pooledArray = new PooledArray<int>(width);
            var tmp = pooledArray.AsSpan(0, width);
            for(int i = 0; i < half; i++) {
                ref var p1 = ref Unsafe.Add(ref pixels, i * width);
                ref var p2 = ref Unsafe.Add(ref pixels, (height - 1 - i) * width);
                var line1 = MemoryMarshal.CreateSpan(ref p1, width);
                var line2 = MemoryMarshal.CreateSpan(ref p2, width);
                line1.CopyTo(tmp);
                line2.CopyTo(line1);
                tmp.CopyTo(line2);
            }
        }
    }
}
