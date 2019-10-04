using Elffy.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    internal sealed class UILayer : Layer
    {
        /// <summary>現在フォーカスがある <see cref="UIBase"/></summary>
        private UIBase _focusedControl;

        public UILayer(string name) : base(name)
        {
        }

        #region HitTest
        /// <summary>このレイヤーに含まれる <see cref="IUIRenderable"/> にヒットテストを実行します</summary>
        public void HitTest()
        {
            var hit = Renderables.OfType<IUIRenderable>()
                                 .Select(x => x.Control)
                                 .Where(control => control.IsHitTestVisible)
                                 .LastOrDefault(control => control.HitTest());
            if(_focusedControl != null) {
                _focusedControl.IsFocused = false;
            }
            if(hit != null) {
                hit.IsFocused = true;
            }
            _focusedControl = hit;
        }
        #endregion
    }
}
