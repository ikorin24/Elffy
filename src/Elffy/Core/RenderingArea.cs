#nullable enable
using Elffy.InputSystem;
using Elffy.OpenGL;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Threading;

namespace Elffy.Core
{
    /// <summary>Implementation of <see cref="IHostScreen"/>, which provides operations of rendering.</summary>
    internal sealed class RenderingArea : IDisposable
    {
        const float UI_FAR = 1f;
        const float UI_NEAR = -1f;

        private bool _isCloseRequested;
        private bool _disposed;
        private Matrix4 _uiProjection;
        private Vector2i _frameBufferSize;
        private readonly CancellationTokenSource _runningTokenSource;

        [ThreadStatic]
        private ScreenCurrentTiming _currentTiming; // default value is 'OutOfFrameLoop'

        public event Action<IHostScreen>? Initialized;

        public event ClosingEventHandler<IHostScreen>? Closing;

        public event Action? Disposed;

        public CancellationToken RunningToken => _runningTokenSource.Token;

        public IHostScreen OwnerScreen { get; }

        public LayerCollection Layers { get; }
        public Camera Camera { get; } = new Camera();
        public Mouse Mouse { get; } = new Mouse();
        public Keyboard Keyboard { get; } = new Keyboard();

        public AsyncBackEndPoint AsyncBack { get; }

        public ScreenCurrentTiming CurrentTiming => _currentTiming;

        internal RenderingArea(IHostScreen screen)
        {
            OwnerScreen = screen;
            AsyncBack = new AsyncBackEndPoint(screen);
            Layers = new LayerCollection(this);
            _runningTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            var clearColor = Color4.Gray;
            GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            GL.Enable(EnableCap.DepthTest);

            // Enable alpha blending.
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Enable back face culling. front face is counter clockwise
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);

            GL.Disable(EnableCap.Multisample);  // I don't care about MSAA

            // Initialize viewport and so on.
            SetFrameBufferSize(OwnerScreen.FrameBufferSize);

            Layers.UILayer.Initialize();
            try {
                Initialized?.Invoke(OwnerScreen);
            }
            catch {
                // Don't throw. (Ignore exceptions in user code)
            }

            foreach(var layer in Layers.AsReadOnlySpan()) {
                layer.ApplyAdd();
            }
            Layers.UILayer.ApplyAdd();
        }

        /// <summary>Update and render the next frame</summary>
        public void RenderFrame()
        {
            var isLastFrame = _isCloseRequested;
            if(isLastFrame) {
                _runningTokenSource.Cancel();
            }
            _currentTiming = ScreenCurrentTiming.FrameInitializing;
            var uiLayer = Layers.UILayer;
            var layers = Layers.AsReadOnlySpan();

            Mouse.InitFrame();
            Keyboard.InitFrame();

            // Apply FrameObject added at previous frame.
            foreach(var layer in layers) {
                layer.ApplyAdd();
            }
            uiLayer.ApplyAdd();

            // UI hit test
            uiLayer.HitTest(Mouse);

            // Early update
            _currentTiming = ScreenCurrentTiming.EarlyUpdate;
            AsyncBack.DoQueuedEvents(FrameLoopTiming.EarlyUpdate);
            foreach(var layer in layers) {
                layer.EarlyUpdate();
            }
            uiLayer.EarlyUpdate();

            // Update
            _currentTiming = ScreenCurrentTiming.Update;
            AsyncBack.DoQueuedEvents(FrameLoopTiming.Update);
            foreach(var layer in layers) {
                layer.Update();
            }
            uiLayer.Update();

            // Late update
            _currentTiming = ScreenCurrentTiming.LateUpdate;
            AsyncBack.DoQueuedEvents(FrameLoopTiming.LateUpdate);
            foreach(var layer in layers) {
                layer.LateUpdate();
            }
            uiLayer.LateUpdate();

            // Render
            _currentTiming = ScreenCurrentTiming.BeforeRendering;
            FBO.Bind(FBO.Empty, FBO.Target.FrameBuffer);
            ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
            AsyncBack.DoQueuedEvents(FrameLoopTiming.BeforeRendering);
            foreach(var layer in layers) {
                if(layer.IsVisible) {
                    layer.Render(Camera.Projection, Camera.View);
                }
            }
            GL.Disable(EnableCap.DepthTest);
            if(uiLayer.IsVisible) {
                uiLayer.Render(_uiProjection);
            }
            GL.Enable(EnableCap.DepthTest);

            _currentTiming = ScreenCurrentTiming.AfterRendering;
            AsyncBack.DoQueuedEvents(FrameLoopTiming.AfterRendering);

            _currentTiming = ScreenCurrentTiming.FrameFinalizing;

            // Apply FrameObject removed at previous frame.
            foreach(var layer in layers) {
                layer.ApplyRemove();
            }
            uiLayer.ApplyRemove();

            _currentTiming = ScreenCurrentTiming.OutOfFrameLoop;

            ContextAssociatedMemorySafety.CollectIfExist(OwnerScreen);

            if(isLastFrame) {
                Dispose();
            }
        }

        public unsafe void RequestClose()
        {
            if(_isCloseRequested) {
                return;
            }
            var isCanceled = false;
            var e = new CancelEventArgs(&isCanceled);
            try {
                Closing?.Invoke(OwnerScreen, e);
                _isCloseRequested = e.Cancel == false;
            }
            catch {
                _isCloseRequested = true;
            }
        }

        public void Dispose()
        {
            if(_disposed) { return; }
            _disposed = true;

            _currentTiming = ScreenCurrentTiming.OutOfFrameLoop;

            // Clear objects in all layers
            var layers = Layers;
            foreach(var layer in layers.AsReadOnlySpan()) {
                layer.ClearFrameObject();
            }
            layers.Clear();

            AsyncBack.AbortAll();
            ContextAssociatedMemorySafety.EnsureCollect(OwnerScreen);   // Must be called before the opengl context is deleted.
            Disposed?.Invoke();
        }

        public void SetFrameBufferSize(in Vector2i frameBufferSize)
        {
            if(_frameBufferSize == frameBufferSize) { return; }
            _frameBufferSize = frameBufferSize;
            OnSizeChanged();
        }

        private void OnSizeChanged()
        {
            var size = _frameBufferSize;
            Camera.ChangeScreenSize(size.X, size.Y);

            GL.Viewport(0, 0, size.X, size.Y);
            Matrix4.OrthographicProjection(0, size.X, 0, size.Y, UI_NEAR, UI_FAR, out _uiProjection);
            Layers.UILayer.UIRoot.Size = size;
            Debug.WriteLine($"Size changed ({size.X}, {size.Y})");
        }
    }
}
