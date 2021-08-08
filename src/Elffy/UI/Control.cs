#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
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
    // Control is an element of the UI, which forms a UI tree.
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
        private ArrayPooledListCore<Control> _childrenCore;
        private TextureCore _texture;
        private Color4 _background;
        private bool _isHitTestVisible;
        private bool _isMouseOver;
        private bool _isMouseOverPrevious;

        private ControlLayouterInternal LayouterPrivate => _layouter ?? ControlLayouterInternal.ThrowCannotGetInstance();

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

        /// <summary>Get absolute position of the control.</summary>
        public Vector2i Position => (Vector2i)Renderable.Position.Xy;

        /// <summary>Get width of <see cref="Control"/></summary>
        public int Width => (int)Renderable.Scale.X;

        /// <summary>Get height of <see cref="Control"/></summary>
        public int Height => (int)Renderable.Scale.Y;

        /// <summary>Get size of <see cref="Control"/></summary>
        public Vector2i Size => (Vector2i)Renderable.Scale.Xy;      // To set size, use the internal method 'SetSize'

        /// <summary>get or set whether this <see cref="Control"/> is enabled in HitTest</summary>
        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set => _isHitTestVisible = value;
        }

        /// <summary>get whether the mouse is over this <see cref="Control"/></summary>
        public bool IsMouseOver => _isMouseOver;

        /// <summary>get <see cref="IsMouseOver"/> of the previouse frame</summary>
        public bool IsMouseOverPreviouse => _isMouseOverPrevious;

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible
        {
            get => Renderable.Visibility == RenderVisibility.Visible;
            set => Renderable.Visibility = value ? RenderVisibility.Visible : RenderVisibility.InvisibleSelf;
        }

        internal ref readonly TextureCore Texture => ref _texture;

        public ControlLayouter Layouter => new ControlLayouter(LayouterPrivate);
        public ref LayoutLength LayoutWidth => ref LayouterPrivate.Width;
        public ref LayoutLength LayoutHeight => ref LayouterPrivate.Height;
        public ref TransformOrigin TransformOrigin => ref LayouterPrivate.TransformOrigin;
        public ref HorizontalAlignment HorizontalAlignment => ref LayouterPrivate.HorizontalAlignment;
        public ref VerticalAlignment VerticalAlignment => ref LayouterPrivate.VerticalAlignment;
        public ref LayoutThickness Margin => ref LayouterPrivate.Margin;
        public ref LayoutThickness Padding => ref LayouterPrivate.Padding;
        public ref Matrix3 RenderTransform => ref LayouterPrivate.RenderTransform;
        public ref Vector2 RenderTransformOrigin => ref LayouterPrivate.RenderTransformOrigin;

        /// <summary>Mouse enter event</summary>
        public event Action<Control>? MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event Action<Control>? MouseLeave;

        public event Action<Control>? Activated;
        public event Action<Control>? Dead;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            _isHitTestVisible = true;
            _layouter = ControlLayouterInternal.Create();
            _renderable = new UIRenderable(this);
            var textureConfig = new TextureConfig(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            _texture = new TextureCore(textureConfig);
        }

        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            return Renderable.TryGetHostScreen(out screen);
        }

        internal void SetSize(in Vector2 size)
        {
            ref var scale = ref Renderable.Scale;
            scale.X = size.X;
            scale.Y = size.Y;
        }

        internal void DoUIEvent() => OnUIEvent();

        protected virtual void OnUIEvent()
        {
            if(_isMouseOver && !_isMouseOverPrevious) {
                try {
                    MouseEnter?.Invoke(this);
                }
                catch { }   // Don't throw, ignore exceptions in user code.
            }
            if(!_isMouseOver && _isMouseOverPrevious) {
                try {
                    MouseLeave?.Invoke(this);
                }
                catch { }   // Don't throw, ignore exceptions in user code.
            }
        }

        protected private void SetAsRootControl()
        {
            Debug.Assert(this is RootPanel);
            _root = SafeCast.As<RootPanel>(this);
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
            if(IsVisible == false || IsHitTestVisible == false) {
                return false;
            }
            var mousePos = (Vector2i)mouse.Position;
            var pos = Position;
            return pos.X <= mousePos.X &&
                   mousePos.X < pos.X + Width &&
                   pos.Y <= mousePos.Y &&
                   mousePos.Y < pos.Y + Height;
        }

        /// <summary>Notify the result of the hit test</summary>
        /// <param name="isHit">the result of the hit test</param>
        internal void NotifyHitTestResult(bool isHit)
        {
            _isMouseOverPrevious = _isMouseOver;
            _isMouseOver = isHit;
        }

        internal void AddedToListCallback(Control parent)
        {
            Debug.Assert(parent is not null);
            Debug.Assert(parent.Root is not null);
            Debug.Assert(LifeState == LifeState.New);
            _parent = parent;
            _root = parent.Root;
            Renderable.ActivateOnInternalLayer(_root.UILayer);
        }

        internal void RemovedFromListCallback()
        {
            _parent = null;
            _root = null;
            Renderable.Terminate();
        }

        protected virtual void OnActivated() => Activated?.Invoke(this);

        protected virtual void OnDead() => Dead?.Invoke(this);

        internal void OnRenderableActivated()
        {
            OnActivated();
        }

        internal void OnRenderableDead()
        {
            try {
                OnDead();
            }
            finally {
                Debug.Assert(_layouter is not null);
                ControlLayouterInternal.Return(ref _layouter);
            }
        }

        /// <summary>Layout itself and update <see cref="Size"/> and <see cref="Position"/>.</summary>
        public void LayoutSelf()
        {
            if(IsRoot) { return; }
            Debug.Assert(this is not RootPanel);
            Debug.Assert(_parent is not null);
            var parentSize = _parent.Size;
            ref readonly var parentPadding = ref _parent.Padding;
            Debug.Assert(parentSize.X >= 0f && parentSize.Y >= 0f);
            var (size, relativePos) = DefaultLayoutingMethod(_parent.Size, _parent.Padding, Layouter);

            // Change size, position and absolutePosition
            SetSize(size);

            ref var parentPos = ref _parent.Renderable.Position;
            ref var position = ref Renderable.Position;
            position.X = relativePos.X + parentPos.X;
            position.Y = relativePos.Y + parentPos.Y;
        }

        public void LayoutSelf<T>(ControlLayoutResolver<T> resolver, T state)
        {
            if(resolver is null) {
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(resolver));
                ThrowNullArg();
            }
            if(IsRoot) { return; }
            Debug.Assert(this is not RootPanel);
            Debug.Assert(_parent is not null);

            var (size, pos) = resolver.Invoke(this, state);
            // Change size, position and absolutePosition
            SetSize(size);

            ref var parentPos = ref _parent.Renderable.Position;
            ref var position = ref Renderable.Position;
            position.X = pos.X + parentPos.X;
            position.Y = pos.Y + parentPos.Y;
        }

        /// <summary>Layout children recursively.</summary>
        public virtual void LayoutChildren()
        {
            foreach(var child in _childrenCore.AsSpan()) {
                child.LayoutSelf();
                child.LayoutChildren();
            }
        }

        /// <summary>Layout size and relative position to the parent</summary>
        /// <param name="areaSize">Size of the area to locate the control. </param>
        /// <param name="areaPadding">Padding in the area specified by the parent control.</param>
        /// <param name="layouter">layouter of the control</param>
        /// <returns>'size' is control size. 'position' is relative position to the parent.</returns>
        protected static (Vector2 size, Vector2 position) DefaultLayoutingMethod(
            Vector2 areaSize, in LayoutThickness areaPadding, in ControlLayouter layouter)
        {
            areaSize.X = MathF.Max(0, areaSize.X);
            areaSize.Y = MathF.Max(0, areaSize.Y);
            ref var margin = ref layouter.Margin;
            ref var layoutWidth = ref layouter.Width;
            ref var layoutHeight = ref layouter.Height;
            ref var horizontalAlignment = ref layouter.HorizontalAlignment;
            ref var verticalAlignment = ref layouter.VerticalAlignment;
            var availableSize = new Vector2(MathF.Max(0, areaSize.X - areaPadding.Left - areaPadding.Right),
                                            MathF.Max(0, areaSize.Y - areaPadding.Top - areaPadding.Bottom));
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
                    size.X = MathF.Max(0, MathF.Min(maxSize.X, layoutWidth.Value * areaSize.X));
                    break;
            }
            switch(layoutHeight.Type) {
                case LayoutLengthType.Length:
                default:
                    size.Y = MathF.Max(0, MathF.Min(maxSize.Y, layoutHeight.Value));
                    break;
                case LayoutLengthType.Proportion:
                    size.Y = MathF.Max(0, MathF.Min(maxSize.Y, layoutHeight.Value * areaSize.Y));
                    break;
            }

            // Calc position
            Vector2 pos;
            switch(horizontalAlignment) {
                case HorizontalAlignment.Center:
                default:
                    pos.X = areaPadding.Left + margin.Left + (maxSize.X - size.X) / 2;
                    break;
                case HorizontalAlignment.Left:
                    pos.X = areaPadding.Left + margin.Left;
                    break;
                case HorizontalAlignment.Right:
                    pos.X = areaSize.X - areaPadding.Right - margin.Right - size.X;
                    break;
            }
            switch(verticalAlignment) {
                case VerticalAlignment.Center:
                default:
                    pos.Y = areaPadding.Top + margin.Top + (maxSize.Y - size.Y) / 2;
                    break;
                case VerticalAlignment.Top:
                    pos.Y = areaPadding.Top + margin.Top;
                    break;
                case VerticalAlignment.Bottom:
                    pos.Y = areaSize.Y - areaPadding.Bottom - margin.Bottom - size.Y;
                    break;
            }

            return (size, pos);
        }
    }

    public delegate (Vector2 size, Vector2 position) ControlLayoutResolver<T>(Control self, T state);
}
