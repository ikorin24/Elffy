using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Mathmatics
{
    public static class Curve
    {
        private const float MIN_X = 0f;
        private const float MAX_X = 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Linear(float value)
        {
            value = CheckRange(value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float value)
        {
            value = CheckRange(value);
            // (0 ~ 1) -> (0, pi/2)
            var x = value * MathHelper.PiOver2;
            return (float)Math.Sin(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SinSmooth(float value)
        {
            value = CheckRange(value);
            // (0 ~ 1) -> (-pi/2, pi/2)
            var x = value * MathHelper.Pi - MathHelper.PiOver2;
            return (float)Math.Sin(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float LinearPrivate(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CheckRange(float value)
        {
            return (value < MIN_X) ? MIN_X : (value > MAX_X) ? MAX_X : value;
        }
    }
}
