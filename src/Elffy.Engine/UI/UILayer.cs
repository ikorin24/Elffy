#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.InputSystem;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Threading;

namespace Elffy.UI
{
    public sealed class UILayer : Layer
    {
        private const float UI_FAR = 1f;
        private const float UI_NEAR = -1f;
        public const int DefaultSortNumber = 100;

        private static readonly Func<CoroutineState, (IHostScreen, UILayer), UniTask> UIEventPipelineFunc = UIEventPipeline;

        private readonly RootPanel _uiRoot;
        private Matrix4 _uiProjection;
        private bool _isHitTestEnabled;

        /// <summary>Get root panel of UI</summary>
        public RootPanel UIRoot => _uiRoot;

        /// <summary>Get or set whether hit test is enabled.</summary>
        public bool IsHitTestEnabled { get => _isHitTestEnabled; set => _isHitTestEnabled = value; }

        public UILayer(string name, int sortNumber = DefaultSortNumber) : base(name, sortNumber)
        {
            _isHitTestEnabled = true;
            _uiRoot = new RootPanel(this);
        }

        public static UniTask<UILayer> NewActivate(IHostScreen screen, string name, int sortNumber = DefaultSortNumber, CancellationToken cancellationToken = default)
        {
            return new UILayer(name, sortNumber).Activate(screen, cancellationToken);
        }

        protected override void OnAlive(IHostScreen screen)
        {
            var uiRoot = _uiRoot;
            uiRoot.Initialize();
            Coroutine.StartOrReserve(screen, (screen, this), UIEventPipelineFunc, FrameTiming.FrameInitializing);
        }

        protected override void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            view = new Matrix4(1, 0, 0, 0,
                               0, -1, 0, _uiRoot.Height,
                               0, 0, 1, 0,
                               0, 0, 0, 1);
            projection = _uiProjection;
        }

        protected override void RenderOverride(IHostScreen screen)
        {
            GL.Disable(EnableCap.DepthTest);
            base.RenderOverride(screen);
            GL.Enable(EnableCap.DepthTest);
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            var frameBufferSize = screen.FrameBufferSize;
            Matrix4.OrthographicProjection(0, frameBufferSize.X, 0, frameBufferSize.Y, UI_NEAR, UI_FAR, out _uiProjection);
            _uiRoot.SetSize((Vector2)frameBufferSize);
        }

        protected override void OnLayerTerminated()
        {
            // nop
        }

        private static async UniTask UIEventPipeline(CoroutineState coroutine, (IHostScreen Screen, UILayer Layer) state)
        {
            var (screen, layer) = state;
            while(coroutine.CanRun && layer.LifeState.IsRunning()) {
                layer.HitTest(screen.Mouse);
                layer.UIEvent();
                await screen.TimingPoints.FrameInitializing.Next();
            }
        }

        private void UIEvent()
        {
            foreach(var frameObject in Objects) {
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
                RecursiveNotifyHitTestFalse(uiRoot);
            }
            return;

            static void RecursiveMouseOverTest(Control control, Mouse mouse, ref Control? hitControl)
            {
                if(control.MouseOverTest(mouse)) {
                    hitControl = control;
                }
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveMouseOverTest(child, mouse, ref hitControl);
                }
            }

            static void RecursiveNotifyHitTestResult(Control control, Control? hitControl)
            {
                // [NOTE]
                // Span で回しているので途中でコントロールを add/remove してはいけない。
                // そのため、途中でイベントの実行等のユーザーコードを差し込める実装にしてはいけない。
                control.NotifyHitTestResult(ReferenceEquals(control, hitControl));
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveNotifyHitTestResult(child, hitControl);
                }
            }

            static void RecursiveNotifyHitTestFalse(Control control)
            {
                // [NOTE]
                // Span で回しているので途中でコントロールを add/remove してはいけない。
                // そのため、途中でイベントの実行等のユーザーコードを差し込める実装にしてはいけない。
                control.NotifyHitTestResult(false);
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveNotifyHitTestFalse(child);
                }
            }
        }
    }
}
