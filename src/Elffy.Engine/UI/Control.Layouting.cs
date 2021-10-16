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
            set => ChangeLayout(ref LayoutInfoPrivate.Width, value);
        }

        public LayoutLength Height
        {
            get => LayoutInfoPrivate.Height;
            set => ChangeLayout(ref LayoutInfoPrivate.Height, value);
        }

        public TransformOrigin TransformOrigin
        {
            get => LayoutInfoPrivate.TransformOrigin;
            set => ChangeLayout(ref LayoutInfoPrivate.TransformOrigin, value);
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => LayoutInfoPrivate.HorizontalAlignment;
            set => ChangeLayout(ref LayoutInfoPrivate.HorizontalAlignment, value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => LayoutInfoPrivate.VerticalAlignment;
            set => ChangeLayout(ref LayoutInfoPrivate.VerticalAlignment, value);
        }

        public LayoutThickness Margin
        {
            get => LayoutInfoPrivate.Margin;
            set => ChangeLayout(ref LayoutInfoPrivate.Margin, value);
        }

        public LayoutThickness Padding
        {
            get => LayoutInfoPrivate.Padding;
            set => ChangeLayout(ref LayoutInfoPrivate.Padding, value);
        }

        public Matrix3 RenderTransform
        {
            get => LayoutInfoPrivate.RenderTransform;
            set => ChangeLayout(ref LayoutInfoPrivate.RenderTransform, value);
        }

        public Vector2 RenderTransformOrigin
        {
            get => LayoutInfoPrivate.RenderTransformOrigin;
            set => ChangeLayout(ref LayoutInfoPrivate.RenderTransformOrigin, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeLayoutChildreRecursively() => OnLayoutChildreRecursively();

        protected virtual void OnLayoutChildreRecursively()
        {
            foreach(var child in Children.AsSpan()) {
                ControlLayoutHelper.LayoutSelf(child);
                ControlLayoutHelper.LayoutChildrenRecursively(child);
            }
        }

        private void ChangeLayout<T>(ref T field, T value)
        {
            if(EqualityComparer<T>.Default.Equals(field, value)) {
                return;
            }
            field = value;
            _root?.RequestRelayout();
        }
    }
}
