#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    partial class Control
    {
        private ControlLayoutInfoInternal LayoutInfoPrivate => _layoutInfo ?? ControlLayoutInfoInternal.ThrowCannotGetInstance();

        public ControlLayoutInfo LayoutInfo => new ControlLayoutInfo(LayoutInfoPrivate);

        public LayoutLength Width
        {
            get => LayoutInfoPrivate.Width;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.Width, value);
        }

        public LayoutLength Height
        {
            get => LayoutInfoPrivate.Height;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.Height, value);
        }

        public TransformOrigin TransformOrigin
        {
            get => LayoutInfoPrivate.TransformOrigin;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.TransformOrigin, value);
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => LayoutInfoPrivate.HorizontalAlignment;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.HorizontalAlignment, value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => LayoutInfoPrivate.VerticalAlignment;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.VerticalAlignment, value);
        }

        public LayoutThickness Margin
        {
            get => LayoutInfoPrivate.Margin;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.Margin, value);
        }

        public LayoutThickness Padding
        {
            get => LayoutInfoPrivate.Padding;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.Padding, value);
        }

        public Matrix3 RenderTransform
        {
            get => LayoutInfoPrivate.RenderTransform;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.RenderTransform, value);
        }

        public Vector2 RenderTransformOrigin
        {
            get => LayoutInfoPrivate.RenderTransformOrigin;
            set => ChangeLayoutInfoField(ref LayoutInfoPrivate.RenderTransformOrigin, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeLayoutChildreRecursively(ControlLayoutContext context) => OnLayoutChildreRecursively(context);

        protected virtual void OnLayoutChildreRecursively(ControlLayoutContext context)
        {
            var layouter = ControlLayouter.Default;
            foreach(var child in Children.AsSpan()) {
                context.LayoutSelf(layouter, child);
                context.LayoutChildreRecursively(child);
            }
        }

        private void ChangeLayoutInfoField<T>(ref T field, T value)
        {
            if(EqualityComparer<T>.Default.Equals(field, value)) {
                return;
            }
            field = value;
            _root?.RequestRelayout();
        }
    }
}
