#nullable enable
using Elffy.InputSystem;
using Elffy.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;

namespace Elffy.Core
{
    /// <summary>OpenGL が描画を行う領域を扱うクラスです</summary>
    internal sealed class RenderingArea : IDisposable
    {
        const float UI_FAR = 1.01f;
        const float UI_NEAR = -0.01f;

        private bool _disposed;
        /// <summary>UI の投影行列</summary>
        private Matrix4 _uiProjection;
        private bool _postProcessChanged;
        private PostProcessCompiled? _ppCompiled;
        private PostProcess? _postProcess;

        public IHostScreen OwnerScreen { get; }

        /// <summary>レイヤーのリスト</summary>
        public LayerCollection Layers { get; }
        public Camera Camera { get; } = new Camera();
        public Mouse Mouse { get; } = new Mouse();

        public AsyncBackEndPoint AsyncBack { get; }

        public PostProcess? PostProcess
        {
            get => _postProcess;
            set
            {
                _postProcess = value;
                _postProcessChanged = true;
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _width = value;
                OnSizeChanged(_width, _height);

                void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range.");
            }
        }
        private int _width;

        public int Height
        {
            get => _height;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _height = value;
                OnSizeChanged(_width, _height);

                void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range.");
            }
        }
        private int _height;

        public Vector2i Size
        {
            get => new Vector2i(_width, _height);
            set
            {
                if(value.X < 0) { ThrowWidthOutOfRange(); }
                if(value.Y < 0) { ThrowHeightOutOfRange(); }

                _width = value.X;
                _height = value.Y;
                OnSizeChanged(_width, _height);

                void ThrowWidthOutOfRange() => throw new ArgumentOutOfRangeException("Width", value.X, $"width is out of range.");
                void ThrowHeightOutOfRange() => throw new ArgumentOutOfRangeException("Height", value.Y, $"height is out of range.");
            }
        }

        internal RenderingArea(IHostScreen screen)
        {
            OwnerScreen = screen;
            Layers = new LayerCollection(this);
            AsyncBack = new AsyncBackEndPoint();
        }

        /// <summary>OpenTL の描画に関する初期設定を行います</summary>
        public void InitializeGL()
        {
            GL.ClearColor(Color4.Gray);
            GL.Enable(EnableCap.DepthTest);

            // Enable alpha blending.
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Enable back face culling. front face is counter clockwise
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);

            // Enable Multi Sampling Anti-alias (MSAA)
            GL.Enable(EnableCap.Multisample);
        }

        /// <summary>フレームを更新して描画します</summary>
        public void RenderFrame()
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
            AsyncBack.DoQueuedEvents(FrameLoopTiming.EarlyUpdate);
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.EarlyUpdate();
            }
            uiLayer.EarlyUpdate();

            // Update
            systemLayer.Update();
            AsyncBack.DoQueuedEvents(FrameLoopTiming.Update);
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.Update();
            }
            uiLayer.Update();

            // Late update
            systemLayer.LateUpdate();
            AsyncBack.DoQueuedEvents(FrameLoopTiming.LateUpdate);
            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.LateUpdate();
            }
            uiLayer.LateUpdate();

            // Comile postProcess if needed
            if(_postProcessChanged) {
                _ppCompiled?.Dispose();
                _ppCompiled = _postProcess?.Compile();
                _postProcessChanged = false;
            }

            // Render
            using(var scope = PostProcessCompiled.Scope.RootScope(_ppCompiled, new Vector2i(_width, _height))) {
                foreach(var layer in Layers.AsReadOnlySpan()) {
                    layer.Render(Camera.Projection, Camera.View, scope);
                }
                uiLayer.Render(_uiProjection);
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
            if(_disposed) { return; }
            _disposed = true;

            // Clear objects in all layers
            var layers = Layers;
            layers.SystemLayer.ClearFrameObject();
            foreach(var layer in layers.AsReadOnlySpan()) {
                layer.ClearFrameObject();
            }
            layers.Clear();

            // Dispose resources of post process
            _ppCompiled?.Dispose();
            _ppCompiled = null;
        }

        private void OnSizeChanged(int width, int height)
        {
            // Change view and projection matrix (World).
            Camera.ChangeScreenSize(width, height);

            // Change projection matrix (UI)
            GL.Viewport(0, 0, width, height);
            Matrix4.OrthographicProjection(0, width, 0, height, UI_NEAR, UI_FAR, out _uiProjection);
            var uiRoot = Layers.UILayer.UIRoot;
            uiRoot.Width = width;
            uiRoot.Height = height;

            Debug.WriteLine($"Size changed ({width}, {height})");
        }
    }
}
