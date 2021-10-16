#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public readonly ref struct ControlLayoutInfo    // `ref struct` to ensure valid access. It is invalid to access pooled instance.
    {
        private readonly ControlLayoutInfoInternal _l;

        public ref LayoutLength Width => ref _l.Width;
        public ref LayoutLength Height => ref _l.Height;
        public ref TransformOrigin TransformOrigin => ref _l.TransformOrigin;
        public ref HorizontalAlignment HorizontalAlignment => ref _l.HorizontalAlignment;
        public ref VerticalAlignment VerticalAlignment => ref _l.VerticalAlignment;
        public ref LayoutThickness Margin => ref _l.Margin;
        public ref LayoutThickness Padding => ref _l.Padding;
        public ref Matrix3 RenderTransform => ref _l.RenderTransform;
        public ref Vector2 RenderTransformOrigin => ref _l.RenderTransformOrigin;

        internal ControlLayoutInfo(ControlLayoutInfoInternal l)
        {
            _l = l;
        }

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => _l.GetHashCode();
    }

    internal sealed class ControlLayoutInfoInternal
    {
        private const uint MaxPooledPerThread = 1024;
        [ThreadStatic]
        private static ControlLayoutInfoInternal? _pooled;
        [ThreadStatic]
        private static uint _pooledCount;

        private ControlLayoutInfoInternal? _nextPooled;

        private LayoutLength _width;
        private LayoutLength _height;
        private TransformOrigin _transformOrigin;
        private HorizontalAlignment _horizontalAlignment;
        private VerticalAlignment _verticalAlignment;
        private LayoutThickness _margin;
        private LayoutThickness _padding;
        private Matrix3 _renderTransform;
        private Vector2 _renderTransformOrigin;

        public ref LayoutLength Width => ref _width;
        public ref LayoutLength Height => ref _height;
        public ref TransformOrigin TransformOrigin => ref _transformOrigin;
        public ref HorizontalAlignment HorizontalAlignment => ref _horizontalAlignment;
        public ref VerticalAlignment VerticalAlignment => ref _verticalAlignment;
        public ref LayoutThickness Margin => ref _margin;
        public ref LayoutThickness Padding => ref _padding;
        public ref Matrix3 RenderTransform => ref _renderTransform;
        public ref Vector2 RenderTransformOrigin => ref _renderTransformOrigin;

        private ControlLayoutInfoInternal()
        {
            Init(this);
        }

        private static void Init(ControlLayoutInfoInternal instance)
        {
            // All fields must be initialized. (except _nextPooled)

            instance._width = new LayoutLength(1f, LayoutLengthType.Proportion);
            instance._height = new LayoutLength(1f, LayoutLengthType.Proportion);
            instance._transformOrigin = TransformOrigin.LeftTop;
            instance._horizontalAlignment = HorizontalAlignment.Center;
            instance._verticalAlignment = VerticalAlignment.Center;
            instance._margin = default;
            instance._padding = default;
            instance._renderTransform = Matrix3.Identity;
            instance._renderTransformOrigin = default;
        }

        public static implicit operator ControlLayoutInfo(ControlLayoutInfoInternal l) => new(l);

        [DoesNotReturn]
        public static ControlLayoutInfoInternal ThrowCannotGetInstance() => throw new InvalidOperationException($"Cannnot get a {nameof(ControlLayoutInfoInternal)} instance.");

        internal static ControlLayoutInfoInternal Create()
        {
            if(_pooled is null) {
                Debug.Assert(_pooledCount == 0);
                return new ControlLayoutInfoInternal();
            }
            else {
                Debug.Assert(_pooledCount > 0);
                _pooledCount--;
                var ret = _pooled;
                _pooled = ret._nextPooled;
                ret._nextPooled = null;
                Init(ret);
                return ret;
            }
        }

        internal static void Return([MaybeNull] ref ControlLayoutInfoInternal instance)
        {
            Debug.Assert(instance is not null);
            Debug.Assert(instance._nextPooled is null);

            if(_pooledCount < MaxPooledPerThread) {
                instance._nextPooled = _pooled;
                _pooled = instance;
                _pooledCount++;
            }
            instance = null;
        }
    }
}
