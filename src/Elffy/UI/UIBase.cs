using Elffy.Core;
using OpenTK;
using System;
using System.Drawing;
using Elffy.Effective;
using System.Collections.Generic;

namespace Elffy.UI
{
    #region class UIBase
    /// <summary>
    /// UI の要素の基底クラス。UI への配置, フォーカス処理, ヒットテストの処理を提供します<para/>
    /// </summary>
    /// 
    /// <remarks>
    /// <see cref="UIBase"/> はUIを構成する論理的なコントロールとしての機能のみを提供します。
    /// 画面への描画に関する処理は、このクラスと対になる <see cref="Core.Renderable"/> 継承クラス <see cref="UIPlain"/> のインスタンスに任されます。
    /// <see cref="UIPlain"/> による描画は <see cref="UIBase"/> によって完全に隠蔽され、外部からは描画を気にすることなく論理的 UI 構造を扱えます。
    /// 
    /// <see cref="UIBase"/> の木構造は、描画を担当する <see cref="Core.Renderable"/> オブジェクトの木構造とは独立しています。
    /// <see cref="UIBase"/> が親子関係の木構造を形成している場合でも、その描画オブジェクトは常に木構造を作りません。
    /// </remarks>
    public abstract class UIBase
    {
        #region Property
        /// <summary>この <see cref="UIBase"/> を描画するオブジェクト</summary>
        internal IUIRenderable Renderable => _renderable;
        private readonly UIPlain _renderable;

        /// <summary>この <see cref="UIBase"/> のツリー構造の子要素を取得します</summary>
        public UIBaseCollection Children { get; }

        #region Parent
        /// <summary>この <see cref="UIBase"/> のツリー構造の親を取得します</summary>
        public UIBase Parent
        {
            get => _parent;
            internal set
            {
                if(_parent == null) {
                    _parent = value;
                }
                else if(_parent != null && value == null) {
                    _parent = value;
                }
                else { throw new InvalidOperationException($"The instance is already a child of another object. Can not has multi parents."); }
            }
        }
        private UIBase _parent;
        #endregion

        #region Position
        public Vector2 Position
        {
            get => _renderable.Position.Xy;
            set
            {
                var vec = value - _renderable.Position.Xy;
                _renderable.Position += new Vector3(vec);
                _absolutePosition += vec;
                foreach(var child in GetOffspring()) {
                    child._absolutePosition += vec;
                }
            }
        }
        #endregion

        #region PositionX
        public float PositionX
        {
            get => _renderable.PositionX;
            set
            {
                var diff = value - _renderable.PositionX;
                _renderable.PositionX += diff;
                _absolutePosition.X += diff;
                foreach(var child in GetOffspring()) {
                    child._absolutePosition.X += diff;
                }
            }
        }
        #endregion

        #region PositionY
        /// <summary>オブジェクトのY座標</summary>
        public float PositionY
        {
            get => _renderable.PositionY;
            set
            {
                var diff = value - _renderable.PositionY;
                _renderable.PositionY += diff;
                _absolutePosition.Y += diff;
                foreach(var child in GetOffspring()) {
                    child._absolutePosition.Y += diff;
                }
            }
        }
        #endregion

        #region AbsolutePosition
        public Vector2 AbsolutePosition
        {
            get => _absolutePosition;
        }
        private Vector2 _absolutePosition;
        #endregion

        #region Width
        /// <summary>get or set Width of <see cref="UIBase"/></summary>
        public int Width
        {
            get => _width;
            set
            {
                if(value < 0) { throw new ArgumentOutOfRangeException(nameof(value), value, "value is invalid."); }
                _width = value;
            }
        }
        private int _width;
        #endregion

        #region Height
        /// <summary>get or set Height of <see cref="UIBase"/></summary>
        public int Height
        {
            get => _height;
            set
            {
                if(value < 0) { throw new ArgumentOutOfRangeException(nameof(value), value, "value is invalid."); }
                _height = value;
            }
        }
        private int _height;
        #endregion

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
        #endregion Property

        /// <summary>Focus enter event</summary>
        public event EventHandler FocusEnter;
        /// <summary>Focus lost event</summary>
        public event EventHandler FocusLost;
        /// <summary>Mouse enter event</summary>
        public event EventHandler MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event EventHandler MouseLeave;

        #region constructor
        /// <summary>constructor of <see cref="UIBase"/></summary>
        public UIBase()
        {
            Children = new UIBaseCollection(this);
            _renderable = new UIPlain(this);
        }
        #endregion

        #region GetOffspring
        /// <summary>このオブジェクトの <see cref="Children"/> 以下に存在する全ての子孫を取得します。列挙順は深さ優先探索 (DFS; depth-first search) です。</summary>
        /// <returns>全ての子孫オブジェクト</returns>
        public IEnumerable<UIBase> GetOffspring()
        {
            foreach(var child in Children) {
                yield return child;
                foreach(var offspring in child.GetOffspring()) {
                    yield return offspring;
                }
            }
        }
        #endregion

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
