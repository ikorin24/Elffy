#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    public static class ControlLayoutHelper
    {
        /// <summary>Layout the specified <see cref="Control"/> and update <see cref="Control.ActualSize"/> and <see cref="Control.ActualPosition"/>.</summary>
        /// <param name="control"></param>
        public static void LayoutSelf(Control control)
        {
            LayoutSelf(control, ControlLayouter.Default);
        }

        public static void LayoutSelf(Control control, ControlLayouter layouter)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(layouter is null) {
                ThrowArg($"Control layouter is null. ({control.GetType().FullName})");
            }
            if(control.IsRoot) { return; }

            var rect = layouter.MesureAbsoluteRect(control);
            control.ActualSize = rect.Size;
            control.ActualPosition = rect.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LayoutChildrenRecursively(Control control)
        {
            if(control is null) {
                ThrowNullArg(nameof(control));
            }
            control.InvokeLayoutChildreRecursively();
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowArg(string message) => throw new ArgumentException(message);
    }

    public class ControlLayouter
    {
        private static ControlLayouter _default = new ControlLayouter();

        public static ControlLayouter Default => _default;

        internal RectF MesureAbsoluteRect(Control target)
        {
            Debug.Assert(target is not RootPanel);
            var parent = target.Parent;
            Debug.Assert(parent is not null);
            Debug.Assert(parent.ActualSize.X >= 0f && parent.ActualSize.Y >= 0f);

            var contentArea = MesureContentArea(parent, target);
            var rect = DecideRect(target, contentArea);
            return new RectF(rect.Position + parent.ActualPosition, rect.Size);
        }

        protected virtual ContentAreaInfo MesureContentArea(Control parent, Control target)
        {
            return new ContentAreaInfo(Vector2.Zero, parent.ActualSize, parent.Padding);
        }

        protected virtual RectF DecideRect(Control target, in ContentAreaInfo contentAreaInfo)
        {
            var layoutInfo = target.LayoutInfo;

            var contentAreaSize = new Vector2(MathF.Max(0, contentAreaInfo.ContentArea.Width), MathF.Max(0, contentAreaInfo.ContentArea.Height));

            ref readonly var contentPadding = ref contentAreaInfo.ContentPadding;
            ref var margin = ref layoutInfo.Margin;
            ref var layoutWidth = ref layoutInfo.Width;
            ref var layoutHeight = ref layoutInfo.Height;
            ref var horizontalAlignment = ref layoutInfo.HorizontalAlignment;
            ref var verticalAlignment = ref layoutInfo.VerticalAlignment;
            var availableSize = new Vector2(MathF.Max(0, contentAreaSize.X - contentPadding.Left - contentPadding.Right),
                                            MathF.Max(0, contentAreaSize.Y - contentPadding.Top - contentPadding.Bottom));
            var maxSize = new Vector2(MathF.Max(0, availableSize.X - margin.Left - margin.Right),
                                      MathF.Max(0, availableSize.Y - margin.Top - margin.Bottom));

            // Calc size
            Vector2 size;
            switch(layoutWidth.Type) {
                case LayoutLengthType.Length:
                default:
                    size.X = MathF.Max(0, MathF.Min(maxSize.X, layoutWidth.Value));
                    break;
                case LayoutLengthType.Proportion:
                    size.X = MathF.Max(0, MathF.Min(maxSize.X, layoutWidth.Value * contentAreaSize.X));
                    break;
            }
            switch(layoutHeight.Type) {
                case LayoutLengthType.Length:
                default:
                    size.Y = MathF.Max(0, MathF.Min(maxSize.Y, layoutHeight.Value));
                    break;
                case LayoutLengthType.Proportion:
                    size.Y = MathF.Max(0, MathF.Min(maxSize.Y, layoutHeight.Value * contentAreaSize.Y));
                    break;
            }

            // Calc position
            Vector2 pos;
            switch(horizontalAlignment) {
                case HorizontalAlignment.Center:
                default:
                    pos.X = contentPadding.Left + margin.Left + (maxSize.X - size.X) / 2;
                    break;
                case HorizontalAlignment.Left:
                    pos.X = contentPadding.Left + margin.Left;
                    break;
                case HorizontalAlignment.Right:
                    pos.X = contentAreaSize.X - contentPadding.Right - margin.Right - size.X;
                    break;
            }
            switch(verticalAlignment) {
                case VerticalAlignment.Center:
                default:
                    pos.Y = contentPadding.Top + margin.Top + (maxSize.Y - size.Y) / 2;
                    break;
                case VerticalAlignment.Top:
                    pos.Y = contentPadding.Top + margin.Top;
                    break;
                case VerticalAlignment.Bottom:
                    pos.Y = contentAreaSize.Y - contentPadding.Bottom - margin.Bottom - size.Y;
                    break;
            }
            pos += contentAreaInfo.ContentArea.Position;
            return new RectF(pos, size);
        }
    }

    public readonly struct ContentAreaInfo
    {
        public readonly RectF ContentArea;
        public readonly LayoutThickness ContentPadding;

        public ContentAreaInfo(in Vector2 contentPos, in Vector2 contentSize, in LayoutThickness contentPadding)
        {
            ContentArea = new RectF(contentPos, contentSize);
            ContentPadding = contentPadding;
        }

        public ContentAreaInfo(in RectF contentArea, in LayoutThickness contentPadding)
        {
            ContentArea = contentArea;
            ContentPadding = contentPadding;
        }
    }
}
