#nullable enable
using Elffy.InputSystem;
using Elffy.UI;

namespace Elffy.Core
{
    internal sealed class UILayer : Layer
    {
        /// <summary>現在フォーカスがある <see cref="Control"/></summary>
        private Control? _focusedControl;

        /// <summary>UI tree の Root</summary>
        public Page UIRoot { get; }

        public YAxisDirection YAxisDirection { get; set; } = YAxisDirection.DownToTop;

        public UILayer(string name) : base(name)
        {
            UIRoot = new Page(this);
        }

        public bool IsHitTestEnabled { get; set; } = true;

        /// <summary><see cref="UILayer"/> 内の <see cref="Control"/> に対してマウスヒットテストを行います</summary>
        /// <param name="mouse">マウスオブジェクト</param>
        public void HitTest(Mouse mouse)
        {
            var uiRoot = UIRoot;
            if(IsHitTestEnabled == false) { return; }
            if(mouse.OnScreen == false) { return; }

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
