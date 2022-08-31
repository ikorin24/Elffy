#nullable enable
using Elffy;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Serialization.Gltf.Internal;

internal unsafe sealed class LargeBufferWriter<T> : ILargeBufferWriter<T>, IDisposable where T : unmanaged
{
    private NativeBuffer _buf;
    private nuint _count;
    private nuint Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buf.ByteLength / (nuint)sizeof(T);
    }

    public nuint WrittenLength => _count;

    public void Advance(nuint count)
    {
        if(count > Capacity - _count) {
            ThrowArg();
            [DoesNotReturn] static void ThrowArg() => throw new ArgumentException($"{nameof(count)} is too large", nameof(count));
        }
        _count += count;
    }

    public LargeBufferWriter() : this(0)
    {
    }

    public LargeBufferWriter(nuint initialCapacity)
    {
        var byteCapacity = checked(initialCapacity * (nuint)sizeof(T));
        _buf = new NativeBuffer(byteCapacity);
        _count = 0;
    }

    ~LargeBufferWriter()
    {
        _buf.Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _buf.Dispose();
        _count = 0;
    }

    public T* GetWrittenBufffer(out nuint count)
    {
        count = _count;
        return (T*)_buf.Ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* GetBufferToWrite(nuint count, bool zeroCleared)
    {
        if(count > Capacity - _count) {
            ResizeBuffer(Capacity + count);
            Debug.Assert(count <= Capacity - _count);
        }
        var p = (T*)_buf.Ptr + _count;
        if(zeroCleared) {
            Clear(p, count * (nuint)sizeof(T));

            static void Clear(void* ptr, nuint byteLen)
            {
#if NET7_0_OR_GREATER
                NativeMemory.Clear(ptr, byteLen);
#else
                if(byteLen <= int.MaxValue) {
                    new Span<byte>(ptr, (int)byteLen).Clear();
                }
                else {
                    throw new NotImplementedException();
                }
#endif
            }
        }
        return p;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
    private void ResizeBuffer(nuint minCapacity)
    {
        nuint minByteCapacity = minCapacity * (nuint)sizeof(T);
        nuint availableMaxByteLength = nuint.MaxValue - nuint.MaxValue % (nuint)sizeof(T);

        if(_buf.ByteLength == availableMaxByteLength) {
            throw new InvalidOperationException("cannot write any more.");
        }
        nuint newByteCapacity;
        if(_buf.ByteLength >= availableMaxByteLength / 2) {
            newByteCapacity = availableMaxByteLength;
        }
        else {
            newByteCapacity = Math.Max(Math.Max(4, minByteCapacity), _buf.ByteLength * 2);
        }
        if(newByteCapacity < minByteCapacity) {
            throw new ArgumentOutOfRangeException("Required capacity is too large.");
        }

        var newBuf = new NativeBuffer(newByteCapacity);
        try {
            Buffer.MemoryCopy(_buf.Ptr, newBuf.Ptr, newBuf.ByteLength, _count * (nuint)sizeof(T));
        }
        catch {
            newBuf.Dispose();
            throw;
        }
        _buf.Dispose();
        _buf = newBuf;
    }
}
