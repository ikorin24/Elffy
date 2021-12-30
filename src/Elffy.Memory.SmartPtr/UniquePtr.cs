#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Elffy
{
    [NonCopyable]
    [DebuggerDisplay("{Ptr,nq}")]
    public unsafe struct UniquePtr : IDisposable
    {
        private void* _ptr;
        private delegate*<void*, void> _onDisposed;

        public bool IsNull => _ptr == null;

        public IntPtr Ptr => (IntPtr)_ptr;

        private UniquePtr(void* ptr, delegate*<void*, void> onDisposed)
        {
            _ptr = ptr;
            _onDisposed = onDisposed;
        }

        public void Reset(void* ptr, delegate*<void*, void> onDisposed)
        {
            Dispose();
            _ptr = ptr;
            _onDisposed = onDisposed;
        }

        public void ResetMalloc(nuint size)
        {
            Dispose();
#if NET6_0_OR_GREATER
            _ptr = NativeMemory.Alloc(size);
            _onDisposed = &NativeMemory.Free;
#else
            _ptr = (void*)Marshal.AllocHGlobal((IntPtr)(ulong)size);
            _onDisposed = &Marshal_FreeHGlobal;
#endif
        }

#if NET6_0_OR_GREATER
        public void ResetCalloc(nuint size)
        {
            Dispose();
            _ptr = NativeMemory.AllocZeroed(size);
            _onDisposed = &NativeMemory.Free;
        }
#endif

        /// <summary>Same as std::unique_ptr::release()</summary>
        /// <returns></returns>
        public void* Release()
        {
            _onDisposed = null;
            var ptr = _ptr;
            _ptr = null;
            return ptr;
        }

        public UniquePtr Move()
        {
            var ptr = _ptr;
            var onDisposed = _onDisposed;
            _ptr = null;
            _onDisposed = null;
            return new UniquePtr(ptr, onDisposed);
        }

        public T* GetPtr<T>() where T : unmanaged => (T*)_ptr;

        public void Dispose()
        {
            var onDisposed = _onDisposed;
            if(onDisposed != null) {
                onDisposed(_ptr);
                _onDisposed = null;
                _ptr = null;
            }
        }

        public static UniquePtr Null() => default;

        public static UniquePtr Malloc(nuint size)
        {
#if NET6_0_OR_GREATER
            return new UniquePtr(NativeMemory.Alloc(size), &NativeMemory.Free);
#else
            return new UniquePtr((void*)Marshal.AllocHGlobal((IntPtr)(ulong)size), &Marshal_FreeHGlobal);
#endif
        }

#if NET6_0_OR_GREATER
        public static UniquePtr Calloc(nuint size) => new UniquePtr(NativeMemory.AllocZeroed(size), &NativeMemory.Free);
#endif

#if !NET6_0_OR_GREATER
        private static void Marshal_FreeHGlobal(void* p) => Marshal.FreeHGlobal((IntPtr)p);
#endif
    }
}
