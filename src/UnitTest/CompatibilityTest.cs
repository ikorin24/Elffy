#nullable enable
using System.Runtime.CompilerServices;
using Elffy;
using Xunit;
using SkiaSharp;

namespace UnitTest
{
    public class CompatibilityTest
    {
        [Fact]
        public unsafe void SKPoint()
        {
            Assert.True(sizeof(SKPoint) == sizeof(float) * 2);
            Assert.True(sizeof(SKPoint) == sizeof(Vector2));
            var skPoint = new SKPoint(10f, -40f);
            ref var vec2 = ref Unsafe.As<SKPoint, Vector2>(ref skPoint);

            Assert.Equal(skPoint.X, vec2.X);
            Assert.Equal(skPoint.Y, vec2.Y);
        }
    }
}
