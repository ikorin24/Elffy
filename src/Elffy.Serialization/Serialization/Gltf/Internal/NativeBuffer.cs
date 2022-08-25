#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Serialization.Gltf.Internal;

internal unsafe readonly struct NativeBuffer : IDisposable
{
    private readonly IntPtr _ptr;
    private readonly nuint _size;

    public byte* Ptr => (byte*)_ptr;
    public nuint ByteLength => _size;

    public NativeBuffer(nuint size)
    {
        if(size == 0) {
            _ptr = IntPtr.Zero;
            _size = 0;
            return;
        }
        _ptr = (IntPtr)NativeMemory.Alloc(size);
        _size = size;
    }

    public bool TryGetSpan(out Span<byte> span)
    {
        if(_size > int.MaxValue) {
            span = default;
            return false;
        }
        span = MemoryMarshal.CreateSpan(ref *(byte*)_ptr, (int)_size);
        return true;
    }

    public Memory<byte>[] GetMemories()
    {
        if(_size == 0) {
            return Array.Empty<Memory<byte>>();
        }
        var memoryCount = (int)(_size >> 31) + 1;
        var memories = new Memory<byte>[memoryCount];
        var ptr = (byte*)_ptr;
        nuint pos = 0;
        for(int i = 0; i < memories.Length; i++) {
            int len = (int)Math.Min(int.MaxValue, _size - pos);
            memories[i] = new PointerMemoryManager(ptr + pos, len).Memory;
            pos += (nuint)len;
        }
        return memories;
    }

    public PointerMemoryStream GetStream() => new PointerMemoryStream(Ptr, ByteLength);

    public void Dispose()
    {
        NativeMemory.Free((void*)_ptr);
        Unsafe.AsRef(in _ptr) = default;
        Unsafe.AsRef(in _size) = 0;
    }
}
