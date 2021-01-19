#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public readonly ref struct ControlLayouter
    {
        private readonly ControlLayouterInternal _l;

        public LayoutLength Width { get => _l.Width; set => _l.Height = value; }
        public LayoutLength Height { get => _l.Height; set => _l.Height = value; }
        public TransformOrigin TransformOrigin { get => _l.TransformOrigin; set => _l.TransformOrigin = value; }
        public HorizontalAlignment HorizontalAlignment { get => _l.HorizontalAlignment; set => _l.HorizontalAlignment = value; }
        public VerticalAlignment VerticalAlignment { get => _l.VerticalAlignment; set => _l.VerticalAlignment = value; }
        public RectF Margin { get => _l.Margin; set => _l.Margin = value; }
        public RectF Padding { get => _l.Padding; set => _l.Padding = value; }

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

        public LayoutLength Width { get; set; }
        public LayoutLength Height { get; set; }
        public TransformOrigin TransformOrigin { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public RectF Margin { get; set; }
        public RectF Padding { get; set; }

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
