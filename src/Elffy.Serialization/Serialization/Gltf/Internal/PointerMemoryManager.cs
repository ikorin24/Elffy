#nullable enable
using Elffy.Serialization.Gltf;
using System;
using System.Buffers;

namespace Elffy.Serialization.Gltf.Internal;

internal unsafe sealed class PointerMemoryManager : MemoryManager<byte>
{
    private readonly void* _ptr;
    private readonly int _length;

    public PointerMemoryManager(void* ptr, int length)
    {
        _ptr = ptr;
        _length = length;
    }

    public override Span<byte> GetSpan() => new Span<byte>(_ptr, _length);

    public override MemoryHandle Pin(int elementIndex = 0) => default;

    public override void Unpin()
    {
    }

    protected override void Dispose(bool disposing)
    {
    }
}

