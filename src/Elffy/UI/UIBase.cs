using Elffy.Core;
using System;
using System.Drawing;

namespace Elffy.UI
{
    #region class UIBase
    /// <summary>
    /// UI の要素の基底クラス。UI への配置, フォーカス処理, ヒットテストの処理を提供します<para/>
    /// </summary>
    public abstract class UIBase : Renderable
    {
        /// <summary>get or set Width of <see cref="UIBase"/></summary>
        public int Width { get; set; }
        /// <summary>get or set Height of <see cref="UIBase"/></summary>
        public int Height { get; set; }
        /// <summary>get or set offset position X of layout</summary>
        public int OffsetX { get; set; }
        /// <summary>get or set offset position Y of layout</summary>
        public int OffsetY { get; set; }
        /// <summary>get or set horizontal alignment of layout</summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }
        /// <summary>get or set vertical alignment of layout</summary>
        public VerticalAlignment VerticalAlignment { get; set; }
        /// <summary>get or set whether this <see cref="UIBase"/> can be focused</summary>
        public bool IsFocusable { get; set; }
        /// <summary>get whether this <see cref="UIBase"/> is focused</summary>
        public bool IsFocused { get; private set; }
        /// <summary>get or set whether this <see cref="UIBase"/> is enable in HitTest</summary>
        public bool IsHitTestVisible { get; set; }
        /// <summary>get whether the mouse is over this <see cref="UIBase"/></summary>
        public bool IsMouseOver { get; private set; }
        /// <summary>Focus enter event</summary>
        public event EventHandler FocusEnter;
        /// <summary>Focus lost event</summary>
        public event EventHandler FocusLost;
        /// <summary>Mouse enter event</summary>
        public event EventHandler MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event EventHandler MouseLeave;

        /// <summary>constructor of <see cref="UIBase"/></summary>
        public UIBase()
        {
            Layer = ObjectLayer.UI;
        }

        internal bool HitTest()
        {
            throw new NotImplementedException();        // TODO: HitTest
        }
    }
    #endregion

    #region enum HorizontalAlignment
    /// <summary>Layout horizontal alignment</summary>
    public enum HorizontalAlignment
    {
        /// <summary>left alignment</summary>
        Left,
        /// <summary>center alignment</summary>
        Center,
        /// <summary>right alignment</summary>
        Right,
    }
    #endregion

    #region enum VerticalAlignment
    /// <summary>Layout vertical alignment</summary>
    public enum VerticalAlignment
    {
        /// <summary>top alignment</summary>
        Top,
        /// <summary>center alignment</summary>
        Center,
        /// <summary>bottom alignment</summary>
        Bottom,
    }
    #endregion

    #region class MouseEventArgs
    /// <summary>Mouse event argument class</summary>
    public class MouseEventArgs : EventArgs
    {
        /// <summary>mouse position</summary>
        public Point MousePosition { get; }

        /// <summary>constructor</summary>
        /// <param name="mousePosition">mouse position</param>
        public MouseEventArgs(Point mousePosition)
        {
            MousePosition = mousePosition;
        }
    }
    #endregion
}
