#nullable enable
using Elffy.Exceptions;
using Elffy.Threading;
using Elffy.UI;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace Elffy.Core
{
    /// <summary>OpenGL が描画を行う領域を扱うクラスです</summary>
    internal class RenderingArea
    {
        const float UI_FAR = 1.01f;
        const float UI_NEAR = -0.01f;
        /// <summary>UI の投影行列</summary>
        private Matrix4 _uiProjection;

        /// <summary>レイヤーのリスト</summary>
        public LayerCollection Layers { get; } = new LayerCollection();

        #region Width
        public int Width
        {
            get => _width;
            set
            {
                ArgumentChecker.ThrowOutOfRangeIf(value < 0, nameof(value), value, $"{nameof(value)} is out of range.");
                _width = value;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        private int _width;
        #endregion

        #region Height
        public int Height
        {
            get => _height;
            set
            {
                ArgumentChecker.ThrowOutOfRangeIf(value < 0, nameof(value), value, $"{nameof(value)} is out of range.");
                _height = value;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        private int _height;
        #endregion

        #region Size
        public Size Size
        {
            get => new Size(_width, _height);
            set
            {
                ArgumentChecker.ThrowOutOfRangeIf(value.Width < 0, nameof(value.Width), value.Width, "value is out of range.");
                ArgumentChecker.ThrowOutOfRangeIf(value.Height < 0, nameof(value.Height), value.Height, "value is out of range.");
                _width = value.Width;
                _height = value.Height;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        #endregion

        /// <summary>get or set color of clearing rendering area on beginning of each frames.</summary>
        public Color4 ClearColor
        {
            get => _clearColor;
            set
            {
                _clearColor = value;
                GL.ClearColor(_clearColor);
            }
        }
        private Color4 _clearColor;

        public RenderingArea()
        {
        }

        #region InitializeGL
        /// <summary>OpenTL の描画に関する初期設定を行います</summary>
        public void InitializeGL()
        {
            ClearColor = Color4.Gray;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Normalize);

            // αブレンディング設定
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 裏面削除 反時計回りが表でカリング
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);
        }
        #endregion

        #region RenderFrame
        /// <summary>フレームを更新して描画します</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">カメラ行列</param>
        public void RenderFrame(Matrix4 projection, Matrix4 view)
        {
            // レイヤー更新
            Layers.SystemLayer.Update();
            foreach(var layer in Layers) {
                layer.Update();
            }
            Dispatcher.DoInvokedAction();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // レイヤー描画処理
            foreach(var layer in Layers) {
                if(layer.IsLightingEnabled) {
                    Light.IsEnabled = true;
                }
                else {
                    Light.IsEnabled = false;
                }
                if(layer is UILayer uILayer) {
                    uILayer.Render(_uiProjection);
                }
                else {
                    layer.Render(projection, view);
                }
            }

            // レイヤー変更適用
            Layers.SystemLayer.ApplyChanging();
            foreach(var layer in Layers) {
                layer.ApplyChanging();
            }
        }
        #endregion

        private void OnSizeChanged(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
            _uiProjection = Matrix4.CreateOrthographicOffCenter(x, x + width, y, y + height, UI_NEAR, UI_FAR);
        }
    }
}
