#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    [Obsolete("Don't use yet", true)]
    public unsafe readonly struct Image3D : IDisposable
    {
        private const string Message_EmptyOrDisposed = "The image is empty or already disposed.";

        private readonly int _width;
        private readonly int _height;
        private readonly int _depth;
        private readonly IntPtr _ptr;

        public int Width => _width;
        public int Height => _height;
        public int Depth => _depth;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ptr == IntPtr.Zero;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use default constructor.", true)]
        public Image3D()
        {
            throw new NotSupportedException("Don't use default constructor.");
        }

        public Image3D(int width, int height, int depth)
        {
            if(width < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(width));
            }
            if(height < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(height));
            }
            if(depth < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(depth));
            }

            if(width == 0 || height == 0 || depth == 0) {
                this = default;
            }
            else {
                _ptr = Marshal.AllocHGlobal(width * height * depth);
            }

            _width = width;
            _height = height;
            _depth = depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColorByte* GetPtr()
        {
            if(IsEmpty) {
                ThrowHelper.ThrowInvalidOp(Message_EmptyOrDisposed);
            }
            return (ColorByte*)_ptr;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_ptr);
            Unsafe.AsRef(_width) = default;
            Unsafe.AsRef(_height) = default;
            Unsafe.AsRef(_depth) = default;
            Unsafe.AsRef(_ptr) = default;
        }
    }
}
