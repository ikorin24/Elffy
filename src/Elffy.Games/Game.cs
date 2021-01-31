#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Elffy.InputSystem;

namespace Elffy
{
    /// <summary>Provides a singleton instance of <see cref="IHostScreen"/></summary>
    public static class Game
    {
        private static IHostScreen? _screen;
        private static CancellationToken _runningToken;

        /// <summary>Get singleton instance of <see cref="IHostScreen"/></summary>
        public static IHostScreen Screen => _screen!;

        /// <summary>Get screen running token, which is canceled when screen got closed.</summary>
        public static CancellationToken RunningToken => _runningToken;

        /// <summary>Get time of current frame. (This is NOT real time.)</summary>
        public static TimeSpan Time => _screen!.Time;

        /// <summary>Get time span between frames</summary>
        public static TimeSpan FrameDelta => _screen!.FrameDelta;

        /// <summary>Get number of current frame.</summary>
        public static long FrameNum => _screen!.FrameNum;

        /// <summary>Get keyborad</summary>
        public static Keyboard Keyboard { get; private set; } = null!;

        /// <summary>Get mouse</summary>
        public static Mouse Mouse { get; private set; } = null!;

        /// <summary>Get layers</summary>
        public static LayerCollection Layers { get; private set; } = null!;

        /// <summary>Get camera</summary>
        public static Camera Camera { get; private set; } = null!;

        /// <summary>Return whether current thread is main thread of the game or not</summary>
        public static bool IsThreadMain => _screen!.IsThreadMain;

        /// <summary>Throw an exception if current thread is not main of the game</summary>
        public static void ThrowIfNotMainThread() => _screen!.ThrowIfNotMainThread();

        /// <summary>Close the game</summary>
        public static void Close()
        {
            _screen!.Close();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Initialize(IHostScreen screen)
        {
            Debug.Assert(_screen is null);
            _screen = screen;
            _runningToken = screen.RunningToken;
            Keyboard = screen.Keyboard;
            Mouse = screen.Mouse;
            Layers = screen.Layers;
            Camera = screen.Camera;
        }
    }
}
