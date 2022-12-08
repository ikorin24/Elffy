#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Elffy.Mathematics
{
    /// <summary>Pseudorandom number generator based on 64 bits Xorshift algorithm.</summary>
    public struct Xorshift64
    {
        private ulong _seed;

        public ulong CurrentSeed => _seed;

        // [NOTICE]
        // An instance created by default constructor DOES NOT WORK !!!!
        // Seed must not be 0.

        /// <summary>Create a new instance of <see cref="Xorshift64"/> initialized by current time as a seed.</summary>
        public Xorshift64()
        {
            var seed = DateTime.Now.Ticks;

            // avoid seed == 0. (It does not work if seed is 0)
            _seed = (seed == 0) ? 1 : (ulong)seed;
        }

        /// <summary>Create new instance of <see cref="Xorshift64"/> initialized specified seed.</summary>
        /// <param name="seed">seed value (!= 0)</param>
        public Xorshift64(long seed)
        {
            // avoid seed == 0. (It does not work if seed is 0)
            _seed = seed == 0 ? 1 : (ulong)seed;
        }

        /// <summary>Get next random value of <see cref="ulong"/>, ranged by 0 &lt; value &lt;= <see cref="ulong.MaxValue"/> .</summary>
        /// <returns>generated value of <see cref="ulong"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Uint64()
        {
            // IntPtr.Size is JIT constant. JIT removes the branch.
            // This method is thread-safe.

            if(IntPtr.Size == 8) {
                var value = _seed;          // 64bits read/write is atomic
                value ^= (value << 13);
                value ^= (value >> 7);
                value ^= (value << 17);
                _seed = value;              // 64bits read/write is atomic
                return value;
            }

            if(IntPtr.Size == 4) {
                var value = (ulong)Interlocked.Read(ref Unsafe.As<ulong, long>(ref _seed));
                value ^= (value << 13);
                value ^= (value >> 7);
                value ^= (value << 17);
                Interlocked.Exchange(ref Unsafe.As<ulong, long>(ref _seed), (long)value);
                return value;
            }

            throw new NotSupportedException("What is your runtime? 32 btis? 64 bits?");
        }

        /// <summary>Get next random value of <see cref="long"/>, ranged by <see cref="long.MinValue"/> &lt;= value &lt;= <see cref="long.MaxValue"/>, except 0</summary>
        /// <returns>generated value of <see cref="long"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Int64() => (long)Uint64();

        /// <summary>Get next random value of <see cref="double"/>, ranged by 0 &lt; value &lt;= 1 .</summary>
        /// <returns>generated value of <see cref="double"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Double() => Uint64() / (double)uint.MaxValue;
    }
}
