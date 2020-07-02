#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Diagnostics;
using System.IO;

namespace Elffy
{
    /// <summary><see cref="Resources"/> 内のデータを読み取るためのストリームを提供します</summary>
    public sealed class ResourceStream : Stream
    {
        // このクラスはもとになる Stream の一部だけを切り出した Stream として振る舞う。
        // _head と _length がもとの Stream 上での位置と長さ。

        private bool _disposed = false;
        private AlloclessFileStream _innerStream = null!;
        private readonly long _head;
        private readonly long _length;

        public override bool CanRead => true;

        // [NOTE]
        // Stream から System.Drawing.Bitmap を作る時に、Stream.CanSeek が false だと
        // Stream から MemoryStream へのコピーが起こり、メモリの無駄。
        // Seek を許可しても特に問題はないため、CanSeek は true にして Seek も実装する。

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
                return _length;
            }
        }

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

        internal ResourceStream(long head, long length)
        {
            Debug.Assert(head >= 0);
            Debug.Assert(length >= 0);

            _head = head;
            _length = length;
            var stream = AlloclessFileStream.OpenRead(Resources.ResourceFilePath);
            stream.Position = _head;
            _innerStream = stream;
        }

        ~ResourceStream() => Dispose(false);

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
            var current = _innerStream.Position;
            var available = (int)(_head + _length - current);
            return _innerStream.Read(buffer, offset, Math.Min(count, available));
        }

        protected override void Dispose(bool disposing)
        {
            if(_disposed) { return; }
            if(disposing) {
                // マネージリソース解放
                _innerStream.Dispose();
                _innerStream = null!;
            }
            // アンマネージドリソースがある場合ここに解放処理を書く
            _disposed = true;
            base.Dispose(disposing);
        }

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
                    newPos = _length - offset;
                    break;
                default:
                    break;
            }
            Position = newPos;
            return newPos;
        }
        public override void Flush() => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
