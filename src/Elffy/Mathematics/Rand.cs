#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Mathematics
{
    /// <summary>Random value generator class</summary>
    public static class Rand
    {
        // They are thread-safe
        private static Xorshift32 _xorshift32 = Xorshift32.GetDefault();    // do not change into readonly
        private static Xorshift64 _xorshift64 = Xorshift64.GetDefault();    // do not change into readonly

        /// <summary>Get random color (R, G, B are random, Alpha is 1).</summary>
        /// <returns>random color</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 Color4() => new Color4(_xorshift32.Single(), _xorshift32.Single(), _xorshift32.Single());

        /// <summary>Get random color (R, G, B, A are random.)</summary>
        /// <returns>random color</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 AlphaColor4() => new Color4(_xorshift32.Single(), _xorshift32.Single(), _xorshift32.Single(), _xorshift32.Single());

        /// <summary>Get random <see cref="Vector2"/>, that is normalized.</summary>
        /// <returns>random <see cref="Vector2"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Vector2() => new Vector2(_xorshift32.Single(), _xorshift32.Single()).Normalized();

        /// <summary>Get random <see cref="Vector3"/>, that is normalized.</summary>
        /// <returns>random <see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Vector3() => new Vector3(_xorshift32.Single(), _xorshift32.Single(), _xorshift32.Single()).Normalized();

        /// <summary>Get random <see cref="Vector4"/>, that is normalized.</summary>
        /// <returns>random <see cref="Vectot4"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Vector4() => new Vector4(_xorshift32.Single(), _xorshift32.Single(), _xorshift32.Single(), _xorshift32.Single()).Normalized();

        /// <summary>Get 1 or -1 randomly</summary>
        /// <returns>1 or -1</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign() => (_xorshift32.Int32() & 2) - 1;

        /// <summary>Get random <see cref="int"/> value ranged by 0 &lt;= value &lt;= <see cref="int.MaxValue"/></summary>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int() => (int)(_xorshift32.Uint32() & 0x7FFFFFFF);

        /// <summary>Get random <see cref="int"/> value ranged by 0 &lt;= value &lt; <paramref name="max"/></summary>
        /// <param name="max">max value of range which does not include. (must be positive value)</param>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int(int max) => (max > 0) ? (Int() % max) : throw new ArgumentOutOfRangeException();

        /// <summary>Get random <see cref="int"/> value range by <paramref name="min"/> &lt;= value &lt; <paramref name="max"/></summary>
        /// <param name="min">min value of range which include. (must be positive value)</param>
        /// <param name="max">max value of range which does not include. (must be bigger than <paramref name="min"/> or equals.)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int(int min, int max)
        {
            var range = max - min;
            return (range >= 0) ? (min + Int() % (max - min)) : throw new ArgumentOutOfRangeException();
        }

        /// <summary>Get random <see cref="float"/> value ranged by 0 &lt; value &lt;= 1</summary>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Float() => _xorshift32.Single();

        /// <summary>Get random <see cref="float"/> value ranged by 0 &lt; value &lt;= <paramref name="max"/></summary>
        /// <param name="max">max value of range</param>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Float(float max) => _xorshift32.Single() * max;

        /// <summary>Get random <see cref="float"/> value ranged by <paramref name="min"/> &lt; value &lt;= <paramref name="max"/></summary>
        /// <param name="min">min value of range</param>
        /// <param name="max">max value of range. (must be bigger than <paramref name="min"/> or equals.)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Float(float min, float max)
        {
            var range = max - min;
            return (range >= 0) ? (min + _xorshift32.Single() * (max - min)) : throw new ArgumentOutOfRangeException();
        }

        /// <summary>Get random <see cref="double"/> value ranged 0 &lt; value &lt;= 1 .</summary>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Double() => _xorshift64.Double();

        /// <summary>Get random <see cref="double"/> value ranged by 0 &lt; value &lt;= <paramref name="max"/></summary>
        /// <param name="max">max value of range</param>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Double(double max) => _xorshift64.Double() * max;

        /// <summary>Get random <see cref="double"/> value ranged by <paramref name="min"/> &lt; value &lt;= <paramref name="max"/></summary>
        /// <param name="min">min value of range</param>
        /// <param name="max">max value of range. (must be bigger than <paramref name="min"/> or equals.)</param>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Double(double min, double max)
        {
            var range = max - min;
            return (range >= 0) ? (min + _xorshift64.Double() * (max - min)) : throw new ArgumentOutOfRangeException();
        }
    }
}
