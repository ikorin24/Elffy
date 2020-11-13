#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Elffy.Mathematics
{
    public static class MathTool
    {
        /// <summary>Pi / 180</summary>
        private const float PiOver180 = Pi / 180f;

        /// <summary>Pi</summary>
        public const float Pi = 3.14159274F;
        /// <summary>1/2 * Pi</summary>
        public const float PiOver2 = 1.57079637F;
        /// <summary>1/3 * Pi</summary>
        public const float PiOver3 = 1.04719758F;
        /// <summary>1/4 * Pi</summary>
        public const float PiOver4 = 0.7853982F;
        /// <summary>1/6 * Pi</summary>
        public const float PiOver6 = 0.5235988F;
        /// <summary>2 * Pi</summary>
        public const float TwoPi = 6.28318548F;
        /// <summary>3/2 * Pi</summary>
        public const float ThreePiOver2 = 4.712389F;
        /// <summary>E</summary>
        public const float E = 2.71828175F;
        /// <summary>Log_10 E</summary>
        public const float Log10E = 0.4342945F;
        /// <summary>Log_2 E</summary>
        public const float Log2E = 1.442695F;

        /// <summary>Convert degree to radian</summary>
        /// <param name="degree">degree value</param>
        /// <returns>radian value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadian(this int degree) => ToRadian((float)degree);

        /// <summary>Convert degree to radian</summary>
        /// <param name="degree">degree value</param>
        /// <returns>radian value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadian(this float degree) => degree * PiOver180;

        /// <summary>Convert radian to degree</summary>
        /// <param name="radian">radian value</param>
        /// <returns>degree value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegree(this int radian) => (float)radian / PiOver180;

        /// <summary>Convert radian to degree</summary>
        /// <param name="radian">radian value</param>
        /// <returns>degree value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegree(this float radian) => radian / PiOver180;

        /// <summary>Round up value to power of two</summary>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundUpToPowerOfTwo(int value)
        {
            if(value > (1 << 30)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as int.");
            }
            return 1 << (32 - BitOperations.LeadingZeroCount((uint)value - 1));
        }

        /// <summary>Round up value to power of two</summary>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RoundUpToPowerOfTwo(uint value)
        {
            if(value > (1u << 31)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as uint.");
            }
            return 1u << (32 - BitOperations.LeadingZeroCount(value - 1));
        }

        /// <summary>Round up value to power of two</summary>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RoundUpToPowerOfTwo(long value)
        {
            if(value > (1L << 62)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as long.");
            }
            return 1L << (64 - BitOperations.LeadingZeroCount((ulong)value - 1));
        }

        /// <summary>Round up value to power of two</summary>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RoundUpToPowerOfTwo(ulong value)
        {
            if(value > (1uL << 63)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as ulong.");
            }
            return 1uL << (64 - BitOperations.LeadingZeroCount(value - 1));
        }
    }
}
