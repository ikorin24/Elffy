#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.InputSystem;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.Threading;

namespace Elffy.UI
{
    public sealed class UIObjectLayer : ObjectLayer
    {
        private const float UI_FAR = 1f;
        private const float UI_NEAR = -1f;
        private const int DefaultSortNumber = 100;

        private readonly FBO _renderTarget;
        private readonly RootPanel _uiRoot;
        private Matrix4 _uiProjection;
        private bool _isHitTestEnabled;

        /// <summary>Get root panel of UI</summary>
        public RootPanel UIRoot => _uiRoot;

        /// <summary>Get or set whether hit test is enabled.</summary>
        public bool IsHitTestEnabled { get => _isHitTestEnabled; set => _isHitTestEnabled = value; }

        public UIObjectLayer(int sortNumber = DefaultSortNumber) : this(FBO.Empty, null, sortNumber) { }

        public UIObjectLayer(string? name, int sortNumber = DefaultSortNumber) : this(FBO.Empty, name, sortNumber) { }
        public UIObjectLayer(FBO renderTarget, int sortNumber = DefaultSortNumber) : this(renderTarget, null, sortNumber) { }

        public UIObjectLayer(FBO renderTarget, string? name, int sortNumber = DefaultSortNumber) : base(sortNumber, name)
        {
            _renderTarget = renderTarget;
            _isHitTestEnabled = true;
            _uiRoot = new RootPanel(this);
        }

        internal sealed override async UniTask ActivateOnScreen(IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken ct)
        {
            await base.ActivateOnScreen(screen, timingPoint, ct);
            await _uiRoot.Initialize(timingPoint, ct);
            Coroutine.StartOrReserve(screen, this, static async (coroutine, layer) =>
            {
                while(coroutine.CanRun && layer.LifeState.IsRunning()) {
                    layer.HitTest(coroutine.Screen.Mouse);
                    layer.UIEvent();
                    await coroutine.TimingPoints.FrameInitializing.Next();
                }
            }, FrameTiming.FrameInitializing);
        }

        protected override void OnAfterExecute(IHostScreen screen, ref FBO currentFbo)
        {
            GL.Enable(EnableCap.DepthTest);
        }

        protected override void OnBeforeExecute(IHostScreen screen, ref FBO currentFbo)
        {
            currentFbo = _renderTarget;
            FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
            GL.Disable(EnableCap.DepthTest);
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            var frameBufferSize = screen.FrameBufferSize;
            Matrix4.OrthographicProjection(0, frameBufferSize.X, 0, frameBufferSize.Y, UI_NEAR, UI_FAR, out _uiProjection);
            _uiRoot.ActualSize = (Vector2)frameBufferSize;
            _uiRoot.RequestRelayout();
        }

        protected override void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            var scale = new Vector3(1, -1, 1);
            var translation = new Vector3(0, _uiRoot.ActualHeight, 0);
            view = Matrix4.FromScaleAndTranslation(scale, translation);
            projection = _uiProjection;
        }

        private void UIEvent()
        {
            foreach(var frameObject in GetFrameObjects()) {
                SafeCast.As<UIRenderable>(frameObject).DoUIEvent();
            }
        }

        private void HitTest(Mouse mouse)
        {
            if(IsHitTestEnabled == false) { return; }
            var uiRoot = UIRoot;
            if(mouse.OnScreen) {
                // Hit control is the last control where mouse over test is true
                var hitControl = default(Control);
                RecursiveMouseOverTest(uiRoot, mouse, ref hitControl);
                RecursiveNotifyHitTestResult(uiRoot, hitControl);
            }
            else {
                RecursiveNotifyHitTestResult(uiRoot, null);
            }
            return;

            static void RecursiveMouseOverTest(Control control, Mouse mouse, ref Control? hitControl)
            {
                if(control.HitTest(mouse)) {
                    hitControl = control;
                }
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveMouseOverTest(child, mouse, ref hitControl);
                }
            }

            static void RecursiveNotifyHitTestResult(Control control, Control? hitControl)
            {
                // [NOTE]
                // Don't add or remove controls while the hit result is being notified.
                // The reason is the controls are iterated as Span<T>.
                // (In other words, user code should not be executed)

                Debug.Assert(control is not null);
                var isHit = ReferenceEquals(control, hitControl);
                control.NotifyHitTestResult(isHit);
                var children = control.ChildrenCore.AsSpan();
                foreach(var child in children) {
                    RecursiveNotifyHitTestResult(child, hitControl);
                }
            }
        }
    }
}
