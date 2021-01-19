#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public readonly ref struct ControlLayouter
    {
        private readonly ControlLayouterInternal _l;

        public ref LayoutLength Width => ref _l.Width;
        public ref LayoutLength Height => ref _l.Height;
        public ref TransformOrigin TransformOrigin => ref _l.TransformOrigin;
        public ref HorizontalAlignment HorizontalAlignment => ref _l.HorizontalAlignment;
        public ref VerticalAlignment VerticalAlignment => ref _l.VerticalAlignment;
        public ref RectF Margin => ref _l.Margin;
        public ref RectF Padding => ref _l.Padding;

        internal ControlLayouter(ControlLayouterInternal l)
        {
            _l = l;
        }
    }

    internal sealed class ControlLayouterInternal
    {
        private const uint MaxPooledPerThread = 1024;
        [ThreadStatic]
        private static ControlLayouterInternal? _pooled;
        [ThreadStatic]
        private static uint _pooledCount;

        private ControlLayouterInternal? _nextPooled;

        private LayoutLength _width;
        private LayoutLength _height;
        private TransformOrigin _transformOrigin;
        private HorizontalAlignment _horizontalAlignment;
        private VerticalAlignment _verticalAlignment;
        private RectF _margin;
        private RectF _padding;

        public ref LayoutLength Width => ref _width;
        public ref LayoutLength Height => ref _height;
        public ref TransformOrigin TransformOrigin => ref _transformOrigin;
        public ref HorizontalAlignment HorizontalAlignment => ref _horizontalAlignment;
        public ref VerticalAlignment VerticalAlignment => ref _verticalAlignment;
        public ref RectF Margin => ref _margin;
        public ref RectF Padding => ref _padding;

        private ControlLayouterInternal()
        {
        }


        internal static ControlLayouterInternal Create()
        {
            if(_pooled is null) {
                Debug.Assert(_pooledCount == 0);
                return new ControlLayouterInternal();
            }
            else {
                Debug.Assert(_pooledCount > 0);
                _pooledCount--;
                var ret = _pooled;
                _pooled = ret._nextPooled;
                ret._nextPooled = null;
                return ret;
            }
        }

        internal static void Return([MaybeNull] ref ControlLayouterInternal instance)
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
