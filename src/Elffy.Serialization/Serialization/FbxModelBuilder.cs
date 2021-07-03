#nullable enable
using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Elffy.Shapes;
using Elffy.Shading;
using Cysharp.Threading.Tasks;
using Elffy.Serialization.Fbx;

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
            using var fbx = FbxSemanticParser.Parse(resourceLoader, name, token);
            CreateTextures(resourceLoader, fbx);

            static void CreateTextures(IResourceLoader resourceLoader, FbxSemantics fbx)
            {
                foreach(var textureName in fbx.Textures) {
                    var path = textureName.ToString().Replace('\\', '/');
                    System.Diagnostics.Debug.WriteLine(path);
                }
            }

            await model.HostScreen.AsyncBack.ToTiming(FrameLoopTiming.Update, token);   // ↓ main thread --------------------------------------
            if(model.LifeState == LifeState.Activated || model.LifeState == LifeState.Alive) {
                load.Invoke(fbx.Vertices, fbx.Indices);
            }
            await UniTask.SwitchToThreadPool();                                         // ↓ thread pool -------------------------------------- 

            // 'using' scope ends here. Dispose resources on a thread pool. 
            // Nobody knows the thread if exceptions are thrown in this method,
            // but I don't care about that.
        }
    }
}
