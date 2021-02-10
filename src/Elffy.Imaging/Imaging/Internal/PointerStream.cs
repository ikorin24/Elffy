#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;
using System.Diagnostics;

namespace Elffy.Imaging.Internal
{
    internal unsafe sealed class PointerStream : Stream
    {
        private const int MaxPoolPerThread = 8;
        [ThreadStatic]
        private static PointerStream? _poolHead;
        [ThreadStatic]
        private static int _pooledCount;

        private PointerStream? _nextPooled;
        private IntPtr _ptr;
        private long _length;
        private long _pos;

        private Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(_ptr.ToPointer()), (int)_length);

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position { get => _pos; set => _pos = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PointerStream(void* ptr, int length)
        {
            Init(ptr, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Init(void* ptr, int length)
        {
            _ptr = (IntPtr)ptr;
            _length = length;
            _pos = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointerStream Create(void* ptr, int length)
        {
            if(_poolHead is null) {
                Debug.Assert(_pooledCount == 0);
                return _poolHead = new PointerStream(ptr, length);
            }
            else {
                var instance = _poolHead;
                _poolHead = instance._nextPooled;
                instance._nextPooled = null;
                instance.Init(ptr, length);
                _pooledCount--;
                return instance;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Return(PointerStream instance)
        {
            if(_pooledCount > MaxPoolPerThread) { return; }
            instance.Init(null, 0);
            instance._nextPooled = _poolHead;
            _poolHead = instance;
            _pooledCount++;
        }

        public override void Flush() { }    // nop

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => ReadCore(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer) => ReadCore(buffer);

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCore(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            WriteCore(buffer);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _pos + offset,
                SeekOrigin.End => _length + offset,
                _ => _pos,
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Return(this);
        }

        private int ReadCore(Span<byte> buffer)
        {
            var span = Span.SliceUnsafe((int)_pos);
            var len = Math.Min(span.Length, buffer.Length);
            span.SliceUnsafe(0, len).CopyTo(buffer);
            _pos += len;
            return len;
        }

        private void WriteCore(ReadOnlySpan<byte> buffer)
        {
            buffer.CopyTo(Span.SliceUnsafe((int)_pos));
            _pos += buffer.Length;
        }
    }
}
