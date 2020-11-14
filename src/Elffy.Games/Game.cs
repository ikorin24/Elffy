#nullable enable
using Elffy.AssemblyServices;
using Elffy.Core;
using Elffy.Imaging;
using Elffy.InputSystem;
using Elffy.Platforms;
using Elffy.Threading;
using Elffy.Threading.Tasks;
using Elffy.UI;
using Elffy.Effective;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Elffy
{
    public static class Game
    {
        private const string DefaultResource = "Resources.dat";

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
            Start(width, height, title, null, Path.GetFullPath(DefaultResource), initialize);
        }

        public static void Start(int width, int height, string title, string? iconName, Action initialize)
        {
            Start(width, height, title, iconName, Path.GetFullPath(DefaultResource), initialize);
        }

        public static void Start(int width, int height, string title, string? iconName, string resourceFilePath, Action initialize)
        {
            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
            ProcessHelper.SingleLaunch(Launch);

            void Launch()
            {
                try {
                    Engine.Run();
                    Resources.Initialize(path => new LocalResourceLoader(path), resourceFilePath);
                    IHostScreen screen;
                    if(iconName is null) {
                        screen = CreateScreen(width, height, title, null);
                    }
                    else {
                        using(var stream = Resources.Loader.GetStream(iconName)) {
                            screen = CreateScreen(width, height, title, stream);
                        }
                    }
                    screen.Initialized += InitScreen;
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

        private static unsafe IHostScreen CreateScreen(int width, int height, string title, Stream? iconStream)
        {
            Bitmap? iconBitmap = null;
            try {
                RawImage iconRawImage = default;

                // TODO: This is low performance, which makes a lot of garbages. It is better to create custom .ico parser.
                if(iconStream is not null) {
                    // Get icon raw image from stream.
                    iconBitmap = new Bitmap(iconStream);
                    using(var pixels = iconBitmap.GetPixels(ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)) {
                        var pixelSpan = pixels.AsSpan();

                        // Pixels of System.Drawing.Bitmap is layouted as (B, G, R, A).
                        // Convert them as (R, G, B, A)
                        for(int i = 0; i < pixelSpan.Length / 4; i++) {
                            var (r, g, b) = (pixelSpan[i * 4 + 2], pixelSpan[i * 4 + 1], pixelSpan[i * 4]);
                            pixelSpan[i * 4] = r;
                            pixelSpan[i * 4 + 1] = g;
                            pixelSpan[i * 4 + 2] = b;
                        }
                        iconRawImage = new RawImage(pixels.Width, pixels.Height, pixels.GetPtr<byte>());
                    }
                }
                var icon = MemoryMarshal.CreateReadOnlySpan(ref iconRawImage, 1);

                switch(Platform.PlatformType) {
                    case PlatformType.Windows:
                    case PlatformType.MacOSX:
                    case PlatformType.Unix:
                        return new Window(width, height, title, WindowStyle.Default, icon);
                    case PlatformType.Android:
                    case PlatformType.Other:
                    default:
                        throw new PlatformNotSupportedException();
                }
            }
            catch {
                iconBitmap?.Dispose();
                throw;
            }
        }
    }
}
