#nullable enable
using Elffy.InputSystem;
using Elffy.Graphics.OpenGL;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Threading;
using Elffy.Shading;

namespace Elffy.Features.Internal
{
    /// <summary>Implementation of <see cref="IHostScreen"/>, which provides operations of rendering.</summary>
    internal sealed class RenderingArea : IDisposable
    {
        private readonly CancellationTokenSource _runningTokenSource;
        private bool _isCloseRequested;
        private bool _disposed;
        private bool _initializedEventCalled;
        private int _runningThreadId;
        private Vector2i _frameBufferSize;
        private Color4 _clearColor;
        private CurrentFrameTiming _currentTiming;

        public event Action<IHostScreen>? Initialized;

        public event ClosingEventHandler<IHostScreen>? Closing;

        public event Action? Disposed;

        public CancellationToken RunningToken => _runningTokenSource.Token;

        public IHostScreen OwnerScreen { get; }

        public LayerCollection Layers { get; }
        public Camera Camera { get; } = new Camera();
        public Mouse Mouse { get; } = new Mouse();
        public Keyboard Keyboard { get; } = new Keyboard();

        public FrameTimingPointList TimingPoints { get; }

        public LightManager Lights { get; }

        public CurrentFrameTiming CurrentTiming => (_runningThreadId == ThreadHelper.CurrentThreadId) ? _currentTiming : CurrentFrameTiming.OutOfFrameLoop;

        public Color4 ClearColor
        {
            get => _clearColor;
            set
            {
                _clearColor = value;
                GL.ClearColor(value.R, value.G, value.B, value.A);
            }
        }

        internal RenderingArea(IHostScreen screen)
        {
            _clearColor = Color4.Black;
            OwnerScreen = screen;
            TimingPoints = new FrameTimingPointList(screen);
            Lights = new LightManager(screen);
            Layers = new LayerCollection(this);
            _runningTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            _runningThreadId = ThreadHelper.CurrentThreadId;
            InitializeGL();

            // Initialize viewport and so on.
            SetFrameBufferSize(OwnerScreen.FrameBufferSize);
        }

        private void InitializeGL()
        {
            var clearColor = _clearColor;
            GL.ClearColor(clearColor.R, _clearColor.G, _clearColor.B, _clearColor.A);
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

        private void InvokeInitializedEvent()
        {
            try {
                Initialized?.Invoke(OwnerScreen);
            }
            catch {
                // Don't throw. (Ignore exceptions in user code)
            }
        }

        /// <summary>Update and render the next frame</summary>
        public void RenderFrame()
        {
            // ------------------------------------------------------------
            // Out of frame loop
            Debug.Assert(_currentTiming == CurrentFrameTiming.OutOfFrameLoop);
            var isLastFrame = _isCloseRequested;
            if(isLastFrame) {
                _runningTokenSource.Cancel();
            }

            var frameTimingPoints = TimingPoints;
            var layers = Layers;
            Mouse.InitFrame();
            Keyboard.InitFrame();

            // ------------------------------------------------------------
            // First Frame initializing
            if(_initializedEventCalled == false) {
                _initializedEventCalled = true;
                _currentTiming = CurrentFrameTiming.FirstFrameInitializing;
                InvokeInitializedEvent();
            }

            // ------------------------------------------------------------
            // Frame initializing
            _currentTiming = CurrentFrameTiming.FrameInitializing;
            layers.ApplyAdd();
            frameTimingPoints.FrameInitializing.DoQueuedEvents();

            // ------------------------------------------------------------
            // Early update
            _currentTiming = CurrentFrameTiming.EarlyUpdate;
            frameTimingPoints.EarlyUpdate.DoQueuedEvents();
            layers.EarlyUpdate();

            // ------------------------------------------------------------
            // Update
            _currentTiming = CurrentFrameTiming.Update;
            frameTimingPoints.Update.DoQueuedEvents();
            layers.Update();

            // ------------------------------------------------------------
            // Late update
            _currentTiming = CurrentFrameTiming.LateUpdate;
            frameTimingPoints.LateUpdate.DoQueuedEvents();
            layers.LateUpdate();

            // ------------------------------------------------------------
            // Before rendering
            _currentTiming = CurrentFrameTiming.BeforeRendering;
            FBO.Bind(FBO.Empty, FBO.Target.FrameBuffer);
            ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
            frameTimingPoints.BeforeRendering.DoQueuedEvents();

            // ------------------------------------------------------------
            // Rendering
            _currentTiming = CurrentFrameTiming.Rendering;
            layers.Render();

            // ------------------------------------------------------------
            // After rendering
            _currentTiming = CurrentFrameTiming.AfterRendering;
            frameTimingPoints.AfterRendering.DoQueuedEvents();

            // ------------------------------------------------------------
            // Frame finalizing
            _currentTiming = CurrentFrameTiming.FrameFinalizing;
            layers.ApplyRemove();

            // ------------------------------------------------------------
            // End of frame (only internal accessible)
            _currentTiming = CurrentFrameTiming.Internal_EndOfFrame;
            frameTimingPoints.InternalEndOfFrame.DoQueuedEvents();

            // ------------------------------------------------------------
            // Out of frame loop
            _currentTiming = CurrentFrameTiming.OutOfFrameLoop;
            ContextAssociatedMemorySafety.CollectIfExist(OwnerScreen);
            if(isLastFrame) {
                Dispose();
            }
        }

        public unsafe bool RequestClose()
        {
            if(_isCloseRequested) {
                return false;
            }
            var isCanceled = false;
            var e = new CancelEventArgs(&isCanceled);
            try {
                Closing?.Invoke(OwnerScreen, e);
                _isCloseRequested = !isCanceled;
                return !isCanceled;
            }
            catch {
                _isCloseRequested = true;
                return true;
            }
        }

        public void Dispose()
        {
            if(_disposed) { return; }
            _disposed = true;

            _currentTiming = CurrentFrameTiming.OutOfFrameLoop;

            var layers = Layers;
            layers.TerminateAllImmediately();

            TimingPoints.AbortAllEvents();
            Lights.ReleaseBuffer();
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
            Debug.WriteLine($"Size changed ({size.X}, {size.Y})");

            var layers = Layers;
            layers.NotifySizeChanged();
        }
    }
}
