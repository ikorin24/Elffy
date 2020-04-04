#nullable enable
using Elffy.Exceptions;
using Elffy.InputSystem;
using Elffy.Threading;
using Elffy.UI;
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public static class CurrentScreen
    {
        public static Mouse Mouse { get; private set; } = null!;
        public static Camera Camera { get; private set; } = null!;
        public static Page UIRoot { get; private set; } = null!;
        public static Light Light { get; private set; } = null!;
        public static Dispatcher Dispatcher => Dispatcher.Current;      // TODO: 廃止予定
        public static LayerCollection Layers { get; private set; } = null!;
        public static TimeSpan Time { get; private set; }
        public static long FrameNum { get; private set; }

        internal static IHostScreen Screen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _screen ?? throw new InvalidOperationException($"Current {nameof(IHostScreen)} is null.");
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                _screen = value;
                Mouse = value.Mouse;
                Camera = value.Camera;
                UIRoot = value.UIRoot;
                Light = value.Light;
                //Dispatcher = value.Dispatcher;
                Layers = value.Layers;
                Time = value.Time;
                FrameNum = value.FrameNum;
            }
        }
        private static IHostScreen? _screen;
    }
}
