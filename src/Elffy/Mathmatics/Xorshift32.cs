#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Mathmatics
{
    /// <summary>Pseudorandom number generator based on 32 bits Xorshift algorithm.</summary>
    public sealed class Xorshift32
    {
        private uint _seed;

        /// <summary>Create new instance of <see cref="Xorshift32"/> whose seed is initialized from current time.</summary>
        public Xorshift32()
        {
            // get lower 32 bits value of 64 bits
            _seed = (uint)DateTime.Now.Ticks;
        }

        /// <summary>Create new instance of <see cref="Xorshift32"/> initialized specified seed.</summary>
        /// <param name="seed">seed value</param>
        public Xorshift32(int seed) => _seed = (uint)seed;

        /// <summary>Get next random value of <see cref="uint"/>, ranged by 0 &lt;= value &lt;= <see cref="uint.MaxValue"/> .</summary>
        /// <returns>generated value of <see cref="uint"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Uint32()
        {
            var value = _seed;
            value ^= (value << 13);
            value ^= (value >> 17);
            value ^= (value << 5);
            _seed = value;
            return value;
        }

        /// <summary>Get next random value of <see cref="int"/>, ranged by <see cref="int.MinValue"/> &lt;= value &lt;= <see cref="int.MaxValue"/></summary>
        /// <returns>generated value of <see cref="int"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Int32() => (int)Uint32();


        /// <summary>Get next random value of <see cref="float"/>, ranged by 0 &lt;= value &lt;= 1 .</summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Single() => Uint32() / (float)uint.MaxValue;
    }
}
