#nullable enable
using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Elffy.Shapes;
using Elffy.Shading;
using Elffy.OpenGL;
using Cysharp.Threading.Tasks;
using FbxTools;
using Elffy.Serialization.Fbx;

namespace Elffy.Serialization
{
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

            return Model3D.Create(obj, Build, RenderModel);
        }

        private static void RenderModel(Model3D model3D, in Matrix4 model, in Matrix4 view, in Matrix4 projection, Model3DDrawElementsDelegate drawElements)
        {
            VAO.Bind(model3D.VAO);
            IBO.Bind(model3D.IBO);
            model3D.ShaderProgram!.Apply(in model, in view, in projection);
            drawElements.Invoke(0, model3D.IBO.Length);
            VAO.Unbind();
            IBO.Unbind();
        }

        private static async UniTask Build(StateObject obj, Model3D model, Model3DLoadMeshDelegate load)
        {
            var (resourceLoader, name, token) = obj;
            token.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();
            // --------------------------------------
            //      ↓ thread pool

            token.ThrowIfCancellationRequested();

            // Parse fbx file
            using var fbx = FbxParser.Parse(resourceLoader.GetStream(name));
            FbxSemantics.GetMesh(fbx, out var vertices, out var indices, token);

            using(vertices)
            using(indices) {
                //      ↑ thread pool
                // --------------------------------------
                await model.HostScreen.AsyncBack.ToTiming(FrameLoopTiming.Update, token);
                // --------------------------------------
                //      ↓ main thread

                model.Shader = PhongShaderSource.Instance;

                if(model.LifeState == LifeState.Activated || model.LifeState == LifeState.Alive) {
                    load.Invoke(vertices.AsSpan(), indices.AsSpan());
                }
                //      ↑ main thread
                // --------------------------------------
                await UniTask.SwitchToThreadPool();
                // --------------------------------------
                //      ↓ thread pool
            }
            // 'using' scope ends here. Dispose resources on a thread pool. 
            // Nobody knows the thread if exceptions are thrown in this method,
            // but I don't care about that.
        }
    }
}
