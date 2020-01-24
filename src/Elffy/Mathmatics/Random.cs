#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Mathmatics
{
    public static class Rand
    {
        private static Xorshift32 _bit32 = new Xorshift32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 Color4() => new Color4(_bit32.Single(), _bit32.Single(), _bit32.Single());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 AlphaColor4() => new Color4(_bit32.Single(), _bit32.Single(), _bit32.Single(), _bit32.Single());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Vector2() => new Vector2(_bit32.Single(), _bit32.Single()).Normalized();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Vector3() => new Vector3(_bit32.Single(), _bit32.Single(), _bit32.Single()).Normalized();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Vector4() => new Vector4(_bit32.Single(), _bit32.Single(), _bit32.Single(), _bit32.Single()).Normalized();

        /// <summary>Get random value ranged by 0 &lt;= value &lt;= <see cref="int.MaxValue"/></summary>
        /// <returns>random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int() => (int)(_bit32.Uint32() & 0x7FFFFFFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int(int max) => (max > 0) ? (Int() % max) : throw new ArgumentOutOfRangeException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int(int min, int max)
        {
            var range = max - min;
            return (range >= 0) ? (min + Int() % (max - min)) : throw new ArgumentOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Float() => _bit32.Single();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Float(float max) => _bit32.Single() * max;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Float(float min, float max)
        {
            var range = max - min;
            return (range >= 0) ? (min + _bit32.Single() * (max - min)) : throw new ArgumentOutOfRangeException();
        }
    }
}
