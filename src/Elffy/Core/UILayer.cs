#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using System.Diagnostics;

namespace Elffy.Core
{
    internal sealed class UILayer : ILayer
    {
        private readonly FrameObjectStore _store;

        /// <inheritdoc/>
        public int ObjectCount => _store.ObjectCount;

        /// <inheritdoc/>
        public bool IsVisible { get; set; } = true;

        /// <inheritdoc/>
        public LayerCollection OwnerCollection { get; }

        /// <summary>Get root panel of UI</summary>
        public RootPanel UIRoot { get; }

        /// <summary>Get or set whether hit test is enabled.</summary>
        public bool IsHitTestEnabled { get; set; } = true;

        public UILayer(LayerCollection owner)
        {
            _store = FrameObjectStore.New(64);
            OwnerCollection = owner;
            UIRoot = new RootPanel(this);
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

        /// <summary>Render objects with specified projection matrix</summary>
        /// <param name="projection">projection matrix</param>
        public unsafe void Render(in Matrix4 projection)
        {
            var view = new Matrix4(1, 0, 0, 0,
                                   0, -1, 0, UIRoot.Height,
                                   0, 0, 1, 0,
                                   0, 0, 0, 1);
            var identity = Matrix4.Identity;
            foreach(var renderable in _store.Renderables) {
                if(renderable.IsRoot == false) { continue; }
                renderable.Render(identity, view, projection);
            }
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
                // Span で回しているので途中でコントロールを add/remove してはいけない。
                // そのため、途中でイベントの実行等のユーザーコードを差し込める実装にしてはいけない。
                control.NotifyHitTestResult(ReferenceEquals(control, hitControl));
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveNotifyHitTestResult(child, hitControl);
                }
            }

            static void RecursiveNotifyHitTestFalse(Control control)
            {
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
