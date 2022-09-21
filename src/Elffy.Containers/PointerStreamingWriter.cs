#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    internal unsafe ref struct PointerStreamingWriter
    {
        public readonly byte* Origin;
        public ulong Offset;

        public ref byte Head => ref *(Origin + Offset);
        public byte* HeadPtr => Origin + Offset;

        public PointerStreamingWriter(void* ptr)
        {
            Origin = (byte*)ptr;
            Offset = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<TValue>(scoped in TValue value) where TValue : unmanaged
        {
            *(TValue*)HeadPtr = value;
            Offset += (ulong)sizeof(TValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(void* data, ulong dataByteLen)
        {
            Buffer.MemoryCopy(data, HeadPtr, dataByteLen, dataByteLen);
            Offset += dataByteLen;
        }
    }
}
