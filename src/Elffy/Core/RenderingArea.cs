#nullable enable
using Elffy.InputSystem;
using Elffy.Threading;
using OpenToolkit.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Drawing;

namespace Elffy.Core
{
    /// <summary>OpenGL が描画を行う領域を扱うクラスです</summary>
    internal class RenderingArea : IDisposable
    {
        const float UI_FAR = 1.01f;
        const float UI_NEAR = -0.01f;

        private readonly PostProcessor _postProcessor = new PostProcessor();
        /// <summary>UI の投影行列</summary>
        private Matrix4 _uiProjection;

        internal IHostScreen OwnerScreen { get; }

        /// <summary>レイヤーのリスト</summary>
        internal LayerCollection Layers { get; }
        internal Camera Camera { get; } = new Camera();
        internal Mouse Mouse { get; } = new Mouse();

        internal bool IsEnabledPostProcess { get; set; }

        internal int Width
        {
            get => _width;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _width = value;
                OnSizeChanged(0, 0, _width, _height);

                void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range.");
            }
        }
        private int _width;

        internal int Height
        {
            get => _height;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _height = value;
                OnSizeChanged(0, 0, _width, _height);

                void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range.");
            }
        }
        private int _height;

        internal Size Size
        {
            get => new Size(_width, _height);
            set
            {
                if(value.Width < 0) { ThrowWidthOutOfRange(); }
                if(value.Height < 0) { ThrowHeightOutOfRange(); }

                _width = value.Width;
                _height = value.Height;
                OnSizeChanged(0, 0, _width, _height);

                void ThrowWidthOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value.Width), value.Width, $"width is out of range.");
                void ThrowHeightOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value.Height), value.Height, $"height is out of range.");
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

            // αブレンディング設定
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 裏面削除 反時計回りが表でカリング
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);

            //_isEnabledPostProcess = true;
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

            // Early update
            systemLayer.EarlyUpdate();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.EarlyUpdate();
            }
            uiLayer.EarlyUpdate();

            // Update
            systemLayer.Update();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.Update();
            }
            uiLayer.Update();

            // Late update
            systemLayer.LateUpdate();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.LateUpdate();
            }
            uiLayer.LateUpdate();

            Dispatcher.DoInvokedAction();

            // Render
            var isEnabledPostProcess = IsEnabledPostProcess;
            if(isEnabledPostProcess) {
                _postProcessor.EnableOffScreenRendering();
            }
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.Render(Camera.Projection, Camera.View);
            }
            uiLayer.Render(_uiProjection);
            if(isEnabledPostProcess) {
                _postProcessor.DisableOffScreenRendering();
                _postProcessor.Render();
            }

            // このフレームで削除されたオブジェクトの削除を適用
            systemLayer.ApplyRemove();
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.ApplyRemove();
            }
            uiLayer.ApplyRemove();
        }

        public void Dispose()
        {
            _postProcessor.Dispose();
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

            if(IsEnabledPostProcess) {
                _postProcessor.CreateNewBuffer(width, height);
            }
            Debug.WriteLine($"Size changed ({width}, {height})");
        }
    }
}
