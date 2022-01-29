#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Elffy.InputSystem;
using Elffy.Components;
using Elffy.Shading;
using Elffy.Components.Implementation;
using Elffy.Features.Internal;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;

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

        private (IHostScreen Screen, UILayer Layer, RootPanel Root) CheckArgAndStateForAddChild(Control childToAdd)
        {
            // Check arguments and states
            ArgumentNullException.ThrowIfNull(childToAdd);
            if(_renderable.TryGetUILayer(out var layer) == false) {
                throw new InvalidOperationException("The parent has no layer.");
            }
            if(layer.TryGetHostScreen(out var screen) == false) {
                throw new InvalidOperationException($"The parent has no {nameof(IHostScreen)}.");
            }
            var root = layer.UIRoot;
            var currentContext = Engine.CurrentContext;
            if(screen != currentContext) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            var childState = childToAdd.LifeState;
            if(childState != LifeState.New) {
                throw new InvalidOperationException($"The state of the child to add must be '{nameof(LifeState.New)}'");
            }
            if(childToAdd._parent is not null) {
                throw new ArgumentException($"The specified child already has a parent control.");
            }
            var lifeState = LifeState;
            if(lifeState == LifeState.New || lifeState.IsSameOrAfter(LifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            var layerLifeState = layer.LifeState;
            if(layerLifeState == LayerLifeState.New || layerLifeState.IsSameOrAfter(LayerLifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            return (screen, layer, root);
        }

        private (IHostScreen Screen, UILayer Layer, int Index) CheckArgAndStateForRemoveChild(Control childToRemove)
        {
            ArgumentNullException.ThrowIfNull(childToRemove);
            if(TryGetHostScreen(out var screen) == false) {
                throw new InvalidOperationException();
            }
            var currentContext = Engine.CurrentContext;
            if(screen != currentContext) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(_renderable.TryGetUILayer(out var layer) == false) {
                throw new InvalidOperationException();
            }
            var childState = childToRemove.LifeState;
            if(childState == LifeState.New || childState.IsSameOrAfter(LifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            var parent = childToRemove.Parent;
            if(parent != this) {
                throw new ArgumentException($"The specified child is not a member of children of the parent.");
            }
            var lifeState = LifeState;
            if(lifeState == LifeState.New || lifeState.IsSameOrAfter(LifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            var layerState = layer.LifeState;
            if(layerState == LayerLifeState.New || layerState.IsSameOrAfter(LayerLifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            var root = _root;
            if(root is null) {
                throw new InvalidOperationException();
            }
            if(childToRemove._root != root) {
                throw new ArgumentException();
            }
            Debug.Assert(screen is not null);
            if(childToRemove.Screen != screen) {
                throw new ArgumentException();
            }
            Debug.Assert(layer is not null);
            if(childToRemove._renderable.Layer != layer) {
                throw new ArgumentException();
            }
            var index = _childrenCore.IndexOf(childToRemove);
            if(index < 0) {
                throw new ArgumentException();
            }
            return (screen, layer, index);
        }

        private void CheckStateForClearChildren()
        {
            if(TryGetHostScreen(out var screen) == false) {
                throw new InvalidOperationException();
            }
            var currentContext = Engine.CurrentContext;
            if(screen != currentContext) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(_renderable.TryGetUILayer(out var layer) == false) {
                throw new InvalidOperationException();
            }
            var lifeState = LifeState;
            if(lifeState == LifeState.New || lifeState.IsSameOrAfter(LifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            var layerState = layer.LifeState;
            if(layerState == LayerLifeState.New || layerState.IsSameOrAfter(LayerLifeState.Terminating)) {
                throw new InvalidOperationException();
            }
            var root = _root;
            if(root is null) {
                throw new InvalidOperationException();
            }
        }

        internal UniTask AddChild(Control control)
        {
            var (screen, layer, root) = CheckArgAndStateForAddChild(control);

            control._parent = this;
            control._root = root;
            _childrenCore.Add(control);
            return ActivateChild(control.Renderable, root, layer, screen);

            static async UniTask ActivateChild(UIRenderable controlRenderable, RootPanel root, UILayer layer, IHostScreen screen)
            {
                var activationTimingPoint = screen.TimingPoints.FrameInitializing;
                await controlRenderable.ActivateOnLayerWithoutCheck(layer, activationTimingPoint, screen, CancellationToken.None);
                Debug.Assert(screen.CurrentTiming == CurrentFrameTiming.FrameInitializing);
                root.RequestRelayout();
                await screen.TimingPoints.Update.NextOrNow();
            }
        }

        internal async UniTask RemoveChild(Control child)
        {
            var (screen, layer, index) = CheckArgAndStateForRemoveChild(child);
            await ParallelOperation.WhenAll(
                child.ClearChildren(),
                RemoveOnlyChild(this, child, index, layer, screen));
            await screen.TimingPoints.Update.NextOrNow();
            return;

            static async UniTask RemoveOnlyChild(Control control, Control child, int index, UILayer layer, IHostScreen screen)
            {
                var terminationTimingPoint = screen.TimingPoints.FrameInitializing;
                child._parent = null;
                child._root = null;
                control._childrenCore.RemoveAt(index);
                await child._renderable.TerminateFromLayerWithoutCheck(layer, terminationTimingPoint, screen);
                Debug.Assert(screen.CurrentTiming == CurrentFrameTiming.FrameInitializing);
                layer.UIRoot.RequestRelayout();
            }
        }

        internal UniTask ClearChildren()
        {
            CheckStateForClearChildren();
            var childCount = _childrenCore.Count;
            if(childCount == 0) {
                return UniTask.CompletedTask;
            }
            return ParallelOperation.WhenAll(childCount, this, static (Span<UniTask> tasks, in Control self) =>
            {
                // [NOTE] I remove children in reverse order because it is faster.
                var children = self._childrenCore.AsSpan();
                for(int i = 0; i < tasks.Length; i++) {
                    tasks[i] = self.RemoveChild(children[tasks.Length - 1 - i]);
                }
            });
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
