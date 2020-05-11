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

        private readonly bool IsDisposed => (_bitmap is null);

        public int Stride => !IsDisposed ? _bitmapData.Stride 
                                         : throw DisposedException();
        public PixelFormat PixelFormat => !IsDisposed ? _bitmapData.PixelFormat 
                                                      : throw DisposedException();
        public int Width => !IsDisposed ? _bitmapData.Width 
                                        : throw DisposedException();
        public int Height => !IsDisposed ? _bitmapData.Height 
                                         : throw DisposedException();
        public IntPtr Ptr => !IsDisposed ? _bitmapData.Scan0 
                                         : throw DisposedException();

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

        public unsafe T* GetPtr<T>() where T : unmanaged 
            => !IsDisposed ? (T*)_bitmapData.Scan0 : throw DisposedException();

        /// <summary>バイト列を取得します。</summary>
        /// <returns>ピクセルのバイト列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> AsSpan()
        {
            if(IsDisposed) { throw DisposedException(); }
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
            if(IsDisposed) { throw DisposedException(); }
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
            if(IsDisposed) { throw DisposedException(); }
            var byteLen = Math.Abs(_bitmapData.Stride) * _bitmapData.Height;
            if((uint)start >= (uint)byteLen) { throw new ArgumentOutOfRangeException(nameof(start)); }
            if((uint)length > (uint)(byteLen - start)) { throw new ArgumentOutOfRangeException(nameof(length)); }
            var ptr = (byte*)_bitmapData.Scan0;
            return new Span<byte>(ptr + start, length);
        }

        public void Dispose()
        {
            // Do not dispose bitmap
            if(!IsDisposed) {
                _bitmap.UnlockBits(_bitmapData);
                Unsafe.AsRef(_bitmap) = null!;
                Unsafe.AsRef(_bitmapData) = null!;
            }
        }

        private ObjectDisposedException DisposedException() => new ObjectDisposedException(nameof(BitmapPixels));
    }
}
