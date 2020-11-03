#nullable enable
using Elffy.InputSystem;
using Elffy.OpenGL;
using Elffy.Shading;
using Elffy.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;

namespace Elffy.Core
{
    /// <summary>OpenGL が描画を行う領域を扱うクラスです</summary>
    internal sealed class RenderingArea : IDisposable
    {
        const float UI_FAR = 1f;
        const float UI_NEAR = -1f;

        private bool _disposed;
        /// <summary>UI の投影行列</summary>
        private Matrix4 _uiProjection;
        private PostProcessImpl _postProcessImpl;   // mutable object, don't make it readonly.
        private Vector2i _size;

        public IHostScreen OwnerScreen { get; }

        /// <summary>レイヤーのリスト</summary>
        public LayerCollection Layers { get; }
        public Camera Camera { get; } = new Camera();
        public Mouse Mouse { get; } = new Mouse();
        public Keyboard Keyboard { get; } = new Keyboard();

        public AsyncBackEndPoint AsyncBack { get; }

        public PostProcess? PostProcess
        {
            get => _postProcessImpl.PostProcess;
            set => _postProcessImpl.PostProcess = value;
        }

        public int Width
        {
            get => _size.X;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _size.X = value;
                OnSizeChanged(_size);

                void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range.");
            }
        }

        public int Height
        {
            get => _size.Y;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _size.Y = value;
                OnSizeChanged(_size);

                void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is out of range.");
            }
        }

        public Vector2i Size
        {
            get => _size;
            set
            {
                if(value.X < 0) { ThrowWidthOutOfRange(); }
                if(value.Y < 0) { ThrowHeightOutOfRange(); }

                _size = value;
                OnSizeChanged(_size);

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

            GL.Disable(EnableCap.Multisample);  // I don't care about MSAA
        }

        /// <summary>フレームを更新して描画します</summary>
        public void RenderFrame()
        {
            var uiLayer = Layers.UILayer;
            var layers = Layers.AsReadOnlySpan();

            // Apply FrameObject added at previous frame.
            foreach(var layer in layers) {
                layer.ApplyAdd();
            }
            uiLayer.ApplyAdd();

            // Early update
            AsyncBack.DoQueuedEvents(FrameLoopTiming.EarlyUpdate);
            foreach(var layer in layers) {
                layer.EarlyUpdate();
            }
            uiLayer.EarlyUpdate();

            // Update
            AsyncBack.DoQueuedEvents(FrameLoopTiming.Update);
            foreach(var layer in layers) {
                layer.Update();
            }
            uiLayer.Update();

            // Late update
            AsyncBack.DoQueuedEvents(FrameLoopTiming.LateUpdate);
            foreach(var layer in layers) {
                layer.LateUpdate();
            }
            uiLayer.LateUpdate();

            // Render
            var ppCompiled = _postProcessImpl.GetCompiled();        // Get comiled postProcess
            var screenFbo = FBO.Empty;
            if(ppCompiled is null) {
                FBO.Bind(screenFbo, FBO.Target.FrameBuffer);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                foreach(var layer in layers) {
                    if(layer.IsVisible) {
                        layer.Render(Camera.Projection, Camera.View);
                    }
                }
                if(uiLayer.IsVisible) {
                    uiLayer.Render(_uiProjection);
                }
            }
            else {
                ref readonly var fbo = ref ppCompiled.GetFBO(_size);
                FBO.Bind(fbo, FBO.Target.FrameBuffer);         // Draw to fbo of post process
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                foreach(var layer in layers) {
                    if(layer.IsVisible) {
                        layer.Render(Camera.Projection, Camera.View);
                    }
                }
                FBO.Bind(screenFbo, FBO.Target.FrameBuffer);   // Draw to screen
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                ppCompiled.Render();
                if(uiLayer.IsVisible) {
                    uiLayer.Render(_uiProjection);
                }
            }

            // Apply FrameObject removed at previous frame.
            foreach(var layer in layers) {
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
            foreach(var layer in layers.AsReadOnlySpan()) {
                layer.ClearFrameObject();
            }
            layers.Clear();

            // Dispose resources of post process
            _postProcessImpl.Dispose();

            AsyncBack.AbortAll();
        }

        private void OnSizeChanged(in Vector2i newSize)
        {
            // Change view and projection matrix (World).
            Camera.ChangeScreenSize(newSize.X, newSize.Y);

            // Change projection matrix (UI)
            GL.Viewport(0, 0, newSize.X, newSize.Y);
            Matrix4.OrthographicProjection(0, newSize.X, 0, newSize.Y, UI_NEAR, UI_FAR, out _uiProjection);
            var uiRoot = Layers.UILayer.UIRoot;
            uiRoot.Width = newSize.X;
            uiRoot.Height = newSize.Y;

            Debug.WriteLine($"Size changed ({newSize.X}, {newSize.Y})");
        }
    }
}
