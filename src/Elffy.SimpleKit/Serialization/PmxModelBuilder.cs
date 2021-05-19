#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Shapes;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Effective.Unsafes;
using Elffy.Components;
using Elffy.OpenGL;
using Elffy.Shading;
using Elffy.Core;
using Cysharp.Threading.Tasks;
using MMDTools.Unmanaged;
using PmxVector3 = MMDTools.Unmanaged.Vector3;

namespace Elffy.Serialization
{
    public static class PmxModelBuilder
    {
        private sealed record ModelState(IResourceLoader ResourceLoader, string Name, CancellationToken CancellationToken);

        public static Model3D CreateLazyLoadingPmx(IResourceLoader resourceLoader, string name, CancellationToken cancellationToken = default)
        {
            if(resourceLoader is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(resourceLoader));
            }
            if(resourceLoader.HasResource(name) == false) {
                ThrowNotFound(name);
                [DoesNotReturn] static void ThrowNotFound(string name) => throw new ResourceNotFoundException(name);
            }

            var obj = new ModelState(resourceLoader, name, cancellationToken);

            return Model3D.Create(obj, Build, RenderModel);
        }

        private static void RenderModel(Model3D model3D, in Matrix4 model, in Matrix4 view, in Matrix4 projection, Model3DDrawElementsDelegate drawElements)
        {
            var parts = model3D.GetComponent<PmxModelParts>();
            var vertexCountArray = parts.VertexCountArray;

            VAO.Bind(model3D.VAO);
            IBO.Bind(model3D.IBO);
            var shaderProgram = model3D.ShaderProgram;
            var start = 0;
            for(int i = 0; i < vertexCountArray.Length; i++) {
                parts.Current = i;
                if(parts.TextureIndexArray[i] >= 0) {
                    shaderProgram.Apply(in model, in view, in projection);
                    drawElements.Invoke(start, vertexCountArray[i]);
                }
                start += vertexCountArray[i];
            }
            VAO.Unbind();
            IBO.Unbind();
        }

        private static async UniTask Build(ModelState obj, Model3D model, Model3DLoadMeshDelegate load)
        {
            obj.CancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(model.LifeState == LifeState.Activated || model.LifeState == LifeState.Alive);

            // Run on thread pool
            await UniTask.SwitchToThreadPool();
            // ------------------------------
            //      ↓ thread pool

            obj.CancellationToken.ThrowIfCancellationRequested();

            // Parse pmx file
            var pmx = PMXParser.Parse(obj.ResourceLoader.GetStream(obj.Name));
            await BuildCore(pmx, model, load, obj);
        }

        private static UniTask BuildCore(PMXObject pmx, Model3D model, Model3DLoadMeshDelegate load, ModelState obj)
        {
            // ------------------------------
            //      ↓ thread pool
            Debug.Assert(Engine.IsThreadMain == false);

            var textureNames = pmx.TextureList.AsSpan();
            var dir = ResourcePath.GetDirectoryName(obj.Name);
            //var bitmaps = new RefTypeRentMemory<Bitmap?>(textureNames.Length);
            //var bitmapSpan = bitmaps.Span;
            var images = new Image[textureNames.Length];    // TODO:

            for(int i = 0; i < images.Length; i++) {
                using var _ = GetTexturePath(dir, textureNames[i], out var texturePath, out var ext);
                var path = texturePath.ToString();

                // Some pmx have the texture paths that don't exist. (Nobody references them.)
                // So skip them.
                if(obj.ResourceLoader.HasResource(path)) {
                    using var stream = obj.ResourceLoader.GetStream(path);
                    images[i] = Image.FromStream(stream, Image.GetTypeFromExt(ext));
                }
            }

            // [NOTE] Though pmx is read only, overwrite pmx data.
            PmxModelLoadHelper.ReverseTrianglePolygon(pmx!.SurfaceList.AsSpan().AsWritable());

            return LoadToModel(pmx, model, load, obj, images);
        }

        private static async UniTask LoadToModel(PMXObject pmx, Model3D model, Model3DLoadMeshDelegate load, ModelState obj, Image[] images)
        {
            // ------------------------------
            //      ↓ thread pool
            Debug.Assert(Engine.IsThreadMain == false);

            UnsafeRawArray<RigVertex> vertices = default;
            ValueTypeRentMemory<int> vertexCountArray = default;
            ValueTypeRentMemory<int> textureIndexArray = default;
            ValueTypeRentMemory<TextureObject> textures = default;
            UnsafeRawArray<Components.Bone> bones = default;
            try {
                (vertices, (vertexCountArray, textureIndexArray), bones) = await UniTask.WhenAll(
                    // build vertices
                    UniTask.Run(pmx =>
                    {
                        Debug.Assert(pmx is PMXObject);
                        var pmxVertexList = Unsafe.As<PMXObject>(pmx).VertexList.AsSpan();
                        var vertices = new UnsafeRawArray<RigVertex>(pmxVertexList.Length);
                        try {
                            for(int i = 0; i < pmxVertexList.Length; i++) {
                                vertices[i] = pmxVertexList[i].ToRigVertex();
                            }
                        }
                        catch {
                            vertices.Dispose();
                            throw;
                        }
                        return vertices;
                    }, pmx, configureAwait: false),
                    
                    // build each parts
                    UniTask.Run(pmx =>
                    {
                        Debug.Assert(pmx is PMXObject);
                        var materials = Unsafe.As<PMXObject>(pmx).MaterialList.AsSpan();
                        var vertexCountArray = new ValueTypeRentMemory<int>(materials.Length);
                        var textureIndexArray = new ValueTypeRentMemory<int>(materials.Length);
                        try {
                            var vSpan = vertexCountArray.Span;
                            var tSpan = textureIndexArray.Span;
                            for(int i = 0; i < materials.Length; i++) {
                                vSpan[i] = materials[i].VertexCount;
                                tSpan[i] = materials[i].Texture;
                            }
                        }
                        catch {
                            vertexCountArray.Dispose();
                            textureIndexArray.Dispose();
                            throw;
                        }
                        return (vertexCountArray, textureIndexArray);
                    }, pmx, configureAwait: false),

                    // build bones
                    UniTask.Run(pmx =>
                    {
                        Debug.Assert(pmx is PMXObject);
                        var pmxBones = Unsafe.As<PMXObject>(pmx).BoneList.AsSpan();
                        var bones = new UnsafeRawArray<Components.Bone>(pmxBones.Length);
                        try {
                            for(int i = 0; i < pmxBones.Length; i++) {
                                bones[i] = new(UnsafeEx.As<PmxVector3, Vector3>(in pmxBones[i].Position),
                                               pmxBones[i].ParentBone >= 0 ? pmxBones[i].ParentBone : null,
                                               pmxBones[i].ConnectedBone >= 0 ? pmxBones[i].ConnectedBone : null);
                            }
                        }
                        catch {
                            bones.Dispose();
                            throw;
                        }
                        return bones;
                    }, pmx, configureAwait: false));

                // create skeleton
                var screen = model.HostScreen;
                var skeleton = new HumanoidSkeleton();
                model.AddComponent(skeleton);
                await skeleton.LoadAsync(bones.AsSpanLike(), screen.AsyncBack);

                //      ↑ thread pool
                // ------------------------------
                await screen.AsyncBack.Ensure(FrameLoopTiming.Update, obj.CancellationToken);
                // ------------------------------
                //      ↓ main thread
                Debug.Assert(Engine.CurrentContext == screen);
                if(model.LifeState == LifeState.Activated || model.LifeState == LifeState.Alive) {

                    // create parts
                    textures = new ValueTypeRentMemory<TextureObject>(images.Length);
                    textures.Span.Clear();  // must be cleared
                    for(int i = 0; i < textures.Length; i++) {
                        var image = images[i];
                        if(image.IsEmpty) {
                            textures[i] = TextureObject.Empty;
                        }
                        else {
                            textures[i] = TextureLoadHelper.LoadByDMA(image, TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear,
                                                TextureMipmapMode.Bilinear, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
                        }
                        // Scadule the loading of textures to each frame.
                        await screen.AsyncBack.ToTiming(FrameLoopTiming.Update, obj.CancellationToken);
                    }
                    var partsComponent = new PmxModelParts(ref vertexCountArray, ref textureIndexArray, ref textures);
                    model.AddComponent(partsComponent);

                    // set shader
                    model.Shader = PmxModelShaderSource.Instance;

                    // load vertices and indices
                    load.Invoke(vertices.AsSpan().AsReadOnly(), pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>());
                }
                else {
                    DisposeObjects(textures, vertexCountArray, textureIndexArray);
                }
                //      ↑ main thread
                // ------------------------------
            }
            catch {
                // I don't care about the thread
                DisposeObjects(textures, vertexCountArray, textureIndexArray);
                throw;
            }
            finally {
                // I don't care about the thread.
                bones.Dispose();
                for(int i = 0; i < images.Length; i++) {
                    images[i].Dispose();
                }
                //bitmaps.Dispose();
                pmx.Dispose();
                vertices.Dispose();
            }

            static void DisposeObjects(in ValueTypeRentMemory<TextureObject> textures, in ValueTypeRentMemory<int> vertexCountArray, in ValueTypeRentMemory<int> textureIndexArray)
            {
                var texSpan = textures.Span;
                for(int i = 0; i < texSpan.Length; i++) {
                    TextureObject.Delete(ref texSpan[i]);
                }
                textures.Dispose();
                vertexCountArray.Dispose();
                textureIndexArray.Dispose();
            }
        }


        private static PooledArray<char> GetTexturePath(ReadOnlySpan<char> dir, ReadOnlyRawString name, out Span<char> texturePath, out ReadOnlySpan<char> ext)
        {
            var pooledArray = new PooledArray<char>(dir.Length + 1 + name.GetCharCount());
            try {
                texturePath = pooledArray.AsSpan();
                dir.CopyTo(texturePath);
                texturePath[dir.Length] = '/';
                name.ToString(texturePath.Slice(dir.Length + 1));
                texturePath.Replace('\\', '/');
                ext = ResourcePath.GetExtension(texturePath);

                return pooledArray;
            }
            catch {
                pooledArray.Dispose();
                throw;
            }
        }
    }
}
