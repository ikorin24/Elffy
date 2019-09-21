using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    /// <summary><see cref="UIBase"/> を描画するためのオブジェクト。対象の <see cref="UIBase"/> のインスタンスと一対一の関係を持つ</summary>
    internal sealed class UIPlain : Renderable, IDisposable, IUIRenderer
    {
        private bool _disposed;
        private readonly UnmanagedArray<Vertex> _vertexArray = new UnmanagedArray<Vertex>(4);
        private readonly UnmanagedArray<int> _indexArray = new UnmanagedArray<int>(6);

        public UIBase Control { get; private set; }

        public UIPlain(UIBase control)
        {
            RenderingLayer = RenderingLayer.UI;
            IsFrozen = true;
            Control = control ?? throw new ArgumentNullException(nameof(control));
        }

        ~UIPlain() => Dispose(false);

        void IUIRenderer.Render() => Render();
        bool IUIRenderer.IsVisible => IsVisible;
        void IUIRenderer.Activate() => Activate();
        void IUIRenderer.Destroy() => Destroy();

        #region Dispose pattern
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
        #endregion

        protected override void OnActivated()
        {
            base.OnActivated();
            SetPolygon(Control.Width, Control.Height, Control.OffsetX, Control.OffsetY);
            InitGraphicBuffer(_vertexArray.Ptr, _vertexArray.Length, _indexArray.Ptr, _indexArray.Length);
        }

        #region SetPolygon
        /// <summary>頂点配列とインデックス配列をセットします</summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="offsetX">X方向のオフセット</param>
        /// <param name="offsetY">Y方向のオフセット</param>
        private void SetPolygon(int width, int height, int offsetX, int offsetY)
        {
            var p0 = new Vector3(offsetX, offsetY, 0);
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

    /// <summary>UI 要素を描画するためのインターフェース</summary>
    internal interface IUIRenderer
    {
        /// <summary>このオブジェクトが描画されるかどうかを取得します</summary>
        bool IsVisible { get; }
        /// <summary>このオブジェクトを描画します</summary>
        void Render();

        void Activate();

        void Destroy();
    }
}
