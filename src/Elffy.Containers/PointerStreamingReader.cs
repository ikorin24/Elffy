#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy
{
    internal unsafe ref struct PointerStreamingReader
    {
        public readonly byte* Origin;
        public ulong Offset;

        public ref byte Head => ref *(Origin + Offset);
        public byte* HeadPtr => Origin + Offset;

        public PointerStreamingReader(void* ptr)
        {
            Origin = (byte*)ptr;
            Offset = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue Read<TValue>() where TValue : unmanaged
        {
            ref var value = ref *(TValue*)HeadPtr;
            Offset += (ulong)sizeof(TValue);
            return ref value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue* ReadArray<TValue>(ulong count) where TValue : unmanaged
        {
            var array = (TValue*)HeadPtr;
            Offset += (ulong)sizeof(TValue) * count;
            return array;
        }
    }
}
