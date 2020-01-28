#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Mathmatics
{
    /// <summary>Pseudorandom number generator based on 32 bits Xorshift algorithm.</summary>
    internal struct Xorshift32
    {
        private uint _seed;

        // [NOTICE]
        // An instance created by default constructor DOES NOT WORK !!!!
        // Seed must not be 0.

        /// <summary>Create new instance of <see cref="Xorshift32"/> initialized specified seed.</summary>
        /// <param name="seed">seed value (not zero)</param>
        public Xorshift32(int seed)
        {
            if(seed == 0) { throw new ArgumentException("0 is invalid seed"); }
            _seed = (uint)seed;
        }

        /// <summary>Create new instance of <see cref="Xorshift32"/></summary>
        public static unsafe Xorshift32 GetDefault()
        {
            // get lower 32 bits value of 64 bits
            var seed = (int)DateTime.Now.Ticks;

            // get undefined value from new allocked unmanaged heap.
            var ptr = default(IntPtr);
            try {
                ptr = Marshal.AllocHGlobal(sizeof(int));
                seed ^= ((int*)ptr)[0];
            }
            finally {
                if(ptr != default) {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            // avoid seed == 0. (It does not work if seed is 0)
            seed = (seed == 0) ? 1 : seed;

            return new Xorshift32(seed);
        }

        /// <summary>Get next random value of <see cref="uint"/>, ranged by 0 &lt; value &lt;= <see cref="uint.MaxValue"/> .</summary>
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
