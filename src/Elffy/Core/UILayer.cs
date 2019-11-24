#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;

namespace Elffy.Core
{
    internal sealed class UILayer : Layer
    {
        /// <summary>現在フォーカスがある <see cref="Control"/></summary>
        private Control? _focusedControl;

        /// <summary>UI tree の Root</summary>
        public Page UIRoot { get; }

        public YAxisDirection YAxisDirection { get; }

        public UILayer(string name, YAxisDirection yAxisDirection) : base(name)
        {
            UIRoot = new Page(this);
            YAxisDirection = yAxisDirection;
        }

        public bool IsHitTestEnabled { get; set; } = true;

        /// <summary>画面への投影行列を指定して、描画を実行します</summary>
        /// <param name="projection"></param>
        internal void Render(Matrix4 projection)
        {
            var view = YAxisDirection switch
            {
                YAxisDirection.TopToBottom => new Matrix4(1, 0,   0, 0,
                                                          0, -1,  0, 0,
                                                          0, 0,   1, 0,
                                                          0, 450, 0, 1),   // TODO: 高さを画面の高さにする
                YAxisDirection.BottomToTop => Matrix4.Identity,
                _ => throw new NotSupportedException(),
            };
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            foreach(var renderable in Renderables.Where(x => x.IsRoot && x.IsVisible)) {
                GL.LoadMatrix(ref view);
                renderable.Render();
            }
        }

        /// <summary><see cref="UILayer"/> 内の <see cref="Control"/> に対してマウスヒットテストを行います</summary>
        /// <param name="mouse">マウスオブジェクト</param>
        public void HitTest(Mouse mouse)
        {
            if(IsHitTestEnabled == false) { return; }
            if(mouse.OnScreen == false) { return; }
            var uiRoot = UIRoot;

            // Hit control is the last control where mouse over test is true
            var hitControl = default(Control);
            foreach(var control in uiRoot.Children) {
                if(control.MouseOverTest(mouse)) {
                    hitControl = control;
                }
            }
            foreach(var control in uiRoot.Children) {
                control.NotifyHitTestResult(control == hitControl, mouse);
            }
        }
    }
}
