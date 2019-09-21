using Elffy.Core;
using OpenTK;
using System;
using System.Drawing;
using Elffy.Effective;

namespace Elffy.UI
{
    #region class UIBase
    /// <summary>
    /// UI の要素の基底クラス。UI への配置, フォーカス処理, ヒットテストの処理を提供します<para/>
    /// </summary>
    public abstract class UIBase : Renderable, IDisposable
    {
        private bool _disposed;
        private readonly UnmanagedArray<Vertex> _vertexArray = new UnmanagedArray<Vertex>(4);
        private readonly UnmanagedArray<int> _indexArray = new UnmanagedArray<int>(6);

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

        ~UIBase() => Dispose(false);

        protected override void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                    _vertexArray.Free();
                    _indexArray.Free();
                    base.Dispose(true);
                }
                // Release unmanaged resource
                _disposed = true;
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal bool HitTest()
        {
            throw new NotImplementedException();        // TODO: HitTest
        }

        protected override void OnActivated()
        {
            SetPolygon(Width, Height, OffsetX, OffsetY);
            InitGraphicBuffer(_vertexArray.Ptr, _vertexArray.Length, _indexArray.Ptr, _indexArray.Length);
            //if(Width > 0 && Height > 0) {

            //}
        }

        #region SetPolygon
        /// <summary>頂点配列とインデックス配列をセットします</summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="offsetX">X方向のオフセット</param>
        /// <param name="offsetY">Y方向のオフセット</param>
        private void SetPolygon(int width, int height, int offsetX, int offsetY)
        {
            var offset = new Vector3(offsetX, offsetY, 0);
            var p0 = Position + offset;
            var p1 = p0 + new Vector3(width, 0, 0);
            var p2 = p0 + new Vector3(width, height, 0);
            var p3 = p0 + new Vector3(0, height, 0);

            var t0 = new Vector2(0, 0);
            var t1 = new Vector2(1, 0);
            var t2 = new Vector2(1, 1);
            var t3 = new Vector2(0, 1);

            var normal = Vector3.UnitZ;

            _vertexArray[0] = new Vertex(p0, normal, t0);
            _vertexArray[1] = new Vertex(p1, normal, t1);
            _vertexArray[2] = new Vertex(p2, normal, t2);
            _vertexArray[3] = new Vertex(p3, normal, t3);
            _indexArray[0] = 0;
            _indexArray[1] = 1;
            _indexArray[2] = 2;
            _indexArray[3] = 2;
            _indexArray[4] = 3;
            _indexArray[5] = 0;
        }
        #endregion
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
