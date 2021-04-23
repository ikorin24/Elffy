#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elffy.InputSystem;
using Elffy.Components;
using Elffy.Core;
using Elffy.Shading;
using System.Diagnostics;
using Elffy.OpenGL;

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
        public Vector2i Position
        {
            get => (Vector2i)Renderable.Position.Xy;

            #region Removed
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //set
            //{
            //    ref var pos = ref Renderable.Position;
            //    var vec = value - (Vector2i)pos.Xy;

            //    if(vec.X != 0 && vec.Y != 0) {
            //        // change both x and y

            //        pos.X += vec.X;
            //        pos.Y += vec.Y;
            //        _absolutePosition += vec;
            //        if(_childrenCore.Count > 0) {
            //            ApplyRecursively(this, in vec);
            //        }
            //    }
            //    else if(vec.X != 0) {
            //        // change only x
            //        pos.X += vec.X;
            //        _absolutePosition.X += vec.X;
            //        if(_childrenCore.Count > 0) {
            //            ApplyXRecursively(this, vec.X);
            //        }
            //    }
            //    else if(vec.Y != 0) {
            //        // change only y
            //        pos.Y += vec.Y;
            //        _absolutePosition.Y += vec.Y;
            //        if(_childrenCore.Count > 0) {
            //            ApplyYRecursively(this, vec.Y);
            //        }
            //    }

            //    [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            //    static void ApplyRecursively(Control parent, in Vector2i vec)
            //    {
            //        foreach(var child in parent._childrenCore.AsSpan()) {
            //            child._absolutePosition += vec;
            //            ApplyRecursively(child, vec);
            //        }
            //    }

            //    [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            //    static void ApplyXRecursively(Control parent, int diff)
            //    {
            //        foreach(var child in parent._childrenCore.AsSpan()) {
            //            child._absolutePosition.X += diff;
            //            ApplyXRecursively(child, diff);
            //        }
            //    }

            //    [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            //    static void ApplyYRecursively(Control parent, int diff)
            //    {
            //        foreach(var child in parent._childrenCore.AsSpan()) {
            //            child._absolutePosition.Y += diff;
            //            ApplyYRecursively(child, diff);
            //        }
            //    }
            //}
            #endregion
        }

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

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible { get => Renderable.IsVisible; set => Renderable.IsVisible = value; }

        public bool HasTexture => _texture.IsEmpty == false;
        internal ref readonly TextureObject Texture => ref _texture.Texture;
        public ref readonly Vector2i ActualTextureSize => ref _texture.Size;

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
        public ref Vector2i TextureSize => ref LayouterPrivate.TextureSize;
        public ref LayoutThickness TextureFixedArea => ref LayouterPrivate.TextureFixedArea;

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
            _texture = new TextureCore(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            SetSize(Vector2.Zero);
            Renderable.Activated += OnRenderableActivatedPrivate;
            Renderable.Dead += OnRenderableDeadPrivate;
        }

        internal void SetSize(in Vector2 size)
        {
            ref var scale = ref Renderable.Scale;
            scale.X = size.X;
            scale.Y = size.Y;
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
            return GetPainter(new RectI(default, TextureSize), copyFromOriginal);
        }

        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            if(_texture.IsEmpty) {
                return CreateAndGetPainter(rect, copyFromOriginal);
            }
            else {
                return _texture.GetPainter(rect, copyFromOriginal);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            TexturePainter CreateAndGetPainter(in RectI rect, bool copyFromOriginal = true)     // capture 'this'
            {
                ref var textureSize = ref TextureSize;
                if(textureSize.X <= 0 || textureSize.Y <= 0) {
                    throw new InvalidOperationException($"{nameof(TextureSize)} must be positive value.");
                }
                _texture.LoadUndefined(textureSize);
                var p = _texture.GetPainter(rect, false);
                if(copyFromOriginal) {
                    p.Fill(ColorByte.Transparent);
                }
                return p;
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
