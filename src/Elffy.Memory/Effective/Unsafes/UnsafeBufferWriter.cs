#nullable enable
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Effective.Unsafes
{
    public sealed class UnsafeBufferWriter<T> : IBufferWriter<T>, IDisposable where T : unmanaged
    {
        private UnsafeRawArray<T> _buffer;
        private int _count;

        public Span<T> WrittenSpan => _buffer.AsSpan(0, _count);

        public UnsafeBufferWriter()
        {
            _buffer = UnsafeRawArray<T>.Empty;
        }

        public UnsafeBufferWriter(int capacity)
        {
            _buffer = new UnsafeRawArray<T>(capacity, false);
        }

        ~UnsafeBufferWriter() => Dispose(false);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            _buffer.Dispose();
        }

        public void Advance(int count)
        {
            if(count < 0) { ThrowOutOfRange(nameof(count)); }
            _count += count;
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            throw new NotImplementedException();    // TODO:
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            var requestedSize = Math.Max(sizeHint, 0);

            if(_buffer.Length < requestedSize + _count) {
                Resize(this, requestedSize);
            }
            return _buffer.AsSpan(_count, requestedSize);


            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Resize(UnsafeBufferWriter<T> self, int requestedSize)
            {
                var newCapacity = Math.Max(4, Math.Max(requestedSize + self._count, self._buffer.Length * 2));
                var newBuffer = new UnsafeRawArray<T>(newCapacity, false);
                try {
                    self._buffer.AsSpan(0, self._count).CopyTo(newBuffer.AsSpan());
                }
                catch {
                    newBuffer.Dispose();
                    throw;
                }
                self._buffer.Dispose();
                self._buffer = newBuffer;
            }
        }

        [DoesNotReturn]
        private static void ThrowOutOfRange(string name) => throw new ArgumentOutOfRangeException(name);
    }
}
