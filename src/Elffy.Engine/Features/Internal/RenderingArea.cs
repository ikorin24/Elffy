#nullable enable
using Elffy.InputSystem;
using Elffy.Graphics.OpenGL;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Threading;

namespace Elffy.Features.Internal
{
    /// <summary>Implementation of <see cref="IHostScreen"/>, which provides operations of rendering.</summary>
    internal sealed class RenderingArea : IDisposable
    {
        private readonly CancellationTokenSource _runningTokenSource;
        private bool _disposed;
        private LifeState _state;
        private bool _isCloseRequested;
        private int _runningThreadId;
        private Vector2i _frameBufferSize;
        private CurrentFrameTiming _currentTiming;

        public event Action<IHostScreen>? Initialized;

        public event ClosingEventHandler<IHostScreen>? Closing;

        public event Action? Disposed;

        public CancellationToken RunningToken => _runningTokenSource.Token;

        public IHostScreen OwnerScreen { get; }

        [Obsolete("", true)]
        public LayerCollection Layers => throw new NotImplementedException();

        public RenderPipeline RenderPipeline { get; }
        public Camera Camera { get; } = new Camera();
        public Mouse Mouse { get; } = new Mouse();
        public Keyboard Keyboard { get; } = new Keyboard();

        public FrameTimingPointList TimingPoints { get; }

        public LightManager Lights { get; }

        public CurrentFrameTiming CurrentTiming => (_runningThreadId == ThreadHelper.CurrentThreadId) ? _currentTiming : CurrentFrameTiming.OutOfFrameLoop;

        internal RenderingArea(IHostScreen screen)
        {
            _state = LifeState.New;
            OwnerScreen = screen;
            TimingPoints = new FrameTimingPointList(screen);
            Lights = new LightManager(screen);
            RenderPipeline = new RenderPipeline(this);
            _runningTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            _state = LifeState.Activating;
            _runningThreadId = ThreadHelper.CurrentThreadId;
            InitializeGL();

            // Initialize viewport and so on.
            SetFrameBufferSize(OwnerScreen.FrameBufferSize);

            Lights.Initialize();
        }

        private void InitializeGL()
        {
            GL.ClearColor(0, 0, 0, 0);
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

        /// <summary>Update and render the next frame</summary>
        public void RenderFrame()
        {
            // ------------------------------------------------------------
            // Out of frame loop
            Debug.Assert(_currentTiming == CurrentFrameTiming.OutOfFrameLoop);
            var isCloseRequested = _isCloseRequested;
            var pipeline = RenderPipeline;

            var frameTimingPoints = TimingPoints;
            Mouse.InitFrame();
            Keyboard.InitFrame();

            // ------------------------------------------------------------
            // First Frame initializing
            if(_state == LifeState.Activating) {
                _currentTiming = CurrentFrameTiming.FirstFrameInitializing;
                try {
                    Initialized?.Invoke(OwnerScreen);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
                finally {
                    _state = LifeState.Alive;
                }
            }

            // ------------------------------------------------------------
            // Frame initializing
            _currentTiming = CurrentFrameTiming.FrameInitializing;
            if(isCloseRequested && _state == LifeState.Alive) {
                _state = LifeState.Terminating;
                _runningTokenSource.Cancel();
                pipeline.TerminateAllOperations(this,
                    onDead: static self =>
                {
                    self._state = LifeState.Dead;
                });
            }
            pipeline.ApplyAdd();
            frameTimingPoints.FrameInitializing.DoQueuedEvents();

            // ------------------------------------------------------------
            // Early update
            _currentTiming = CurrentFrameTiming.EarlyUpdate;
            frameTimingPoints.EarlyUpdate.DoQueuedEvents();
            pipeline.EarlyUpdate();

            // ------------------------------------------------------------
            // Update
            _currentTiming = CurrentFrameTiming.Update;
            frameTimingPoints.Update.DoQueuedEvents();
            pipeline.Update();

            // ------------------------------------------------------------
            // Late update
            _currentTiming = CurrentFrameTiming.LateUpdate;
            frameTimingPoints.LateUpdate.DoQueuedEvents();
            pipeline.LateUpdate();

            // ------------------------------------------------------------
            // Before rendering
            _currentTiming = CurrentFrameTiming.BeforeRendering;
            FBO.Bind(FBO.Empty, FBO.Target.FrameBuffer);
            ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
            frameTimingPoints.BeforeRendering.DoQueuedEvents();

            // ------------------------------------------------------------
            // Rendering
            _currentTiming = CurrentFrameTiming.Rendering;
            pipeline.Render();

            // ------------------------------------------------------------
            // After rendering
            _currentTiming = CurrentFrameTiming.AfterRendering;
            frameTimingPoints.AfterRendering.DoQueuedEvents();

            // ------------------------------------------------------------
            // Frame finalizing
            _currentTiming = CurrentFrameTiming.FrameFinalizing;
            frameTimingPoints.FrameFinalizing.DoQueuedEvents();
            pipeline.ApplyRemove();

            // ------------------------------------------------------------
            // End of frame (only internal accessible)
            _currentTiming = CurrentFrameTiming.Internal_EndOfFrame;
            frameTimingPoints.InternalEndOfFrame.DoQueuedEvents();

            // ------------------------------------------------------------
            // Out of frame loop
            _currentTiming = CurrentFrameTiming.OutOfFrameLoop;
            ContextAssociatedMemorySafety.CollectIfExist(OwnerScreen);

            if(_state == LifeState.Dead) {
                Dispose();
            }
        }

        public unsafe bool RequestClose()
        {
            if(_isCloseRequested) {
                return false;
            }
            _isCloseRequested = true;
            var isCanceled = false;
            var e = new CancelEventArgs(&isCanceled);
            try {
                Closing?.Invoke(OwnerScreen, e);
                if(isCanceled) {
                    _isCloseRequested = false;
                    return false;
                }
                else {
                    return true;
                }
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                _isCloseRequested = true;
                return true;
            }
        }

        public void Dispose()
        {
            if(_disposed) { return; }
            _disposed = true;

            _currentTiming = CurrentFrameTiming.OutOfFrameLoop;

            RenderPipeline.AbortAllOperations();

            TimingPoints.AbortAllEvents();
            Lights.Release();
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

            var pipeline = RenderPipeline;
            pipeline.NotifySizeChanged();
        }
    }
}
