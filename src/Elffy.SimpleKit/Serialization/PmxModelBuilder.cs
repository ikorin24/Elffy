#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Shapes;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Effective.Unsafes;
using Elffy.Components;
using Elffy.Graphics.OpenGL;
using Elffy.Shading.Forward;
using Cysharp.Threading.Tasks;
using MMDTools.Unmanaged;
using Elffy.Features;

namespace Elffy.Serialization
{
    public static class PmxModelBuilder
    {
        private sealed record ModelState(ResourceFile File, CancellationToken CancellationToken);

        private static readonly Model3DBuilderDelegate<ModelState> _build = Build;
        private static readonly Model3DRenderingDelegate _render = RenderModel;

        public static Model3D CreateLazyLoadingPmx(ResourceFile file, CancellationToken cancellationToken = default)
        {
            ResourceFile.ThrowArgumentExceptionIfInvalid(file);
            var obj = new ModelState(file, cancellationToken);
            return Model3D.Create(obj, _build, _render);
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
            Debug.Assert(model.LifeState == LifeState.Activating);

            // Run on thread pool
            await UniTask.SwitchToThreadPool();
            // ------------------------------
            //      ↓ thread pool

            obj.CancellationToken.ThrowIfCancellationRequested();
            using var pmx = PMXParser.Parse(obj.File.GetStream());

            // [NOTE] Though pmx is read only, overwrite pmx data.
            PmxModelLoadHelper.ReverseTrianglePolygon(pmx.SurfaceList.AsSpan().AsWritable());
            await LoadToModel(pmx, model, load, obj);
        }

        private static async UniTask LoadToModel(PMXObject pmx, Model3D model, Model3DLoadMeshDelegate load, ModelState obj)
        {
            // ------------------------------
            //      ↓ thread pool
            Debug.Assert(Engine.IsThreadMain == false);
            UnsafeRawArray<RigVertex> vertices = default;
            ValueTypeRentMemory<int> vertexCountArray = default;
            ValueTypeRentMemory<int> textureIndexArray = default;
            ValueTypeRentMemory<TextureObject> textures = default;
            UnsafeRawArray<Components.Bone> bones = default;
            var images = RefTypeRentMemory<IImageSource?>.Empty;
            try {
                (vertices, (vertexCountArray, textureIndexArray), bones, images) =
                    await UniTask.WhenAll(
                        BuildVertices(pmx),
                        BuildParts(pmx),
                        BuildBones(pmx),
                        LoadTextureImages(pmx, obj.File));

                // create skeleton
                model.TryGetHostScreen(out var screen);
                Debug.Assert(screen is not null);
                var skeleton = new HumanoidSkeleton();
                model.AddComponent(skeleton);
                await skeleton.LoadAsync(bones.AsSpanLike(), screen.TimingPoints);

                //      ↑ thread pool
                // ------------------------------
                await screen.TimingPoints.Update.NextOrNow(obj.CancellationToken);
                // ------------------------------
                //      ↓ main thread
                Debug.Assert(Engine.CurrentContext == screen);
                Debug.Assert(model.LifeState == LifeState.Activating);

                // create parts
                textures = new ValueTypeRentMemory<TextureObject>(images.Length, true); // It must be initialized by zero
                for(int i = 0; i < textures.Length; i++) {
                    var image = images[i];
                    if(image is null) {
                        textures[i] = TextureObject.Empty;
                    }
                    else {
                        textures[i] = TextureLoadHelper.LoadByDMA(image.AsImageRef(), TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear,
                                            TextureMipmapMode.Bilinear, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
                    }
                    // Scadule the loading of textures to each frame.
                    await screen.TimingPoints.Update.Next(obj.CancellationToken);
                }
                var partsComponent = new PmxModelParts(ref vertexCountArray, ref textureIndexArray, ref textures);
                model.AddComponent(partsComponent);

                // set shader
                model.Shader = PmxModelShader.Instance;

                // load vertices and indices
                load.Invoke(vertices.AsSpan().AsReadOnly(), pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>());

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
                    images[i]?.Dispose();
                }
                images.Dispose();
                vertices.Dispose();
            }

            static void DisposeObjects(in ValueTypeRentMemory<TextureObject> textures, in ValueTypeRentMemory<int> vertexCountArray, in ValueTypeRentMemory<int> textureIndexArray)
            {
                var texSpan = textures.AsSpan();
                for(int i = 0; i < texSpan.Length; i++) {
                    TextureObject.Delete(ref texSpan[i]);
                }
                textures.Dispose();
                vertexCountArray.Dispose();
                textureIndexArray.Dispose();
            }
        }

        private static async UniTask<UnsafeRawArray<RigVertex>> BuildVertices(PMXObject pmx)
        {
            await UniTask.SwitchToThreadPool();
            return await OnThreadPool(pmx);

            static UniTask<UnsafeRawArray<RigVertex>> OnThreadPool(PMXObject pmx)
            {
                // build vertices
                var pmxVertices = Unsafe.As<PMXObject>(pmx).VertexList.AsSpan();
                var vertices = new UnsafeRawArray<RigVertex>(pmxVertices.Length, false);
                try {
                    for(int i = 0; i < pmxVertices.Length; i++) {
                        vertices[i] = pmxVertices[i].ToRigVertex();
                    }
                }
                catch {
                    vertices.Dispose();
                    throw;
                }
                return UniTask.FromResult(vertices);
            }
        }

        private static async UniTask<(ValueTypeRentMemory<int>, ValueTypeRentMemory<int>)> BuildParts(PMXObject pmx)
        {
            await UniTask.SwitchToThreadPool();
            return await OnThreadPool(pmx);

            static UniTask<(ValueTypeRentMemory<int>, ValueTypeRentMemory<int>)> OnThreadPool(PMXObject pmx)
            {
                var materials = pmx.MaterialList.AsSpan();
                var vertexCountArray = new ValueTypeRentMemory<int>(materials.Length, false);
                var textureIndexArray = new ValueTypeRentMemory<int>(materials.Length, false);
                try {
                    var vSpan = vertexCountArray.AsSpan();
                    var tSpan = textureIndexArray.AsSpan();
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
                return UniTask.FromResult((vertexCountArray, textureIndexArray));
            }
        }

        private static async UniTask<UnsafeRawArray<Components.Bone>> BuildBones(PMXObject pmx)
        {
            await UniTask.SwitchToThreadPool();
            return await OnThreadPool(pmx);

            static UniTask<UnsafeRawArray<Components.Bone>> OnThreadPool(PMXObject pmx)
            {
                var pmxBones = pmx.BoneList.AsSpan();
                var bones = new UnsafeRawArray<Components.Bone>(pmxBones.Length, false);
                try {
                    for(int i = 0; i < pmxBones.Length; i++) {
                        var parentBone = pmxBones[i].ParentBone >= 0 ? pmxBones[i].ParentBone : (int?)null;
                        Vector3 boneVec;
                        var c = pmxBones[i].ConnectedBone;
                        if(c >= 0) {
                            boneVec = pmxBones[c].Position.ToVector3() - pmxBones[i].Position.ToVector3();
                        }
                        else {
                            boneVec = pmxBones[i].PositionOffset.ToVector3();
                        }

                        bones[i] = new Components.Bone();

                    }
                }
                catch {
                    bones.Dispose();
                    throw;
                }
                return UniTask.FromResult(bones);
            }
        }

        private static async UniTask<RefTypeRentMemory<IImageSource?>> LoadTextureImages(PMXObject pmx, ResourceFile pmxFile)
        {
            await UniTask.SwitchToThreadPool();
            return await OnThreadPool(pmx, pmxFile);

            static UniTask<RefTypeRentMemory<IImageSource?>> OnThreadPool(PMXObject pmx, ResourceFile pmxFile)
            {
                var dir = ResourcePath.GetDirectoryName(pmxFile.Name);
                var textureNames = pmx.TextureList.AsSpan();
                var resourceLoader = pmxFile.ResourceLoader;
                var images = new RefTypeRentMemory<IImageSource?>(textureNames.Length);
                try {
                    for(int i = 0; i < textureNames.Length; i++) {
                        using var _ = GetTexturePath(dir, textureNames[i], out var texturePath, out var ext);
                        var path = texturePath.ToString();

                        // Some pmx have the texture paths that don't exist. (Nobody references them.)
                        // So skip them.
                        if(resourceLoader.TryGetStream(path, out var stream)) {
                            try {
                                images[i] = Image.LoadToImageSource(stream, Image.GetTypeFromExt(ext));
                            }
                            finally {
                                stream.Dispose();
                            }
                        }
                    }
                    return UniTask.FromResult(images);
                }
                catch {
                    foreach(var image in images.AsSpan()) {
                        image?.Dispose();
                    }
                    throw;
                }
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
