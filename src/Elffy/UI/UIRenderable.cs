#nullable enable
using System;
using System.Diagnostics;
using Elffy.Core;
using Elffy.Exceptions;
using System.ComponentModel;
using UnmanageUtility;

namespace Elffy.UI
{
    /// <summary><see cref="UI.Control"/> を描画するためのオブジェクト。対象の <see cref="UI.Control"/> のインスタンスと一対一の関係を持つ</summary>
    internal sealed class UIRenderable : Renderable //, IUIRenderable
    {
        /// <summary>このインスタンスの描画対象である論理 UI コントロール</summary>
        public Control Control { get; private set; }

        /// <summary><see cref="UI.Control"/> の描画オブジェクトを作成します。</summary>
        /// <param name="control">描画対象の論理 UI コントロール</param>
        public UIRenderable(Control control)
        {
            ArgumentChecker.ThrowIfNullArg(control, nameof(control));
            IsFrozen = true;
            Control = control;
        }

        //void IUIRenderable.Render() => Render(,);
        //bool IUIRenderable.IsVisible => IsVisible;
        //void IUIRenderable.Activate() => Activate(Control.Root!.UILayer);
        //void IUIRenderable.Destroy() => Terminate();

        protected override void OnAlive()
        {
            base.OnAlive();
            // Layer is always UILayer
            var yAxisDir = ((UILayer)Layer!).YAxisDirection;
            Span<Vertex> vertices = stackalloc Vertex[4];
            Span<int> indices = stackalloc int[6];
            SetPolygon(Control.Width, Control.Height, Control.OffsetX, Control.OffsetY, yAxisDir, vertices, indices);
            LoadGraphicBuffer(vertices, indices);
        }

        #region SetPolygon
        /// <summary>頂点配列とインデックス配列をセットします</summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="offsetX">X方向のオフセット</param>
        /// <param name="offsetY">Y方向のオフセット</param>
        /// <param name="yAxisDirection">Y軸方向</param>
        /// <param name="vertices">頂点</param>
        /// <param name="indices">頂点インデックス</param>
        private void SetPolygon(int width, int height, int offsetX, int offsetY, YAxisDirection yAxisDirection, Span<Vertex> vertices, Span<int> indices)
        {
            Debug.Assert(vertices.Length == 4);
            Debug.Assert(indices.Length == 6);

            var p0 = new Vector3(offsetX, offsetY, 0);
            var p1 = p0 + new Vector3(width, 0, 0);
            var p2 = p0 + new Vector3(width, height, 0);
            var p3 = p0 + new Vector3(0, height, 0);

            Vector2 t0, t1, t2, t3;
            int i0, i1, i2, i3, i4, i5;
            switch(yAxisDirection) {
                case YAxisDirection.TopToBottom:
                    t3 = new Vector2(0, 0); t0 = new Vector2(0, 1);
                    t2 = new Vector2(1, 0); t1 = new Vector2(1, 1);

                    i0 = 0; i1 = 2; i2 = 1;
                    i3 = 2; i4 = 0; i5 = 3;
                    break;
                case YAxisDirection.BottomToTop:
                    t3 = new Vector2(0, 1); t2 = new Vector2(1, 1);
                    t0 = new Vector2(0, 0); t1 = new Vector2(1, 0);

                    i0 = 0; i1 = 1; i2 = 2;
                    i3 = 2; i4 = 3; i5 = 0;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(yAxisDirection), (int)yAxisDirection, typeof(YAxisDirection));
            }
            var normal = Vector3.UnitZ;

            vertices[0] = new Vertex(p0, normal, t0);
            vertices[1] = new Vertex(p1, normal, t1);
            vertices[2] = new Vertex(p2, normal, t2);
            vertices[3] = new Vertex(p3, normal, t3);
            indices[0] = i0;
            indices[1] = i1;
            indices[2] = i2;
            indices[3] = i3;
            indices[4] = i4;
            indices[5] = i5;
        }
        #endregion
    }
}
