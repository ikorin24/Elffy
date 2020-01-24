#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Mathmatics
{
    /// <summary>Pseudorandom number generator based on 64 bits Xorshift algorithm.</summary>
    public sealed class Xorshift64
    {
        private ulong _seed;

        /// <summary>Create new instance of <see cref="Xorshift64"/> whose seed is initialized from current time.</summary>
        public Xorshift64()
        {
            _seed = (ulong)DateTime.Now.Ticks;
        }

        /// <summary>Create new instance of <see cref="Xorshift64"/> initialized specified seed.</summary>
        /// <param name="seed">seed value</param>
        public Xorshift64(long seed) => _seed = (ulong)seed;

        /// <summary>Get next random value of <see cref="ulong"/>, ranged by 0 &lt;= value &lt;= <see cref="ulong.MaxValue"/> .</summary>
        /// <returns>generated value of <see cref="ulong"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Uint64()
        {
            var value = _seed;
            value ^= (value << 13);
            value ^= (value >> 7);
            value ^= (value << 17);
            _seed = value;
            return value;
        }

        /// <summary>Get next random value of <see cref="long"/>, ranged by <see cref="long.MinValue"/> &lt;= value &lt;= <see cref="long.MaxValue"/></summary>
        /// <returns>generated value of <see cref="long"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Int64() => (long)Uint64();

        /// <summary>Get next random value of <see cref="double"/>, ranged by 0 &lt;= value &lt;= 1 .</summary>
        /// <returns>generated value of <see cref="double"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Double() => Uint64() / (double)uint.MaxValue;
    }
}
