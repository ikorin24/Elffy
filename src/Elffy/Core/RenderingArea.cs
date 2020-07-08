#nullable enable
using Elffy.Exceptions;
using Elffy.InputSystem;
using Elffy.Threading;
using Elffy.UI;
using OpenToolkit.Graphics.OpenGL;
using System;
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

        internal IHostScreen OwnerScreen { get; }

        /// <summary>レイヤーのリスト</summary>
        internal LayerCollection Layers { get; }
        internal Camera Camera { get; } = new Camera();
        internal Mouse Mouse { get; } = new Mouse();

        internal int Width
        {
            get => _width;
            set
            {
                if(value < 0) { throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range."); }
                _width = value;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        private int _width;

        internal int Height
        {
            get => _height;
            set
            {
                if(value < 0) { throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range."); }
                _height = value;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        private int _height;

        internal Size Size
        {
            get => new Size(_width, _height);
            set
            {
                if(value.Width < 0) { throw new ArgumentOutOfRangeException(nameof(value.Width), value.Width, $"value is out of range."); }
                if(value.Height < 0) { throw new ArgumentOutOfRangeException(nameof(value.Height), value.Height, $"value is out of range."); }

                _width = value.Width;
                _height = value.Height;
                OnSizeChanged(0, 0, _width, _height);
            }
        }

        /// <summary>get or set color of clearing rendering area on beginning of each frames.</summary>
        internal Color4 ClearColor
        {
            get => _clearColor;
            set
            {
                _clearColor = value;
                GL.ClearColor(_clearColor);
            }
        }
        private Color4 _clearColor;

        internal RenderingArea(IHostScreen screen)
        {
            OwnerScreen = screen;
            Layers = new LayerCollection(this);
        }

        /// <summary>OpenTL の描画に関する初期設定を行います</summary>
        internal void InitializeGL()
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

        /// <summary>フレームを更新して描画します</summary>
        internal void RenderFrame()
        {
            var systemLayer = Layers.SystemLayer;
            var uiLayer = Layers.UILayer;

            // 前フレームで追加されたオブジェクトの追加を適用
            systemLayer.ApplyAdd();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.ApplyAdd();
            }
            uiLayer.ApplyAdd();

            // 事前レイヤー更新
            systemLayer.EarlyUpdate();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.EarlyUpdate();
            }
            uiLayer.EarlyUpdate();

            // レイヤー更新
            systemLayer.Update();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.Update();
            }
            uiLayer.Update();

            // 事後レイヤー更新
            systemLayer.LateUpdate();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.LateUpdate();
            }
            uiLayer.LateUpdate();

            Dispatcher.DoInvokedAction();

            // レイヤー描画処理
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.Render(Camera.Projection, Camera.View);
            }
            uiLayer.Render(_uiProjection);

            // このフレームで削除されたオブジェクトの削除を適用
            systemLayer.ApplyRemove();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.ApplyRemove();
            }
            uiLayer.ApplyRemove();
        }

        private void OnSizeChanged(int x, int y, int width, int height)
        {
            // Change view and projection matrix (World).
            Camera.ChangeScreenSize(width, height);

            // Change projection matrix (UI)
            GL.Viewport(x, y, width, height);
            Matrix4.OrthographicProjection(x, x + width, y, y + height, UI_NEAR, UI_FAR, out _uiProjection);
            var uiRoot = Layers.UILayer.UIRoot;
            uiRoot.Width = width;
            uiRoot.Height = height;
        }
    }
}
