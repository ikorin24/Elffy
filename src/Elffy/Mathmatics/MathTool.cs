#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Mathmatics
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float value)
        {
            return (float)Math.Sin(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float value)
        {
            return (float)Math.Cos(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float value)
        {
            return (float)Math.Tan(value);
        }

        /// <summary>Convert degree to radian</summary>
        /// <param name="degree">degree value</param>
        /// <returns>radian value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadian(this float degree)
        {
            return degree * PiOver180;
        }

        /// <summary>Convert radian to degree</summary>
        /// <param name="radian">radian value</param>
        /// <returns>degree value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegree(this float radian)
        {
            return radian / PiOver180;
        }
    }
}
