#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct ValueTypeRentMemory<T> : IDisposable where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"{nameof(ValueTypeRentMemory<T>)}<{typeof(T).Name}>[{Span.Length}]";

        // IMemoryOwner<T> を継承するメリットが特になく、
        // Memory<T> を公開する方法もないので
        // IMemoryOwner<T> は継承しない。

        private readonly int _id;
        private readonly int _lender;
        private readonly Memory<byte> _byteMemory;

        public readonly Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.Cast<byte, T>(_byteMemory.Span);
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _byteMemory.IsEmpty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ValueTypeRentMemory(int length)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(!MemoryPool.TryRentByteMemory<T>(length, out _byteMemory, out _id, out _lender)) {
                Debug.Assert(_lender < 0);
                _byteMemory = new byte[sizeof(T) * length];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(!_byteMemory.IsEmpty) {
                MemoryPool.ReturnByteMemory(_lender, _id);
            }
        }
    }
}
