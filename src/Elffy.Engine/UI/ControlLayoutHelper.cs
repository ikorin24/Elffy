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
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(control.IsRoot) { return; }
            Debug.Assert(control is not RootPanel);
            var parent = control.Parent;
            Debug.Assert(parent is not null);
            Debug.Assert(parent.ActualSize.X >= 0f && parent.ActualSize.Y >= 0f);

            LayoutSelfPrivate(control, parent, DefaultResolver.ContentAreaResolver, null, DefaultResolver.ChildLayoutResolver, null);
        }

        public static void LayoutSelf<T>(Control control, ControlContentAreaResolver<T> contentAreaResolver, T state)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(control.IsRoot) { return; }
            if(contentAreaResolver is null) { ThrowNullArg(nameof(contentAreaResolver)); }
            Debug.Assert(control is not RootPanel);
            var parent = control.Parent;
            Debug.Assert(parent is not null);
            Debug.Assert(parent.ActualSize.X >= 0f && parent.ActualSize.Y >= 0f);

            LayoutSelfPrivate(control, parent, contentAreaResolver, state, DefaultResolver.ChildLayoutResolver, null);
        }

        public static void LayoutSelf<T>(Control control, ControlChildLayoutResolver<T> childLayoutResolver, T state)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(control.IsRoot) { return; }
            if(childLayoutResolver is null) { ThrowNullArg(nameof(childLayoutResolver)); }
            Debug.Assert(control is not RootPanel);
            var parent = control.Parent;
            Debug.Assert(parent is not null);
            Debug.Assert(parent.ActualSize.X >= 0f && parent.ActualSize.Y >= 0f);

            LayoutSelfPrivate(control, parent, DefaultResolver.ContentAreaResolver, null, childLayoutResolver, state);
        }

        public static void LayoutSelf<T1, T2>(Control control,
                                              ControlContentAreaResolver<T1> contentAreaResolver, T1 state1,
                                              ControlChildLayoutResolver<T2> childLayoutResolver, T2 state2)
        {
            if(control is null) {
                ThrowNullArg(nameof(control));
            }
            if(contentAreaResolver is null) {
                ThrowNullArg(nameof(contentAreaResolver));
            }
            if(childLayoutResolver is null) {
                ThrowNullArg(nameof(childLayoutResolver));
            }
            if(control.IsRoot) {
                return;
            }
            Debug.Assert(control is not RootPanel);
            var parent = control.Parent;
            Debug.Assert(parent is not null);
            LayoutSelfPrivate(control, parent, contentAreaResolver, state1, childLayoutResolver, state2);
        }

        private static void LayoutSelfPrivate<T1, T2>(Control control, Control parent,
            ControlContentAreaResolver<T1> contentAreaResolver, T1 state1,
            ControlChildLayoutResolver<T2> childLayoutResolver, T2 state2)
        {
            var (areaSize, offsetPos, areaPadding) = contentAreaResolver.Invoke(parent, state1);
            var (size, relativePosInParent) = childLayoutResolver.Invoke(control, areaSize, offsetPos, areaPadding, state2);
            control.ActualSize = size;
            control.ActualPosition = relativePosInParent + parent.ActualPosition;
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

        private sealed class DefaultResolver
        {
            private static readonly DefaultResolver _instance = new DefaultResolver();

            public static readonly ControlContentAreaResolver<object?> ContentAreaResolver = _instance.DefaultContentAreaResolver;
            public static readonly ControlChildLayoutResolver<object?> ChildLayoutResolver = _instance.DefaultChildLayoutResolver;


            private (Vector2 ContentSize, Vector2 ContentPosInParent, LayoutThickness ContentPadding) DefaultContentAreaResolver(Control parent, object? _)
            {
                return (parent.ActualSize, Vector2.Zero, parent.Padding);
            }

            private (Vector2 Size, Vector2 Position) DefaultChildLayoutResolver(Control control, Vector2 contentAreaSize, Vector2 contentPosInParent, LayoutThickness contentPadding, object? _)
            {
                var layoutInfo = control.LayoutInfo;

                contentAreaSize.X = MathF.Max(0, contentAreaSize.X);
                contentAreaSize.Y = MathF.Max(0, contentAreaSize.Y);
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
                pos += contentPosInParent;

                return (size, pos);
            }
        }
    }

    public delegate (Vector2 ContentSize, Vector2 ContentPosInParent, LayoutThickness ContentPadding) ControlContentAreaResolver<T>(Control parent, T state);
    public delegate (Vector2 Size, Vector2 Position) ControlChildLayoutResolver<T>(Control control, Vector2 contentAreaSize, Vector2 contentPosInParent, LayoutThickness contentPadding, T state);
}
