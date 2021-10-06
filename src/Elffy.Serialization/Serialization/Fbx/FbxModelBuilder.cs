#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Shapes;
using Elffy.Components;
using Elffy.Serialization.Fbx.Internal;
using Elffy.Effective;

namespace Elffy.Serialization.Fbx
{
    /// <summary>Provides methods for creating <see cref="Model3D"/> from <see cref="IResourceLoader"/>.</summary>
    public static class FbxModelBuilder
    {
        private sealed record StateObject(ResourceFile File, CancellationToken CancellationToken);

        public static Model3D CreateLazyLoadingFbx(ResourceFile file, CancellationToken cancellationToken = default)
        {
            ResourceNotFoundException.ThrowIfNotFound(file);
            var obj = new StateObject(file, cancellationToken);

            return Model3D.Create(obj, Build);
        }

        private static async UniTask Build(StateObject obj, Model3D model, Model3DLoadMeshDelegate load)
        {
            //var (resourceLoader, name, token) = obj;
            var (file, token) = obj;
            model.TryGetHostScreen(out var screen);
            Debug.Assert(screen is not null);
            var timingPoints = screen.TimingPoints;
            token.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();                                         // ↓ thread pool -------------------------------------- 
            token.ThrowIfCancellationRequested();


            // TODO:
            // - サブメッシュの実装 (メッシュごとにテクスチャを設定したい、実態は結合メッシュへのスライス、CPU側からのアクセスはとりあえず実装しない、表示/非表示切り替えもなし)
            // - マルチテクスチャの実装


            // Parse fbx file
            using var fbx = FbxSemanticParser<SkinnedVertex>.ParseUnsafe(file.GetStream(), false, token);
            await timingPoints.Update.Next(token);        // ↓ main thread --------------------------------------

            await CreateTexture(file, fbx, model);

            // Create a skeleton component
            if(fbx.Skeletons.IsEmpty == false) {
                var skeletonIndex = 0;
                var skeleton = new HumanoidSkeleton();
                model.AddComponent(skeleton);
                using var bones = new ValueTypeRentMemory<Bone>(fbx.Skeletons[skeletonIndex].BoneCount);
                fbx.Skeletons[skeletonIndex].CreateBones(bones.Span);
                await skeleton.LoadAsync(bones, timingPoints, cancellationToken: token);
            }

            load.Invoke(fbx.Vertices, fbx.Indices);
            await UniTask.SwitchToThreadPool();                                         // ↓ thread pool -------------------------------------- 

            // 'using' scope ends here. Dispose resources on a thread pool. 
            // Nobody knows the thread if exceptions are thrown in this method,
            // but I don't care about that.
        }

        private static async UniTask CreateTexture(ResourceFile file, FbxSemanticsUnsafe<SkinnedVertex> fbx, Model3D model)
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
                    //var name = path;
                    //using var image = await file.ResourceLoader.LoadImageAsync(name, screen.AsyncBack, FrameTiming.Update);
                    //textureLoader.Load(i, image);
                }
            }
            catch {
                (texture as IDisposable).Dispose();
                throw;
            }
        }
    }
}
