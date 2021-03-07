#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elffy.InputSystem;
using Elffy.Components;
using Elffy.Core;
using Elffy.Shading;
using System.Diagnostics;

namespace Elffy.UI
{
    // [Rendering]
    // 
    // Control and UIRenderable are always a pair.
    // Control is an element of the UI, which forms a UI tree. (UI tree means logical tree and visual tree.)
    // UIRenderable is a subclass of Renderable (that means FrameObject),
    // which is managed by the engine and renders the control.
    // Control is just a data structure, so it doesn't know how it is rendered on the screen.
    // On the other hand, `UIRenderable` doesn't know UI tree.
    // They don't know each other well, and they shouldn't.
    // Control forms a UI tree, but UIRenderable always exists as a root in the tree of Positionable.
    // They are independent from each other.
    //
    //
    // [UIRenderable object]
    // 
    // UIRenderable is an internal class, and subclass of FrameObject.
    // It is always on the internal layer when it is activated, and users can not access the instance.
    // Don't put the instance public even if as Renderable or FrameObject.

    /// <summary>Base class of UI element, which forms a UI tree and provides hit test.</summary>
    public abstract class Control
    {
        private readonly UIRenderable _renderable;
        private ControlLayouterInternal? _layouter;
        private Control? _parent;
        private RootPanel? _root;
        private Vector2 _absolutePosition;
        private Vector2i _offset;
        private ArrayPooledListCore<Control> _childrenCore;
        private TextureCore _texture;
        private Color4 _background;
        private bool _isHitTestVisible;
        private bool _isMouseOver;

        protected private ControlLayouterInternal Layouter => _layouter ?? ControlLayouterInternal.ThrowCannotGetInstance();

        internal ref ArrayPooledListCore<Control> ChildrenCore => ref _childrenCore;

        internal UIRenderable Renderable => _renderable;

        /// <summary>Get life state</summary>
        public LifeState LifeState => _renderable.LifeState;

        public ControlCollection Children => new(this);

        public Control? Parent => _parent;

        public RootPanel? Root => _root;

        public bool IsRoot => ReferenceEquals(_root, this);

        public UIShaderSource? Shader
        {
            get => SafeCast.As<UIShaderSource>(_renderable.ShaderInternal);
            set => _renderable.ShaderInternal = value;
        }

        public ref Color4 Background => ref _background;

        public Vector2i Position
        {
            get => (Vector2i)Renderable.Position.Xy;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ref var pos = ref Renderable.Position;
                var vec = value - (Vector2i)pos.Xy;

                if(vec.X != 0 && vec.Y != 0) {
                    // change both x and y

                    pos.X += vec.X;
                    pos.Y += vec.Y;
                    _absolutePosition += vec;
                    if(_childrenCore.Count > 0) {
                        ApplyRecursively(this, in vec);
                    }
                }
                else if(vec.X != 0) {
                    // change only x
                    pos.X += vec.X;
                    _absolutePosition.X += vec.X;
                    if(_childrenCore.Count > 0) {
                        ApplyXRecursively(this, vec.X);
                    }
                }
                else if(vec.Y != 0) {
                    // change only y
                    pos.Y += vec.Y;
                    _absolutePosition.Y += vec.Y;
                    if(_childrenCore.Count > 0) {
                        ApplyYRecursively(this, vec.Y);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyRecursively(Control parent, in Vector2i vec)
                {
                    foreach(var child in parent._childrenCore.AsSpan()) {
                        child._absolutePosition += vec;
                        ApplyRecursively(child, vec);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyXRecursively(Control parent, int diff)
                {
                    foreach(var child in parent._childrenCore.AsSpan()) {
                        child._absolutePosition.X += diff;
                        ApplyXRecursively(child, diff);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyYRecursively(Control parent, int diff)
                {
                    foreach(var child in parent._childrenCore.AsSpan()) {
                        child._absolutePosition.Y += diff;
                        ApplyYRecursively(child, diff);
                    }
                }
            }
        }

        public Vector2i AbsolutePosition => (Vector2i)_absolutePosition;

        /// <summary>get or set Width of <see cref="Control"/></summary>
        public int Width
        {
            get => (int)Renderable.Scale.X;
            set
            {
                if(value < 0) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
                }
                Renderable.Scale.X = value;
            }
        }


        /// <summary>get or set Height of <see cref="Control"/></summary>
        public int Height
        {
            get => (int)Renderable.Scale.Y;
            set
            {
                if(value < 0) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
                }
                Renderable.Scale.Y = value;
            }
        }

        /// <summary>get or set size of <see cref="Control"/></summary>
        public Vector2i Size
        {
            get => (Vector2i)Renderable.Scale.Xy;
            set
            {
                if(value.X < 0 || value.Y < 0) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
                }
                Renderable.Scale.X = value.X;
                Renderable.Scale.Y = value.Y;
            }
        }

        /// <summary>get or set offset position of layout</summary>
        public ref Vector2i Offset => ref _offset;

        /// <summary>get or set whether this <see cref="Control"/> is enabled in HitTest</summary>
        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set => _isHitTestVisible = value;
        }

        /// <summary>get whether the mouse is over this <see cref="Control"/></summary>
        public bool IsMouseOver => _isMouseOver;

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible { get => Renderable.IsVisible; set => Renderable.IsVisible = value; }

        internal ref readonly TextureCore Texture => ref _texture;

        public ref LayoutLength LayoutWidth => ref Layouter.Width;
        public ref LayoutLength LayoutHeight => ref Layouter.Height;
        public ref TransformOrigin TransformOrigin => ref Layouter.TransformOrigin;
        public ref HorizontalAlignment HorizontalAlignment => ref Layouter.HorizontalAlignment;
        public ref VerticalAlignment VerticalAlignment => ref Layouter.VerticalAlignment;
        public ref LayoutThickness Margin => ref Layouter.Margin;
        public ref LayoutThickness Padding => ref Layouter.Padding;

        /// <summary>Mouse enter event</summary>
        public event Action<Control, MouseEventArgs>? MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event Action<Control, MouseEventArgs>? MouseLeave;

        public event Action<Control>? Activated;
        public event Action<Control>? Dead;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            _isHitTestVisible = true;
            _layouter = ControlLayouterInternal.Create();
            _renderable = new UIRenderable(this);
            Width = 30;
            Height = 30;
            _texture = new TextureCore(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            Renderable.Activated += OnRenderableActivatedPrivate;
            Renderable.Dead += OnRenderableDeadPrivate;
        }

        protected private void SetAsRootControl()
        {
            Debug.Assert(this is RootPanel);
            _root = SafeCast.As<RootPanel>(this);
        }

        protected virtual void OnRecieveHitTestResult(bool isHit, Mouse mouse)
        {
            var isMouseOverPrev = IsMouseOver;
            _isMouseOver = isHit;
            if(isHit) {
                if(isMouseOverPrev == false) {
                    MouseEnter?.Invoke(this, new MouseEventArgs(mouse.Position));
                }
            }
            else {
                if(isMouseOverPrev) {
                    MouseLeave?.Invoke(this, new MouseEventArgs(mouse.Position));
                }
            }
        }

        public TexturePainter GetPainter(bool copyFromOriginal = true)
        {
            if(_texture.IsEmpty) {
                _texture.LoadUndefined(new Vector2i(Width, Height));
                var p = _texture.GetPainter(false);
                if(copyFromOriginal) {
                    p.Fill(ColorByte.White);        // TODO: デフォルトが白じゃない場合は？
                }
                return p;
            }
            else {
                return _texture.GetPainter(copyFromOriginal);
            }
        }

        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            if(_texture.IsEmpty) {
                _texture.LoadUndefined(new Vector2i(Width, Height));
                var p = _texture.GetPainter(rect, copyFromOriginal);
                p.Fill(ColorByte.White);            // TODO: デフォルトが白じゃない場合は？
                return p;
            }
            else {
                return _texture.GetPainter(rect, copyFromOriginal);
            }
        }

        /// <summary>このオブジェクトの <see cref="Children"/> 以下に存在する全ての子孫を取得します。列挙順は深さ優先探索 (DFS; depth-first search) です。</summary>
        /// <returns>全ての子孫オブジェクト</returns>
        public IEnumerable<Control> GetOffspring()
        {
            foreach(var child in Children) {
                yield return child;
                foreach(var offspring in child.GetOffspring()) {
                    yield return offspring;
                }
            }
        }

        /// <summary>マウスオーバーしているかを取得します</summary>
        /// <param name="mouse">マウス</param>
        /// <returns>マウスオーバーしているか</returns>
        internal bool MouseOverTest(Mouse mouse)
        {
            var mousePos = (Vector2i)mouse.Position;
            return IsVisible &&
                   IsHitTestVisible &&
                   _absolutePosition.X <= mousePos.X &&
                   mousePos.X < _absolutePosition.X + Width &&
                   _absolutePosition.Y <= mousePos.Y &&
                   mousePos.Y < _absolutePosition.Y + Height;
        }

        /// <summary>ヒットテストの結果を通知します</summary>
        /// <param name="isHit">ヒットテスト結果</param>
        /// <param name="mouse">マウス</param>
        internal void NotifyHitTestResult(bool isHit, Mouse mouse)
        {
            OnRecieveHitTestResult(isHit, mouse);
        }

        internal void AddedToListCallback(Control parent)
        {
            Debug.Assert(parent is not null);
            Debug.Assert(parent.Root is not null);
            Debug.Assert(LifeState == LifeState.New);
            _parent = parent;
            _root = parent.Root;
            Renderable.Activate(_root.UILayer);
        }

        internal void RemovedFromListCallback()
        {
            _parent = null;
            _root = null;
            Renderable.Terminate();
        }

        protected virtual void OnActivated()
        {
            Activated?.Invoke(this);
        }

        protected virtual void OnDead()
        {
            Dead?.Invoke(this);
        }

        private void OnRenderableActivatedPrivate(FrameObject sender)
        {
            OnActivated();
        }

        private void OnRenderableDeadPrivate(FrameObject sender)
        {
            OnDead();
            Debug.Assert(_layouter is not null);
            ControlLayouterInternal.Return(ref _layouter);
        }

        internal void Layout(in Vector2 parentSize, in LayoutThickness parentPadding)
        {
            LayoutRecursively(parentSize, parentPadding);
        }

        protected void LayoutSelf(in Vector2 parentSize, in LayoutThickness parentPadding, out Vector2 size)
        {
            Debug.Assert(this is not RootPanel);
            Debug.Assert(_parent is not null);
            Debug.Assert(parentSize.X >= 0f && parentSize.Y >= 0f);
            var layouter = Layouter;
            ref var margin = ref layouter.Margin;
            ref var layoutWidth = ref layouter.Width;
            ref var layoutHeight = ref layouter.Height;
            ref var horizontalAlignment = ref layouter.HorizontalAlignment;
            ref var verticalAlignment = ref layouter.VerticalAlignment;
            var availableSize = new Vector2(MathF.Max(0, parentSize.X - parentPadding.Left - parentPadding.Right),
                                            MathF.Max(0, parentSize.Y - parentPadding.Top - parentPadding.Bottom));
            var maxSize = new Vector2(MathF.Max(0, availableSize.X - margin.Left - margin.Right),
                                      MathF.Max(0, availableSize.Y - margin.Top - margin.Bottom));

            // Calc size
            switch(layoutWidth.Type) {
                case LayoutLengthType.Length:
                default:
                    size.X = MathF.Max(0, MathF.Min(maxSize.X, layoutWidth.Value));
                    break;
                case LayoutLengthType.Proportion:
                    size.X = MathF.Max(0, MathF.Min(maxSize.X, layoutWidth.Value * parentSize.X));
                    break;
            }
            switch(layoutHeight.Type) {
                case LayoutLengthType.Length:
                default:
                    size.Y = MathF.Max(0, MathF.Min(maxSize.Y, layoutHeight.Value));
                    break;
                case LayoutLengthType.Proportion:
                    size.Y = MathF.Max(0, MathF.Min(maxSize.Y, layoutHeight.Value * parentSize.Y));
                    break;
            }

            // Calc position
            Vector2 pos;
            switch(horizontalAlignment) {
                case HorizontalAlignment.Center:
                default:
                    pos.X = parentPadding.Left + margin.Left + (maxSize.X - size.X) / 2;
                    break;
                case HorizontalAlignment.Left:
                    pos.X = parentPadding.Left + margin.Left;
                    break;
                case HorizontalAlignment.Right:
                    pos.X = parentSize.X - parentPadding.Right - margin.Right - size.X;
                    break;
            }
            switch(verticalAlignment) {
                case VerticalAlignment.Center:
                default:
                    pos.Y = parentPadding.Top + margin.Top + (maxSize.Y - size.Y) / 2;
                    break;
                case VerticalAlignment.Top:
                    pos.Y = parentPadding.Top + margin.Top;
                    break;
                case VerticalAlignment.Bottom:
                    pos.Y = parentSize.Y - parentPadding.Bottom - margin.Bottom - size.Y;
                    break;
            }

            Size = (Vector2i)size;
            Renderable.Position.X = pos.X;
            Renderable.Position.Y = pos.Y;
            _absolutePosition = _parent._absolutePosition + (Vector2i)pos;
        }

        protected virtual void LayoutRecursively(in Vector2 parentSize, in LayoutThickness parentPadding)
        {
            LayoutSelf(parentSize, parentPadding, out var size);
            ref var padding = ref Padding;
            foreach(var child in _childrenCore.AsSpan()) {
                child.LayoutRecursively(size, padding);
            }
        }
    }
}
