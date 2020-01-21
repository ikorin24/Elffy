#nullable enable
using System;

namespace Elffy.Mathmatics
{
    public static class Rand
    {
        private static Random? _r;
        private static Random _rand => (_r ??= new Random());

        public static Color4 Color4() => new Color4((float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble());
        public static Color4 AlphaColor4() => new Color4((float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble());

        public static Vector2 Vector2() => new Vector2((float)_rand.NextDouble(), (float)_rand.NextDouble()).Normalized();
        public static Vector3 Vector3() => new Vector3((float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble()).Normalized();
        public static Vector4 Vector4() => new Vector4((float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble()).Normalized();

        public static int Int() => _rand.Next();
        public static int Int(int max) => _rand.Next(max);
        public static int Int(int min, int max) => _rand.Next(min, max);

        public static float Float() => (float)_rand.NextDouble();
        public static float Float(float max) => (float)_rand.NextDouble() * max;
        public static float Float(float min, float max) => min + (float)_rand.NextDouble() * (max - min);

        public static double Double() => _rand.NextDouble();
        public static double Double(double max) => _rand.NextDouble() * max;
        public static double Double(double min, double max) => min + _rand.NextDouble() * (max - min);

    }
}
