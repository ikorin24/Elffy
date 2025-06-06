﻿#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public static class ResourceFileExtension
    {
        public static Icon LoadIcon(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return IcoParser.Parse(stream);
        }

        public static UniTask<Icon> LoadIconAsync(this ResourceFile file, FrameTimingPoint? timingPoint,
                                                  CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(static file => LoadIcon(file), file,
                                 static icon => icon.Dispose(),
                                 timingPoint, cancellationToken);
        }

        public static Image LoadImage(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return Image.FromStream(stream, Image.GetTypeFromExt(file.FileExtension));
        }

        public static UniTask<Image> LoadImageAsync(this ResourceFile file, FrameTimingPoint? timingPoint,
                                                    CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(static file => LoadImage(file), file,
                                 static image => image.Dispose(),
                                 timingPoint, cancellationToken);
        }

        public static Texture LoadTexture(this ResourceFile file)
        {
            return LoadTexture(file, TextureConfig.Default);
        }

        public static Texture LoadTexture(this ResourceFile file, in TextureConfig config)
        {
            using var image = LoadImage(file);
            var texture = new Texture(config);
            texture.Load(image);
            return texture;
        }

        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file,
                                                        FrameTimingPoint timingPoint,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(file, TextureConfig.Default, timingPoint, cancellationToken);
        }

        public static async UniTask<Texture> LoadTextureAsync(this ResourceFile file, TextureConfig config,
                                                              FrameTimingPoint timingPoint,
                                                              CancellationToken cancellationToken = default)
        {
            if(timingPoint is null) { ThrowNullArg(nameof(timingPoint)); }
            using var image = await LoadImageAsync(file, timingPoint, cancellationToken);
            var texture = new Texture(config);
            texture.Load(image);
            return texture;
        }

        public static Typeface LoadTypeface(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return new Typeface(stream);
        }

        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file,
                                                          FrameTimingPoint? timingPoint,
                                                          CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(static file => LoadTypeface(file), file,
                                 static typeface => typeface.Dispose(),
                                 timingPoint, cancellationToken);
        }

        public unsafe static Mesh LoadMeshContainer(this ResourceFile file) => MeshResourceLoader.LoadMeshContainer(file);

        public static UniTask<Mesh> LoadMeshContainerAsync(this ResourceFile file, FrameTimingPoint? timingPoint, CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(
                static file => LoadMeshContainer(file), file,
                null, timingPoint, cancellationToken);
        }

        private static async UniTask<T> AsyncLoadCore<T, TState>(Func<TState, T> onTreadPool, TState state, Action<T>? onCatch,
                                                                 FrameTimingPoint? timingPoint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();

            var result = onTreadPool(state);
            if(timingPoint is not null) {
                try {
                    await timingPoint.Next(cancellationToken);
                }
                catch {
                    onCatch?.Invoke(result);
                    throw;
                }
            }
            return result;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
