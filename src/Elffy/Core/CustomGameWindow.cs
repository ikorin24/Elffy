﻿#nullable enable
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Elffy.Core
{
    internal sealed class CustomGameWindow : NativeWindow
    {
        private const double MaxFrequency = 500.0;

        private readonly Stopwatch _watchRender = new Stopwatch();
        private readonly Stopwatch _watchUpdate = new Stopwatch();

        /// <summary>
        /// Gets a value indicating whether or not UpdatePeriod has consistently failed to reach TargetUpdatePeriod.
        /// This can be used to do things such as decreasing visual quality if the user's computer isn't powerful enough
        /// to handle the application.
        /// </summary>
        private bool _isRunningSlowly;

        private double _updateEpsilon; // quantization error for UpdateFrame events

        private double _renderFrequency;
        private double _updateFrequency;
        private VSyncMode _vSync;

        public bool IsMultiThreaded { get; }

        public double RenderFrequency
        {
            get => _renderFrequency;
            set
            {
                if(value <= 1.0) {
                    _renderFrequency = 0.0;
                }
                else if(value <= MaxFrequency) {
                    _renderFrequency = value;
                }
                else {
                    _renderFrequency = MaxFrequency;
                }
            }
        }

        public double UpdateFrequency
        {
            get => _updateFrequency;

            set
            {
                if(value < 1.0) {
                    _updateFrequency = 0.0;
                }
                else if(value <= MaxFrequency) {
                    _updateFrequency = value;
                }
                else {
                    _updateFrequency = MaxFrequency;
                }
            }
        }

        public VSyncMode VSync
        {
            get => _vSync;
            set
            {
                switch(value) {
                    case VSyncMode.On:
                        GLFW.SwapInterval(1);
                        break;

                    case VSyncMode.Off:
                        GLFW.SwapInterval(0);
                        break;

                    case VSyncMode.Adaptive:
                        GLFW.SwapInterval(_isRunningSlowly ? 0 : 1);
                        break;
                }

                _vSync = value;
            }
        }

        public event Action? Load;

        public event Action? Unload;

        public event Action<FrameEventArgs>? UpdateFrame;

        public event Action<FrameEventArgs>? RenderFrame;

        public CustomGameWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(nativeWindowSettings)
        {
            IsMultiThreaded = gameWindowSettings.IsMultiThreaded;
            //IsMultiThreaded = true;
            RenderFrequency = gameWindowSettings.RenderFrequency;
            UpdateFrequency = gameWindowSettings.UpdateFrequency;
        }

        public void Run()
        {
            IsVisible = true;
            Load?.Invoke();
            OnResize(new ResizeEventArgs(Size));
            if(IsMultiThreaded) {
                Task.Factory.StartNew(StartUpdateThread);       // TODO: Close 時に終了する
            }
            else {
                _watchUpdate.Start();
            }
            _watchRender.Start();
            while(true) {
                if(!Exists || IsExiting) { return; }

                if(!IsMultiThreaded) {
                    DispatchUpdateFrame();
                }
                DispatchRenderFrame();
                Thread.Sleep(1);    // TODO: アイドル時のCPU使用率を下げるのが目的だが時間を無駄にするのは困るので、直近の何フレームかの速度を見ていい感じにする
            }
        }

        private void StartUpdateThread()
        {
            _watchUpdate.Start();
            while(Exists && !IsExiting) {
                DispatchUpdateFrame();
            }
        }

        private void DispatchUpdateFrame()
        {
            var isRunningSlowlyRetries = 4;
            var elapsed = _watchUpdate.Elapsed.TotalSeconds;

            var updatePeriod = UpdateFrequency == 0 ? 0 : 1 / UpdateFrequency;

            while(elapsed > 0 && elapsed + _updateEpsilon >= updatePeriod) {
                ProcessEvents();

                _watchUpdate.Restart();
                UpdateFrame?.Invoke(new FrameEventArgs(elapsed));

                // Calculate difference (positive or negative) between
                // actual elapsed time and target elapsed time. We must
                // compensate for this difference.
                _updateEpsilon += elapsed - updatePeriod;

                if(UpdateFrequency <= double.Epsilon) {
                    // An UpdateFrequency of zero means we will raise
                    // UpdateFrame events as fast as possible (one event
                    // per ProcessEvents() call)
                    break;
                }

                _isRunningSlowly = _updateEpsilon >= updatePeriod;
                if(_isRunningSlowly && --isRunningSlowlyRetries == 0) {
                    // If UpdateFrame consistently takes longer than TargetUpdateFrame
                    // stop raising events to avoid hanging inside the UpdateFrame loop.
                    break;
                }

                elapsed = _watchUpdate.Elapsed.TotalSeconds;
            }

            // Update VSync if set to adaptive
            if(_vSync == VSyncMode.Adaptive) {
                GLFW.SwapInterval(_isRunningSlowly ? 0 : 1);
            }
        }

        private void DispatchRenderFrame()
        {
            var elapsed = _watchRender.Elapsed.TotalSeconds;
            var renderPeriod = RenderFrequency == 0 ? 0 : 1 / RenderFrequency;
            if(elapsed > 0 && elapsed >= renderPeriod) {
                _watchRender.Restart();
                RenderFrame?.Invoke(new FrameEventArgs(elapsed));
            }
        }

        /// <inheritdoc />
        public unsafe void SwapBuffers()
        {
            GLFW.SwapBuffers(WindowPtr);
        }

        /// <inheritdoc />
        public override void Close()
        {
            Unload?.Invoke();
            base.Close();
        }
    }
}