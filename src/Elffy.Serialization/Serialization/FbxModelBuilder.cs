#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Shapes;
using Elffy.Core;
using Elffy.Components;
using Elffy.Serialization.Fbx;
using Elffy.Serialization.Fbx.Internal;

namespace Elffy.Serialization
{
    /// <summary>Provides methods for creating <see cref="Model3D"/> from <see cref="IResourceLoader"/>.</summary>
    public static class FbxModelBuilder
    {
        private sealed record StateObject(IResourceLoader ResourceLoader, string Name, CancellationToken CancellationToken);

        /// <summary>Create <see cref="Model3D"/> instance from resource with lazy loading.</summary>
        /// <remarks>Loading will run after <see cref="FrameObject.Activate(Layer)"/> on thread pool.</remarks>
        /// <param name="resourceLoader">resource loader</param>
        /// <param name="name">resource name</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>new <see cref="Model3D"/> instance</returns>
        public static Model3D CreateLazyLoadingFbx(IResourceLoader resourceLoader, string name, CancellationToken cancellationToken = default)
        {
            if(resourceLoader is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(resourceLoader));
            }
            if(resourceLoader.HasResource(name) == false) {
                ThrowNotFound(name);
                [DoesNotReturn] static void ThrowNotFound(string name) => throw new ResourceNotFoundException(name);
            }

            var obj = new StateObject(resourceLoader, name, cancellationToken);

            return Model3D.Create(obj, Build);
        }

        private static async UniTask Build(StateObject obj, Model3D model, Model3DLoadMeshDelegate load)
        {
            var (resourceLoader, name, token) = obj;
            token.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();                                         // ↓ thread pool -------------------------------------- 
            token.ThrowIfCancellationRequested();


            // TODO:
            // - サブメッシュの実装 (メッシュごとにテクスチャを設定したい、実態は結合メッシュへのスライス、CPU側からのアクセスはとりあえず実装しない、表示/非表示切り替えもなし)
            // - マルチテクスチャの実装


            // Parse fbx file
            using var fbx = FbxSemanticParser<SkinnedVertex>.ParseUnsafe(resourceLoader, name, token);
            await model.HostScreen.AsyncBack.ToTiming(FrameLoopTiming.Update, token);   // ↓ main thread --------------------------------------

            await CreateTexture(resourceLoader, fbx, model);

            if(model.LifeState == LifeState.Activated || model.LifeState == LifeState.Alive) {
                load.Invoke(fbx.Vertices, fbx.Indices);
            }
            await UniTask.SwitchToThreadPool();                                         // ↓ thread pool -------------------------------------- 

            // 'using' scope ends here. Dispose resources on a thread pool. 
            // Nobody knows the thread if exceptions are thrown in this method,
            // but I don't care about that.
        }

        private static async UniTask CreateTexture(IResourceLoader resourceLoader, FbxSemanticsUnsafe<SkinnedVertex> fbx, Model3D model)
        {
            // ↓ main thread --------------------------------------
            var contextExist = model.TryGetHostScreen(out var screen);
            Debug.Assert(contextExist && Engine.CurrentContext == screen);
            var texture = new MultiTexture();
            model.AddComponent(texture);
            using var textureLoader = texture.GetLoaderContext(fbx.Textures.Length);
            try {
                for(int i = 0; i < fbx.Textures.Length; i++) {
                    var path = fbx.Textures[i].ToString().Replace('\\', '/');
                    Debug.WriteLine(path);
                    continue;

                    // TODO: パスの解決
                    var name = path;
                    using var image = await resourceLoader.LoadImageAsync(name, screen.AsyncBack, FrameLoopTiming.Update);
                    textureLoader.Load(i, image);
                }
            }
            catch {
                texture.Dispose();
                throw;
            }
        }
    }
}
