#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Shading.Defered
{
    public readonly ref struct LightUpdateContext
    {
        private readonly Span<Vector4> _positions;
        private readonly Span<Color4> _colors;

        public int LightCount => _positions.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LightUpdateContext(Span<Vector4> positions, Span<Color4> colors)
        {
            if(positions.Length != colors.Length) {
                ThrowArg($"{nameof(positions)} and {nameof(colors)} must have same length.");
            }
            Debug.Assert(positions.Length == colors.Length);
            _positions = positions;
            _colors = colors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPointLight(int index, in Vector3 position, in Color4 color)
        {
            if((uint)index >= _positions.Length) {
                ThrowOutOfRange();
            }
            _positions.At(index) = new Vector4(position, 1);
            _colors.At(index) = color;
        }

        // TODO: direct light

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Vector4> GetPositionSpan() => _positions;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Color4> GetColorSpan() => _colors;

        [DoesNotReturn]
        private static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException("index is out of range");

        [DoesNotReturn]
        private static void ThrowArg(string message) => throw new ArgumentException(message);
    }

    public delegate void LightUpdateAction(LightUpdateContext context);
}
