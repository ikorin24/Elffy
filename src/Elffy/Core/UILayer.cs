#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using OpenToolkit.Graphics.OpenGL4;
using System;
using System.Linq;

namespace Elffy.Core
{
    internal sealed class UILayer : ILayer
    {
        private readonly FrameObjectStore _store = new FrameObjectStore();

        /// <summary>現在フォーカスがある <see cref="Control"/></summary>
        private Control? _focusedControl;

        /// <summary>このレイヤーを持つ親</summary>
        internal LayerCollection Owner { get; }
        LayerCollection? ILayer.OwnerCollection => Owner;

        /// <summary>UI tree の Root</summary>
        internal Page UIRoot { get; }

        internal UILayer(LayerCollection owner)
        {
            Owner = owner;
            UIRoot = new Page(this);
        }

        internal bool IsHitTestEnabled { get; set; } = true;

        /// <summary>現在生きている全オブジェクトの数を取得します</summary>
        public int ObjectCount => _store.ObjectCount;

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        public void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        public void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        internal void ApplyRemove() => _store.ApplyRemove();

        internal void ApplyAdd() => _store.ApplyAdd();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        /// <summary>フレームの更新を行います</summary>
        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        public void ClearFrameObject() => _store.ClearFrameObject();

        /// <summary>画面への投影行列を指定して、描画を実行します</summary>
        /// <param name="projection"></param>
        internal unsafe void Render(in Matrix4 projection)
        {
            //var view = YAxisDirection switch
            //{
            //    YAxisDirection.TopToBottom => new Matrix4(1, 0,  0, 0,
            //                                              0, -1, 0, UIRoot.Height,
            //                                              0, 0,  1, 0,
            //                                              0, 0,  0, 1),
            //    YAxisDirection.BottomToTop => Matrix4.Identity,
            //    _ => throw new NotSupportedException(),
            //};
            var view = new Matrix4(1, 0, 0, 0,
                                   0, -1, 0, UIRoot.Height,
                                   0, 0, 1, 0,
                                   0, 0, 0, 1);
            foreach(var renderable in _store.Renderables) {
                if(!renderable.IsRoot || !renderable.IsVisible) { continue; }
                renderable.Render(projection, view, Matrix4.Identity);
            }
        }

        /// <summary><see cref="UILayer"/> 内の <see cref="Control"/> に対してマウスヒットテストを行います</summary>
        /// <param name="mouse">マウスオブジェクト</param>
        public void HitTest(Mouse mouse)
        {
            if(IsHitTestEnabled == false) { return; }
            var uiRoot = UIRoot;
            if(mouse.OnScreen) {
                // Hit control is the last control where mouse over test is true
                var hitControl = default(Control);
                foreach(var control in uiRoot.Children.AsReadOnlySpan()) {
                    if(control.MouseOverTest(mouse)) {
                        hitControl = control;
                    }
                }
                foreach(var control in uiRoot.Children.AsReadOnlySpan()) {
                    control.NotifyHitTestResult(control == hitControl, mouse);    // TODO: ヒット時イベント中に control を remove されるとまずい (Spanで回してるので)
                }
            }
            else {
                foreach(var control in uiRoot.Children.AsReadOnlySpan()) {
                    control.NotifyHitTestResult(false, mouse);                    // TODO: ヒット時イベント中に control を remove されるとまずい (Spanで回してるので)
                }
            }
        }
    }
}
