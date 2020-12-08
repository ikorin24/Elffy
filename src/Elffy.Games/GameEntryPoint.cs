#nullable enable
using Elffy.Core;
using Elffy.Imaging;
using Elffy.Platforms;
using Elffy.Threading;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    /// <summary>Game entry point class</summary>
    public static class GameEntryPoint
    {
        private static Action _initialize = null!;

        /// <summary>Start game</summary>
        /// <param name="width">game screen width</param>
        /// <param name="height">game screen height</param>
        /// <param name="title">game screen title</param>
        /// <param name="iconName">icon name in the resources. (null if no icon)</param>
        /// <param name="resourceFilePath">resource file path, which is relative path from the directory of entry assembly.</param>
        /// <param name="initialize"></param>
        public static void Start(int width, int height, string title, string? iconName, string? resourceFilePath, Action initialize)
        {
            if(resourceFilePath is null && iconName is not null) {
                throw new ArgumentNullException($"{nameof(iconName)} is assigned but {nameof(resourceFilePath)} is null.");
            }

            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
            ProcessHelper.SingleLaunch(Launch);

            void Launch()
            {
                try {
                    Engine.Run();
                    if(resourceFilePath is not null) {
                        Resources.Initialize(path => new LocalResourceLoader(path), resourceFilePath);
                    }
                    IHostScreen screen;
                    if(iconName is null) {
                        screen = CreateScreen(width, height, title, null);
                    }
                    else {
                        using(var stream = Resources.Loader.GetStream(iconName)) {
                            screen = CreateScreen(width, height, title, stream);
                        }
                    }
                    screen.Initialized += screen =>
                    {
                        SetScreen(screen);
                        _initialize();
                    };
                    CustomSynchronizationContext.CreateIfNeeded(out _, out var syncContextReciever);
                    screen.Show();

                    while(Engine.HandleOnce()) {
                        syncContextReciever?.DoAll();
                    }
                }
                //catch(Exception ex) {
                //    // TODO: logging of engine
                //}
                finally {
                    Resources.Close();
                    CustomSynchronizationContext.Restore();
                    Engine.Stop();
                }
            }
        }

        private static void SetScreen(IHostScreen screen)
        {
            if(screen is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(screen));
            }

            Timing.Initialize(screen.AsyncBack);
            Game.Initialize(screen);
            GameUI.Initialize(screen.UIRoot);
        }

        private static unsafe IHostScreen CreateScreen(int width, int height, string title, Stream? iconStream)
        {
            Bitmap? iconBitmap = null;
            try {
                Span<RawImage> icon = stackalloc RawImage[1];

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
                        icon[0] = new RawImage(pixels.Width, pixels.Height, pixels.GetPtr<byte>());
                    }
                }
                else {
                    icon = Span<RawImage>.Empty;
                }

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
