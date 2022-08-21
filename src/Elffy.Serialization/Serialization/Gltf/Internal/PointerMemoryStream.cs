#nullable enable
using Elffy.Serialization.Gltf;
using System;
using System.IO;

namespace Elffy.Serialization.Gltf.Internal;

internal unsafe sealed class PointerMemoryStream : Stream
{
    private readonly byte* _ptr;
    private readonly long _length;
    private long _pos;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position { get => _pos; set => _pos = value; }

    public PointerMemoryStream(byte* ptr, nuint length)
    {
        if(length > long.MaxValue) { throw new NotSupportedException("Too large"); }
        _ptr = ptr;
        _length = (long)length;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var p = _ptr + _pos;
        int copyLen = (int)Math.Min(count, _length - _pos);
        var source = new Span<byte>(p, copyLen);
        var dest = buffer.AsSpan(offset, copyLen);
        source.CopyTo(dest);
        _pos += copyLen;
        return copyLen;
    }

    public override int Read(Span<byte> buffer)
    {
        var p = _ptr + _pos;
        int copyLen = (int)Math.Min(buffer.Length, _length - _pos);
        var source = new Span<byte>(p, copyLen);
        source.CopyTo(buffer);
        _pos += copyLen;
        return copyLen;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        // TODO: not implemented yet
        throw new NotImplementedException();
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

