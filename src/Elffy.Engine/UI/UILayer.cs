#nullable enable
using Elffy.Features.Internal;
using Elffy.InputSystem;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.UI
{
    internal sealed class UILayer : ILayer
    {
        private readonly FrameObjectStore _store;
        private readonly LayerTimingPointList _timingPoints;
        private readonly LayerCollection _owner;
        private readonly RootPanel _uiRoot;
        private bool _isVisible;
        private bool _isHitTestEnabled;

        /// <inheritdoc/>
        public int ObjectCount => _store.ObjectCount;

        /// <inheritdoc/>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        /// <inheritdoc/>
        public LayerCollection OwnerCollection => _owner;

        /// <summary>Get root panel of UI</summary>
        public RootPanel UIRoot => _uiRoot;

        public LayerTimingPointList TimingPoints => _timingPoints;

        /// <summary>Get or set whether hit test is enabled.</summary>
        public bool IsHitTestEnabled { get => _isHitTestEnabled; set => _isHitTestEnabled = value; }

        public UILayer(LayerCollection owner)
        {
            _isVisible = true;
            _isHitTestEnabled = true;
            _store = FrameObjectStore.New(64);
            _timingPoints = new LayerTimingPointList(this);
            _owner = owner;
            _uiRoot = new RootPanel(this);
        }

        /// <inheritdoc/>
        public void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(frameObject is UIRenderable);
            _store.AddFrameObject(frameObject);
        }

        /// <inheritdoc/>
        public void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        /// <inheritdoc/>
        public void ClearFrameObject() => _store.ClearFrameObject();

        public void ApplyRemove() => _store.ApplyRemove();

        public void ApplyAdd() => _store.ApplyAdd();

        public void UIEvent()
        {
            foreach(var frameObject in _store.List) {
                SafeCast.As<UIRenderable>(frameObject).DoUIEvent();
            }
        }

        public void EarlyUpdate() => _store.EarlyUpdate();

        public void Update() => _store.Update();

        public void LateUpdate() => _store.LateUpdate();

        public void Render(in LayerRenderInfo renderInfo)
        {
            var view = new Matrix4(1, 0, 0, 0,
                                   0, -1, 0, UIRoot.Height,
                                   0, 0, 1, 0,
                                   0, 0, 0, 1);
            var identity = Matrix4.Identity;
            var timingPoints = _timingPoints;
            GL.Disable(EnableCap.DepthTest);
            timingPoints.BeforeRendering.DoQueuedEvents();
            foreach(var renderable in _store.Renderables) {
                if(renderable.IsRoot == false) { continue; }
                renderable.Render(identity, view, renderInfo.UIProjection);
            }
            GL.Enable(EnableCap.DepthTest);
            timingPoints.AfterRendering.DoQueuedEvents();
        }

        /// <summary>Do hit test</summary>
        /// <param name="mouse">mouse object</param>
        public void HitTest(Mouse mouse)
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

        public void Initialize()
        {
            UIRoot.Initialize();
        }
    }
}
