#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Mathematics
{
    /// <summary>Pseudorandom number generator based on 32 bits Xorshift algorithm.</summary>
    public struct Xorshift32
    {
        private uint _seed;

        public uint CurrentSeed => _seed;

        // [NOTICE]
        // An instance created by default constructor DOES NOT WORK !!!!
        // Seed must not be 0.

        /// <summary>Create a new instance of <see cref="Xorshift32"/> initialized by current time as a seed.</summary>
        public Xorshift32()
        {
            // get lower 32 bits value of 64 bits
            var seed = (uint)DateTime.Now.Ticks;

            // avoid seed == 0. (It does not work if seed is 0)
            _seed = (seed == 0) ? 1 : seed;
        }

        /// <summary>Create a new instance of <see cref="Xorshift32"/> initialized by the specified seed.</summary>
        /// <param name="seed">seed value (!= 0)</param>
        public Xorshift32(int seed)
        {
            _seed = (seed == 0) ? 1 : (uint)seed;
        }

        /// <summary>Get next random value of <see cref="uint"/>, ranged by 0 &lt; value &lt;= <see cref="uint.MaxValue"/> .</summary>
        /// <returns>generated value of <see cref="uint"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Uint32()
        {
            // 32 bits read/write is atomic, so this method is thread-safe.
            var value = _seed;
            value ^= (value << 13);
            value ^= (value >> 17);
            value ^= (value << 5);
            _seed = value;
            return value;
        }

        /// <summary>Get next random value of <see cref="int"/>, ranged by <see cref="int.MinValue"/> &lt;= value &lt;= <see cref="int.MaxValue"/>, except 0</summary>
        /// <returns>generated value of <see cref="int"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Int32() => (int)Uint32();

        /// <summary>Get next random value of <see cref="float"/>, ranged by 0 &lt; value &lt;= 1 .</summary>
        /// <returns>generated value of <see cref="float"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Single() => Uint32() / (float)uint.MaxValue;
    }
}
