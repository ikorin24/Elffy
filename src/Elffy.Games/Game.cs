#nullable enable
using Elffy.AssemblyServices;
using Elffy.Core;
using Elffy.InputSystem;
using Elffy.Platforms;
using Elffy.Threading;
using Elffy.Threading.Tasks;
using Elffy.UI;
using System;
using System.IO;

namespace Elffy
{
    public static class Game
    {
        private static SyncContextReceiver? _syncContextReciever;
        private static Action _initialize = null!;

        public static IHostScreen Screen { get; private set; } = null!;
        public static Layer WorldLayer { get; private set; } = null!;
        public static Camera MainCamera { get; private set; } = null!;
        public static Mouse Mouse { get; private set; } = null!;
        public static Keyboard Keyboard { get; private set; } = null!;
        public static AsyncBackEndPoint AsyncBack { get; private set; } = null!;
        public static ControlCollection UI { get; private set; } = null!;

        /// <summary>Get time of current frame. (This is NOT real time.)</summary>
        public static ref readonly TimeSpan Time => ref Screen.Time;
        
        /// <summary>Get number of current frame.</summary>
        public static ref readonly long FrameNum => ref Screen.FrameNum;

        public static void Start(int width, int height, string title, Action initialize)
        {
            Start(width, height, title, Path.GetFullPath("Resources.dat"), initialize);
        }

        public static void Start(int width, int height, string title, string resourceFilePath, Action initialize)
        {
            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
            ProcessHelper.SingleLaunch(Launch);

            void Launch()
            {
                try {
                    var screen = CreateScreen(width, height, title);
                    screen.Initialized += InitScreen;
                    Engine.Run();
                    Resources.Initialize(path => new LocalResourceLoader(path), resourceFilePath);
                    CustomSynchronizationContext.CreateIfNeeded(out _, out _syncContextReciever);
                    screen.Show();

                    while(Engine.HandleOnce()) {
                        _syncContextReciever?.DoAll();
                    }
                }
                finally {
                    Resources.Close();
                    CustomSynchronizationContext.Restore();
                    _syncContextReciever = null;
                    Engine.Stop();
                }
            }
        }

        private static void InitScreen(IHostScreen screen)
        {
            Screen = screen;

            // Cache each fields to avoid accessing via interface.
            WorldLayer = screen.Layers.WorldLayer;
            MainCamera = screen.Camera;
            Mouse = screen.Mouse;
            Keyboard = screen.Keyboard;
            UI = screen.UIRoot.Children;
            AsyncBack = screen.AsyncBack;
            
            _initialize();
        }

        private static IHostScreen CreateScreen(int width, int height, string title)
        {
            switch(Platform.PlatformType) {
                case PlatformType.Windows:
                case PlatformType.MacOSX:
                case PlatformType.Unix:
                    return new Window(width, height, title, WindowStyle.Default);
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
