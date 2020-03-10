#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using TKMatrix4 = OpenTK.Matrix4;

namespace Elffy.Core
{
    internal sealed class UILayer : ILayer
    {
        private readonly FrameObjectStore _store = new FrameObjectStore();

        /// <summary>現在フォーカスがある <see cref="Control"/></summary>
        private Control? _focusedControl;

        /// <summary>UI tree の Root</summary>
        internal Page UIRoot { get; }

        internal YAxisDirection YAxisDirection { get; }

        internal UILayer(YAxisDirection yAxisDirection)
        {
            UIRoot = new Page(this);
            YAxisDirection = yAxisDirection;
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

        /// <summary>オブジェクトの追加と削除の変更を適用します</summary>
        internal void ApplyChanging() => _store.ApplyChanging();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        /// <summary>フレームの更新を行います</summary>
        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        public void ClearFrameObject() => _store.ClearFrameObject();

        /// <summary>画面への投影行列を指定して、描画を実行します</summary>
        /// <param name="projection"></param>
        internal void Render(TKMatrix4 projection)
        {
            var view = YAxisDirection switch
            {
                YAxisDirection.TopToBottom => new TKMatrix4(1, 0,   0, 0,
                                                          0, -1,  0, 0,
                                                          0, 0,   1, 0,
                                                          0, UIRoot.Height, 0, 1),
                YAxisDirection.BottomToTop => TKMatrix4.Identity,
                _ => throw new NotSupportedException(),
            };
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            foreach(var renderable in _store.Renderables.Where(x => x.IsRoot && x.IsVisible)) {
                GL.LoadMatrix(ref view);
                renderable.Render();
            }
        }

        /// <summary><see cref="UILayer"/> 内の <see cref="Control"/> に対してマウスヒットテストを行います</summary>
        /// <param name="mouse">マウスオブジェクト</param>
        public void HitTest(Mouse mouse)
        {
            if(IsHitTestEnabled == false) { return; }
            var uiRoot = UIRoot;
            var pos = mouse.Position;
            if(YAxisDirection == YAxisDirection.BottomToTop) {
                pos.Y = UIRoot.Height - pos.Y;
            }
            if(mouse.OnScreen) {
                // Hit control is the last control where mouse over test is true
                var hitControl = default(Control);
                foreach(var control in uiRoot.Children) {
                    if(control.MouseOverTest(pos)) {
                        hitControl = control;
                    }
                }
                foreach(var control in uiRoot.Children) {
                    control.NotifyHitTestResult(control == hitControl, pos);
                }
            }
            else {
                foreach(var control in uiRoot.Children) {
                    control.NotifyHitTestResult(false, pos);
                }
            }
        }
    }
}
