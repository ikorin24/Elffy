﻿#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using Elffy.Core;
using System;
using System.Threading;

namespace Elffy
{
    /// <summary>Interface for abstracting screens for each platform</summary>
    public interface IHostScreen
    {
        /// <summary>Get or set title of the screen</summary>
        string Title { get; set; }

        /// <summary>Get mouse</summary>
        Mouse Mouse { get; }

        /// <summary>Get keyborad</summary>
        Keyboard Keyboard { get; }

        /// <summary>Get camera</summary>
        Camera Camera { get; }

        /// <summary>Get UI root</summary>
        RootPanel UIRoot { get; }

        /// <summary>Get asynchronous end point</summary>
        public AsyncBackEndPoint AsyncBack { get; }

        /// <summary>Get pixel size of rendering area.</summary>
        Vector2i ClientSize { get; set; }

        /// <summary>Get frame buffer size</summary>
        Vector2i FrameBufferSize { get; }

        /// <summary>Get location of the <see cref="IHostScreen"/></summary>
        Vector2i Location { get; set; }

        /// <summary>Get list of the layers which has <see cref="FrameObject"/>s</summary>
        LayerCollection Layers { get; }

        /// <summary>Get time of current frame. (This is NOT real time.)</summary>
        TimeSpan Time { get; }

        /// <summary>Get time span between frames</summary>
        TimeSpan FrameDelta { get; }

        /// <summary>Get number of current frame.</summary>
        long FrameNum { get; }

        /// <summary>Get screen running token, which is canceled when screen got closed.</summary>
        CancellationToken RunningToken { get; }

        /// <summary>Get whether screen is running</summary>
        bool IsRunning { get; }

        /// <summary>Get current screen frame loop timing.</summary>
        /// <remarks>If not main thread of <see cref="IHostScreen"/>, always returns <see cref="ScreenCurrentTiming.OutOfFrameLoop"/></remarks>
        ScreenCurrentTiming CurrentTiming { get; }

        /// <summary>Event which fires on initialized</summary>
        event Action<IHostScreen> Initialized;

        /// <summary>Event which fires on closing</summary>
        event ClosingEventHandler<IHostScreen> Closing;

        FrameAsyncEnumerable Frames(FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default);

        /// <summary>Acticate screen</summary>
        void Activate();

        /// <summary>Close screen</summary>
        void Close();

        /// <summary>Execute next frame</summary>
        void HandleOnce();
    }
}
