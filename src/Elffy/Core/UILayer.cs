#nullable enable
using Elffy.InputSystem;
using Elffy.UI;

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
        public void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        /// <inheritdoc/>
        public void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        /// <inheritdoc/>
        public void ClearFrameObject() => _store.ClearFrameObject();

        public void ApplyRemove() => _store.ApplyRemove();

        public void ApplyAdd() => _store.ApplyAdd();

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
            foreach(var renderable in _store.Renderables) {
                if(renderable.IsRoot == false) { continue; }
                renderable.Render(projection, view, Matrix4.Identity);
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
                RecursiveNotifyHitTestResult(uiRoot, mouse, hitControl);        // TODO: ヒット時イベント中に control を remove されるとまずい (Spanで回してるので)
            }
            else {
                RecursiveNotifyHitTestFalse(uiRoot, mouse);         // TODO: ヒット時イベント中に control を remove されるとまずい (Spanで回してるので)
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

            static void RecursiveNotifyHitTestResult(Control control, Mouse mouse, Control? hitControl)
            {
                control.NotifyHitTestResult(ReferenceEquals(control, hitControl), mouse);
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveNotifyHitTestResult(child, mouse, hitControl);
                }
            }

            static void RecursiveNotifyHitTestFalse(Control control, Mouse mouse)
            {
                control.NotifyHitTestResult(false, mouse);
                foreach(var child in control.ChildrenCore.AsSpan()) {
                    RecursiveNotifyHitTestFalse(child, mouse);
                }
            }
        }

        public void Initialize()
        {
            UIRoot.Initialize();
        }
    }
}
