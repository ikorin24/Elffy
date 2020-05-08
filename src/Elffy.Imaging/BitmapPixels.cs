#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace Elffy.Imaging
{
    /// <summary><see cref="BitmapData"/> のヘルパー構造体</summary>
    public readonly struct BitmapPixels : IDisposable
    {
        private readonly Bitmap _bitmap;
        private readonly BitmapData _bitmapData;

        public int Stride => _bitmapData.Stride;
        public PixelFormat PixelFormat => _bitmapData.PixelFormat;
        public int Width => _bitmapData.Width;
        public int Height => _bitmapData.Height;
        public IntPtr Ptr => _bitmapData.Scan0;

        public BitmapPixels(Bitmap bitmap, ImageLockMode lockMode, PixelFormat format)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(Bitmap));
            _bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), lockMode, format);
        }

        public BitmapPixels(Bitmap bitmap, Rectangle rect, ImageLockMode lockMode, PixelFormat format)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(Bitmap));
            _bitmapData = bitmap.LockBits(rect, lockMode, format);
        }

        public unsafe T* GetPtr<T>() where T : unmanaged => (T*)_bitmapData.Scan0;

        /// <summary>バイト列を取得します。</summary>
        /// <returns>ピクセルのバイト列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> AsSpan()
        {
            var byteLen = Math.Abs(_bitmapData.Stride) * _bitmapData.Height;
            var ptr = (byte*)_bitmapData.Scan0;
            return new Span<byte>(ptr, byteLen);
        }

        /// <summary>バイト列を取得します。</summary>
        /// <param name="start">開始位置</param>
        /// <returns>ピクセルのバイト列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> AsSpan(int start)
        {
            var byteLen = Math.Abs(_bitmapData.Stride) * _bitmapData.Height;
            if((uint)start >= (uint)byteLen) { throw new ArgumentOutOfRangeException(nameof(start)); }
            var ptr = (byte*)_bitmapData.Scan0;
            return new Span<byte>(ptr + start, byteLen);
        }

        /// <summary>バイト列を取得します。</summary>
        /// <param name="start">開始位置</param>
        /// <param name="length">長さ</param>
        /// <returns>ピクセルのバイト列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> AsSpan(int start, int length)
        {
            var byteLen = Math.Abs(_bitmapData.Stride) * _bitmapData.Height;
            if((uint)start >= (uint)byteLen) { throw new ArgumentOutOfRangeException(nameof(start)); }
            if((uint)length > (uint)(byteLen - start)) { throw new ArgumentOutOfRangeException(nameof(length)); }
            var ptr = (byte*)_bitmapData.Scan0;
            return new Span<byte>(ptr + start, length);
        }

        public void Dispose()
        {
            // Do not dispose bitmap
            _bitmap.UnlockBits(_bitmapData);
        }
    }
}
