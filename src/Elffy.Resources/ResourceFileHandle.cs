#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public readonly struct ResourceFileHandle : IDisposable, IEquatable<ResourceFileHandle>
    {
        private readonly SafeFileHandle? _handle;
        private readonly long _offset;
        private readonly long _size;

        public static ResourceFileHandle None => default;

        public long FileSize => _size;

        [Obsolete("Don't use default constructor", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ResourceFileHandle() => throw new NotSupportedException("Don't use default constructor");

        internal ResourceFileHandle(SafeFileHandle handle, long offset, long resourceFileSize)
        {
            _handle = handle;
            _offset = offset;
            _size = resourceFileSize;
        }

        public int Read(Span<byte> buffer, long fileOffset)
        {
            if((ulong)fileOffset >= (ulong)_size) {
                ThrowArgOutOfRange();
                static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(fileOffset));
            }
            var readBuf = buffer.Slice(0, (int)Math.Min(buffer.Length, _size));
            var handle = ValidateHandle();
            var actualOffset = _offset + fileOffset;
            return RandomAccess.Read(handle, readBuf, actualOffset);
        }

        public unsafe long Read(IntPtr buffer, nuint bufferLength, long fileOffset) => Read(buffer.ToPointer(), bufferLength, fileOffset);

        public unsafe long Read(void* buffer, nuint bufferLength, long fileOffset)
        {
            if(bufferLength <= int.MaxValue) {
                return Read(new Span<byte>(buffer, (int)bufferLength), fileOffset);
            }
            else {
                if((ulong)fileOffset >= (ulong)_size) {
                    ThrowArgOutOfRange();
                    static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(fileOffset));
                }
                var readBufLen = Math.Min((ulong)bufferLength, (ulong)_size);

                var handle = ValidateHandle();
                var memCount = readBufLen >> 31;
                Debug.Assert(memCount > 0);
                var memoryArray = new Memory<byte>[memCount];
                for(int i = 0; i < memoryArray.Length; i++) {
                    ulong offset = ((ulong)i << 31);
                    byte* ptr = (byte*)buffer + offset;
                    int len = (int)(readBufLen - offset);
                    var memoryManager = new PointerMemoryManager(ptr, len);
                    memoryArray[i] = memoryManager.Memory;
                }
                return RandomAccess.Read(handle, memoryArray, fileOffset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SafeFileHandle ValidateHandle() => _handle ?? throw new InvalidOperationException("File handle is null");

        public void Dispose()
        {
            _handle?.Dispose();
        }

        public override bool Equals(object? obj) => obj is ResourceFileHandle handle && Equals(handle);

        public bool Equals(ResourceFileHandle other) => (_handle == other._handle) && (_offset == other._offset) && (_size == other._size);

        public override int GetHashCode() => HashCode.Combine(_handle, _offset, _size);

        public static bool operator ==(ResourceFileHandle left, ResourceFileHandle right) => left.Equals(right);

        public static bool operator !=(ResourceFileHandle left, ResourceFileHandle right) => !(left == right);

        private unsafe sealed class PointerMemoryManager : MemoryManager<byte>
        {
            private byte* _ptr;
            private int _len;

            public PointerMemoryManager(byte* ptr, int len)
            {
                _ptr = ptr;
                _len = len;
            }

            public override Span<byte> GetSpan() => new Span<byte>(_ptr, _len);

            public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle(_ptr);

            public override void Unpin()
            {
                // nop
            }

            protected override void Dispose(bool disposing)
            {
                _ptr = null;
                _len = 0;
            }
        }
    }
}
