#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    public readonly ref struct ReadOnlySpanTuple<T1, T2>
    {
        public readonly ReadOnlySpan<T1> Item1;
        public readonly ReadOnlySpan<T2> Item2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanTuple(ReadOnlySpan<T1> item1, ReadOnlySpan<T2> item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out ReadOnlySpan<T1> item1, out ReadOnlySpan<T2> item2)
        {
            item1 = Item1;
            item2 = Item2;
            ValueTuple<int, int> a;
        }
    }
}
