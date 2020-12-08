#nullable enable
using System;
using System.Diagnostics;
using Elffy.InputSystem;

namespace Elffy
{
    /// <summary>Provides a singleton instance of <see cref="IHostScreen"/></summary>
    public static class Game
    {
        private static IHostScreen? _screen;

        /// <summary>Get singleton instance of <see cref="IHostScreen"/></summary>
        public static IHostScreen Screen => _screen!;

        /// <summary>Get time of current frame. (This is NOT real time.)</summary>
        public static ref readonly TimeSpan Time => ref _screen!.Time;

        /// <summary>Get number of current frame.</summary>
        public static ref readonly long FrameNum => ref _screen!.FrameNum;

        /// <summary>Get keyborad</summary>
        public static Keyboard Keyboard { get; private set; } = null!;

        /// <summary>Get mouse</summary>
        public static Mouse Mouse { get; private set; } = null!;

        /// <summary>Get layers</summary>
        public static LayerCollection Layers { get; private set; } = null!;

        /// <summary>Get camera</summary>
        public static Camera Camera { get; private set; } = null!;

        internal static void Initialize(IHostScreen screen)
        {
            Debug.Assert(_screen is null);
            _screen = screen;
            Keyboard = screen.Keyboard;
            Mouse = screen.Mouse;
            Layers = screen.Layers;
            Camera = screen.Camera;
        }
    }
}
