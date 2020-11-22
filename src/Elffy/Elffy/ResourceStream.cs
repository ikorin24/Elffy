#nullable enable
using Elffy.Core;
using Elffy.Effective.Unsafes;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Stream class used by <see cref="LocalResourceLoader"/></summary>
    public sealed class ResourceStream : Stream
    {
        // このクラスはもとになる Stream の一部だけを切り出した Stream として振る舞う。
        // _head と _length がもとの Stream 上での位置と長さ。

        private bool _disposed = false;
        private AlloclessFileStream _innerStream = null!;
        private readonly long _head;
        private readonly long _length;

        /// <summary>The property returns true</summary>
        public override bool CanRead => true;

        // [NOTE]
        // Stream から System.Drawing.Bitmap を作る時に、Stream.CanSeek が false だと
        // Stream から MemoryStream へのコピーが起こり、メモリの無駄。
        // Seek を許可しても特に問題はないため、CanSeek は true にして Seek も実装する。

        /// <summary>The property returns true</summary>
        public override bool CanSeek => true;

        /// <summary>The property returns false</summary>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
                return _length;
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
                return _innerStream.Position - _head;
            }
            set
            {
                if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
                if((ulong)value >= (ulong)_length) { throw new ArgumentOutOfRangeException(nameof(value), value, "value is out of range"); }
                _innerStream.Position = _head + value;
            }
        }

        internal ResourceStream(string resourceFilePath, in ResourceObject obj)
        {
            Debug.Assert(obj.Position >= 0);
            Debug.Assert(obj.Length >= 0);

            _head = obj.Position;
            _length = obj.Length;
            var stream = AlloclessFileStream.OpenRead(resourceFilePath);
            stream.Position = _head;
            _innerStream = stream;
        }

        ~ResourceStream() => Dispose(false);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
            if(buffer is null) { throw new ArgumentNullException(nameof(buffer)); }
            return ReadCore(buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
            return ReadCore(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReadCore(Span<byte> buffer)
        {
            var current = _innerStream.Position;
            var available = (int)(_head + _length - current);
            var readLen = Math.Min(buffer.Length, available);
            return _innerStream.Read(buffer.Slice(0, readLen));
        }

        protected override void Dispose(bool disposing)
        {
            if(_disposed) { return; }
            if(disposing) {
                _innerStream.Dispose();
                _innerStream = null!;
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = 0;
            switch(origin) {
                case SeekOrigin.Begin:
                    newPos = offset;
                    break;
                case SeekOrigin.Current:
                    newPos = (_innerStream.Position - _head) + offset;
                    break;
                case SeekOrigin.End:
                    newPos = _length + offset;
                    break;
                default:
                    break;
            }
            Position = newPos;
            return newPos;
        }

        /// <summary>Not supported !! (This method throws <see cref="NotSupportedException"/>)</summary>
        public override void Flush() => throw new NotSupportedException();

        /// <summary>Not supported !! (This method throws <see cref="NotSupportedException"/>)</summary>
        /// <param name="value"></param>
        public override void SetLength(long value) => throw new NotSupportedException();
        
        /// <summary>Not supported !! (This method throws <see cref="NotSupportedException"/>)</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
