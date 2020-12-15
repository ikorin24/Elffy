#nullable enable
using Elffy.Core;
using Elffy.Imaging;
using Elffy.Platforms;
using Elffy.Threading;
using Elffy.Exceptions;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    /// <summary>Game entry point class</summary>
    public static class GameEntryPoint
    {
        private static Func<UniTask> _entryPointAsync = null!;

        /// <summary>Start game</summary>
        /// <param name="width">game screen width</param>
        /// <param name="height">game screen height</param>
        /// <param name="title">game screen title</param>
        /// <param name="iconName">icon name in the resources. (null if no icon)</param>
        /// <param name="resourceFilePath">resource file path, which is relative path from the directory of entry assembly.</param>
        /// <param name="entryPointSync">entry point of the game</param>
        public static void Start(int width, int height, string title, string? iconName, string? resourceFilePath, Action entryPointSync)
        {
            UniTask EntryPointAsync()
            {
                try {
                    entryPointSync();
                }
                catch(Exception ex) {
                    return UniTask.FromException(ex);
                }
                return UniTask.CompletedTask;
            }
            Start(width, height, title, iconName, resourceFilePath, EntryPointAsync);
        }

        /// <summary>Start game</summary>
        /// <param name="width">game screen width</param>
        /// <param name="height">game screen height</param>
        /// <param name="title">game screen title</param>
        /// <param name="iconName">icon name in the resources. (null if no icon)</param>
        /// <param name="resourceFilePath">resource file path, which is relative path from the directory of entry assembly.</param>
        /// <param name="entryPointAsync">entry point of the game</param>
        public static void Start(int width, int height, string title, string? iconName, string? resourceFilePath, Func<UniTask> entryPointAsync)
        {
            if(resourceFilePath is null && iconName is not null) {
                ThrowNullResourceFile();
                [DoesNotReturn] static void ThrowNullResourceFile() => throw new ArgumentNullException($"{nameof(iconName)} is assigned but {nameof(resourceFilePath)} is null.");
            }
            if(entryPointAsync is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(entryPointAsync));
            }

            _entryPointAsync = entryPointAsync;
            ProcessHelper.SingleLaunch(Launch, (width, height, title, iconName, resourceFilePath));

            static void Launch(in (int width, int height, string title, string? iconName, string? resourceFilePath) arg)
            {
                IHostScreen? screen = null;
                try {
                    Engine.Run();
                    if(arg.resourceFilePath is not null) {
                        Resources.Initialize(path => new LocalResourceLoader(path), arg.resourceFilePath);
                    }
                    using(var stream = arg.iconName is null ? Stream.Null : Resources.Loader.GetStream(arg.iconName)) {
                        screen = CreateScreen(arg.width, arg.height, arg.title, stream);
                    }
                    screen.Initialized += OnScreenInitialized;
                    CustomSynchronizationContext.CreateIfNeeded(out _, out var syncContextReciever);
                    screen.Show();

                    while(Engine.HandleOnce()) {
                        syncContextReciever?.DoAll();
                    }
                    ScreenExceptionHolder.ThrowIfExceptionExists(screen);
                }
                //catch {
                //    // TODO: logging here
                //    throw;
                //}
                finally {
                    Resources.Close();
                    CustomSynchronizationContext.Restore();
                    Engine.Stop();
                }
            }
        }

        private static async void OnScreenInitialized(IHostScreen screen)
        {
            Timing.Initialize(screen);
            Game.Initialize(screen);
            GameUI.Initialize(screen.UIRoot);
            try {
                await _entryPointAsync();
            }
            catch(Exception ex) {
                ScreenExceptionHolder.SetException(screen, ex);
                screen.Dispose();
                // Don't throw. No one catch it.
            }
        }

        private static unsafe IHostScreen CreateScreen(int width, int height, string title, Stream iconStream)
        {
            Bitmap? iconBitmap = null;
            try {
                Span<RawImage> icon = stackalloc RawImage[1];

                // TODO: This is low performance, which makes a lot of garbages. It is better to create custom .ico parser.
                if(!ReferenceEquals(iconStream, Stream.Null)) {
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
