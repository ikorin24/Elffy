#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using Elffy.InputSystem;
using Elffy.Components;
using Elffy.Shading;
using Elffy.Components.Implementation;
using Elffy.Features.Internal;
using System.Threading;
using Cysharp.Threading.Tasks;

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
    public abstract partial class Control
    {
        private readonly UIRenderable _renderable;
        private ControlLayoutInfoInternal? _layoutInfo;
        private Control? _parent;
        private RootPanel? _root;
        private ArrayPooledListCore<Control> _childrenCore;
        private TextureCore _texture;
        private Color4 _background;
        private bool _isHitTestVisible;
        private bool _isMouseOver;
        private bool _isMouseOverPrevious;

        /// <summary>Mouse enter event</summary>
        public event Action<Control>? MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event Action<Control>? MouseLeave;

        public event Action<Control>? Dead;

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
        public Vector2 ActualPosition
        {
            get => Renderable.Position.Xy;
            internal set => Renderable.Position.RefXy() = value;
        }

        /// <summary>Get actual width of <see cref="Control"/></summary>
        public float ActualWidth => Renderable.Scale.X;

        /// <summary>Get actual height of <see cref="Control"/></summary>
        public float ActualHeight => Renderable.Scale.Y;

        /// <summary>Get actual size of <see cref="Control"/></summary>
        public Vector2 ActualSize
        {
            get => Renderable.Scale.Xy;
            internal set => Renderable.Scale.RefXy() = value;
        }

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

        public IHostScreen? Screen => Renderable.Screen;

        internal ref readonly TextureCore Texture => ref _texture;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            _isHitTestVisible = true;
            _layoutInfo = ControlLayoutInfoInternal.Create();
            _renderable = new UIRenderable(this);
            var textureConfig = new TextureConfig(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            _texture = new TextureCore(textureConfig);
        }

        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            return Renderable.TryGetHostScreen(out screen);
        }

        internal void DoUIEvent() => OnUIEvent();

        protected virtual void OnUIEvent()
        {
            if(_isMouseOver && !_isMouseOverPrevious) {
                try {
                    MouseEnter?.Invoke(this);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw, ignore exceptions in user code.
                }
            }
            if(!_isMouseOver && _isMouseOverPrevious) {
                try {
                    MouseLeave?.Invoke(this);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw, ignore exceptions in user code.
                }
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
                _texture.LoadUndefined((Vector2i)ActualSize);
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
                _texture.LoadUndefined((Vector2i)ActualSize);
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
        [Obsolete("", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
            var pos = ActualPosition;
            return pos.X <= mousePos.X &&
                   mousePos.X < pos.X + ActualWidth &&
                   pos.Y <= mousePos.Y &&
                   mousePos.Y < pos.Y + ActualHeight;
        }

        /// <summary>Notify the result of the hit test</summary>
        /// <param name="isHit">the result of the hit test</param>
        internal void NotifyHitTestResult(bool isHit)
        {
            _isMouseOverPrevious = _isMouseOver;
            _isMouseOver = isHit;
        }

        internal UniTask AddedToListCallback(Control parent, CancellationToken ct)
        {
            Debug.Assert(parent is not null);
            Debug.Assert(parent.Root is not null);
            Debug.Assert(LifeState == LifeState.New);
            var root = parent.Root;
            if(root.TryGetHostScreen(out var screen) == false) {
                throw new InvalidOperationException();
            }
            _parent = parent;
            _root = root;
            return Renderable.ActivateOnLayer(root.UILayer, screen.TimingPoints.Update, ct);
        }

        internal UniTask RemovedFromListCallback()
        {
            _parent = null;
            _root = null;
            var timingPoint = TryGetHostScreen(out var screen) ? screen.TimingPoints.Update : null;
            return Renderable.TerminateFromLayer(timingPoint);
        }

        protected virtual void OnDead() => Dead?.Invoke(this);

        internal void OnRenderableDead()
        {
            try {
                OnDead();
            }
            finally {
                Debug.Assert(_layoutInfo is not null);
                ControlLayoutInfoInternal.Return(ref _layoutInfo);
            }
        }
    }
}
