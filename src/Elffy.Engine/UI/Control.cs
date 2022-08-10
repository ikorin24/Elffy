#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Elffy.InputSystem;
using Elffy.Shading;
using Elffy.Features.Internal;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Threading;
using Elffy.Markup;

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
    [MarkupConstructor("new ${type}(); ${addTask}(${caller}.Children.Add(${obj}))")]
    public abstract partial class Control
    {
        private readonly UIRenderable _renderable;
        private ControlLayoutInfoInternal? _layoutInfo;
        private Control? _parent;
        private RootPanel? _root;
        private ArrayPooledListCore<Control> _childrenCore;
        private Color4 _background;
        private Vector4 _cornerRadius;
        private Vector4 _borderWidth;
        private Color4 _borderColor;
        private bool _isHitTestVisible;
        private bool _isMouseOver;
        private bool _isMouseOverPrevious;

        private EventSource<Control>? _mouseEnter;
        private EventSource<Control>? _mouseLeave;
        private EventSource<Control>? _dead;

        public Event<Control> MouseEnter => new Event<Control>(ref _mouseEnter);
        public Event<Control> MouseLeave => new Event<Control>(ref _mouseLeave);
        public Event<Control> Dead => new Event<Control>(ref _dead);

        internal ref ArrayPooledListCore<Control> ChildrenCore => ref _childrenCore;

        internal UIRenderable Renderable => _renderable;

        /// <summary>Get life state</summary>
        public LifeState LifeState => _renderable.LifeState;

        public ControlCollection Children => new(this);

        public Control? Parent => _parent;

        public RootPanel? Root => _root;

        public bool IsRoot => ReferenceEquals(_root, this);

        public UIRenderingShader? Shader
        {
            get => SafeCast.As<UIRenderingShader>(_renderable.ShaderInternal);
            set => _renderable.ShaderInternal = value;
        }

        public ref Color4 Background => ref _background;

        public ref Vector4 BorderWidth => ref _borderWidth;

        /// <summary>Get absolute position of the control.</summary>
        public Vector2 ActualPosition
        {
            get => _renderable.Position.Xy;
            internal set => _renderable.Position.RefXy() = value;
        }

        public RectF ActualRect => new RectF(ActualPosition, ActualSize);

        /// <summary>Get actual width of <see cref="Control"/></summary>
        public float ActualWidth => _renderable.Scale.X;

        /// <summary>Get actual height of <see cref="Control"/></summary>
        public float ActualHeight => _renderable.Scale.Y;

        /// <summary>Get actual size of <see cref="Control"/></summary>
        public Vector2 ActualSize
        {
            get => _renderable.Scale.Xy;
            internal set => _renderable.Scale.RefXy() = value;
        }

        /// <summary>get or set whether this <see cref="Control"/> is enabled in HitTest</summary>
        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set => _isHitTestVisible = value;
        }

        /// <summary>get whether the mouse is over this <see cref="Control"/></summary>
        public bool IsMouseOver => _isMouseOver;

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible
        {
            get => _renderable.IsVisible;
            set => _renderable.IsVisible = value;
        }

        public ref Vector4 CornerRadius => ref _cornerRadius;

        public Vector4 ActualCornerRadius => CalcActualCornerRadius(ActualSize, CornerRadius);

        public IHostScreen? Screen => _renderable.Screen;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            _isHitTestVisible = true;
            _layoutInfo = ControlLayoutInfoInternal.Create();
            _renderable = new UIRenderable(this);
        }

        public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen) => _renderable.TryGetScreen(out screen);

        public IHostScreen GetValidScreen() => _renderable.GetValidScreen();

        internal void DoUIEvent() => OnUIEvent();

        protected virtual void OnUIEvent()
        {
            if(_isMouseOver && !_isMouseOverPrevious) {
                try {
                    _mouseEnter?.Invoke(this);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw, ignore exceptions in user code.
                }
            }
            if(!_isMouseOver && _isMouseOverPrevious) {
                try {
                    _mouseLeave?.Invoke(this);
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

        internal bool HitTest(Mouse mouse) => HitTest(mouse.Position);

        internal bool HitTest(Vector2 pos)
        {
            if(IsVisible == false || IsHitTestVisible == false) {
                return false;
            }
            var rect = ActualRect;
            var inRect = rect.Contains(pos);

            // corner radius of (left-top, right-top, right-bottom, left-bottom)
            var radius = ActualCornerRadius;
            if(radius.IsZero) {
                return inRect;
            }

            if(inRect == false) {
                return false;
            }
            // position based on left-top of the control
            var p = pos - rect.Position;

            var c1 = new Vector2(radius.X, radius.X);
            if(p.X < c1.X && p.Y < c1.Y) {
                return (p - c1).Length <= radius.X;
            }
            var c2 = new Vector2(rect.Width - radius.Y, radius.Y);
            if(p.X > c2.X && p.Y < c2.Y) {
                return (p - c2).Length <= radius.Y;
            }
            var c3 = new Vector2(rect.Width - radius.Z, rect.Height - radius.Z);
            if(p.X > c3.X && p.Y > c3.Y) {
                return (p - c3).Length <= radius.Z;
            }
            var c4 = new Vector2(radius.W, rect.Height - radius.W);
            if(p.X < c4.X && p.Y > c4.Y) {
                return (p - c4).Length <= radius.W;
            }

            return true;
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
            if(layer.TryGetScreen(out var screen) == false) {
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
            if(lifeState == LifeState.New || lifeState >= LifeState.Terminating) {
                throw new InvalidOperationException();
            }
            var layerLifeState = layer.LifeState;
            if(layerLifeState == LifeState.New || layerLifeState >= LifeState.Terminating) {
                throw new InvalidOperationException();
            }
            return (screen, layer, root);
        }

        private (IHostScreen Screen, UILayer Layer, int Index) CheckArgAndStateForRemoveChild(Control childToRemove)
        {
            ArgumentNullException.ThrowIfNull(childToRemove);
            if(TryGetScreen(out var screen) == false) {
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
            if(childState == LifeState.New || childState >= LifeState.Terminating) {
                throw new InvalidOperationException();
            }
            var parent = childToRemove.Parent;
            if(parent != this) {
                throw new ArgumentException($"The specified child is not a member of children of the parent.");
            }
            var lifeState = LifeState;
            if(lifeState == LifeState.New || lifeState >= LifeState.Terminating) {
                throw new InvalidOperationException();
            }
            var layerState = layer.LifeState;
            if(layerState == LifeState.New || layerState >= LifeState.Terminating) {
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
            if(TryGetScreen(out var screen) == false) {
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
            if(lifeState == LifeState.New || lifeState >= LifeState.Terminating) {
                throw new InvalidOperationException();
            }
            var layerState = layer.LifeState;
            if(layerState == LifeState.New || layerState >= LifeState.Terminating) {
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
            return ActivateChild(control._renderable, root, layer, screen);

            static async UniTask ActivateChild(UIRenderable controlRenderable, RootPanel root, UILayer layer, IHostScreen screen)
            {
                var activationTimingPoint = screen.Timings.FrameInitializing;
                await controlRenderable.ActivateOnLayerWithoutCheck(layer, activationTimingPoint, screen, CancellationToken.None);
                Debug.Assert(screen.CurrentTiming == CurrentFrameTiming.FrameInitializing);
                root.RequestRelayout();
                await screen.Timings.Update.NextOrNow();
            }
        }

        internal async UniTask RemoveChild(Control child)
        {
            var (screen, layer, index) = CheckArgAndStateForRemoveChild(child);
            await ParallelOperation.WhenAll(
                child.ClearChildren(),
                RemoveOnlyChild(this, child, index, layer, screen));
            await screen.Timings.Update.NextOrNow();
            return;

            static async UniTask RemoveOnlyChild(Control control, Control child, int index, UILayer layer, IHostScreen screen)
            {
                var terminationTimingPoint = screen.Timings.FrameInitializing;
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

        protected virtual void OnDead() => _dead?.Invoke(this);

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

        private static Vector4 CalcActualCornerRadius(in Vector2 controlActualSize, in Vector4 cornerRadius)
        {
            var (width, height) = controlActualSize;
            // avoid zero-dividing
            if(width == 0 || height == 0) {
                return Vector4.Zero;
            }
            var (lt, rt, rb, lb) = cornerRadius;
            lt = MathF.Max(0, lt);
            rt = MathF.Max(0, rt);
            rb = MathF.Max(0, rb);
            lb = MathF.Max(0, lb);

            const float Epsilon = 1e-4f;    // for avoiding zero-dividing
            var lRounded = lt + lb + Epsilon;
            var tRounded = lt + rt + Epsilon;
            var rRounded = rt + rb + Epsilon;
            var bRounded = lb + rb + Epsilon;
            if((tRounded <= width) && (bRounded <= width) && (lRounded <= height) && (rRounded <= height)) {
                return new Vector4(lt, rt, rb, lb);
            }
            var lCoef = MathF.Min(1, lRounded / height) * height / lRounded;
            var tCoef = MathF.Min(1, tRounded / width) * width / tRounded;
            var rCoef = MathF.Min(1, rRounded / height) * height / rRounded;
            var bCoef = MathF.Min(1, bRounded / width) * width / bRounded;
            return new Vector4(
                lt * MathF.Min(lCoef, tCoef),
                rt * MathF.Min(rCoef, tCoef),
                rb * MathF.Min(rCoef, bCoef),
                lb * MathF.Min(lCoef, bCoef));
        }
    }
}
