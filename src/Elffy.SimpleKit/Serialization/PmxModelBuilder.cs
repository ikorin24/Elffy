#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using Elffy.Shapes;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Effective.Unsafes;
using Elffy.Components;
using Elffy.Shading.Forward;
using Cysharp.Threading.Tasks;
using MMDTools.Unmanaged;

namespace Elffy.Serialization
{
    public static class PmxModelBuilder
    {
        private sealed record ModelState(ResourceFile File, CancellationToken CancellationToken);

        private static readonly Model3DBuilderDelegate<ModelState> _build = Build;

        public static Model3D CreateLazyLoadingPmx(ResourceFile file, CancellationToken cancellationToken = default)
        {
            ResourceFile.ThrowArgumentExceptionIfInvalid(file);
            var obj = new ModelState(file, cancellationToken);
            return Model3D.Create(obj, _build);
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
            using var modelData = await UniTask.WhenAll(
                    BuildVertices(pmx),
                    BuildBones(pmx),
                    LoadTextureImages(pmx, obj.File))
                .ContinueWith(d => new ModelData(d.Item1, d.Item2, d.Item3));

            // create skeleton
            model.TryGetHostScreen(out var screen);
            Debug.Assert(screen is not null);

            // Don't make the followings parallel like UniTask.WhenAll.
            // ModelData will be disposed when an exception is throw, but another parallel code could not know that.
            await BuildAndAttachArrayTexture(model, modelData.Images.AsSpan(), new Vector2i(1024, 1024), screen.TimingPoints.Update, obj.CancellationToken);
            await UniTask.SwitchToThreadPool();
            var skeleton = new HumanoidSkeleton();
            model.AddComponent(skeleton);
            await skeleton.LoadAsync(modelData.Bones.AsSpanLike(), screen.TimingPoints, obj.CancellationToken);

            //      ↑ thread pool
            // ------------------------------
            await screen.TimingPoints.Update.NextOrNow(obj.CancellationToken);
            // ------------------------------
            //      ↓ main thread
            Debug.Assert(Engine.CurrentContext == screen);
            Debug.Assert(model.LifeState == LifeState.Activating);

            model.Shader = PmxModelShader.Instance;

            // load vertices and indices
            load.Invoke(modelData.Vertices.AsSpan().AsReadOnly(), pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>());

            //      ↑ main thread
            // ------------------------------
        }

        private static async UniTask<UnsafeRawArray<SkinnedVertex>> BuildVertices(PMXObject pmx)
        {
            await UniTask.SwitchToThreadPool();
            return await OnThreadPool(pmx);

            static UniTask<UnsafeRawArray<SkinnedVertex>> OnThreadPool(PMXObject pmx)
            {
                // build vertices
                var pmxVertices = pmx.VertexList.AsSpan();
                var materials = pmx.MaterialList.AsSpan();
                var vertices = new UnsafeRawArray<SkinnedVertex>(pmxVertices.Length, false);
                try {

                    for(int i = 0; i < pmxVertices.Length; i++) {
                        vertices[i] = pmxVertices[i].ToRigVertex();
                    }

                    {
                        var indices = pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>();
                        int i = 0;
                        foreach(var mat in materials) {
                            for(int j = 0; j < mat.VertexCount; j++) {
                                vertices[indices[i++]].TextureIndex = mat.Texture;
                            }
                        }
                    }
                }
                catch {
                    vertices.Dispose();
                    throw;
                }
                return UniTask.FromResult(vertices);
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

                //var materials = pmx.MaterialList.AsSpan();
                //using var matTexMem = new ValueTypeRentMemory<int>(materials.Length, true);
                //var matTex = matTexMem.AsSpan();
                //for(int i = 0; i < materials.Length; i++) {
                //    matTex[i] = materials[i].Texture;
                //}

                try {
                    for(int i = 0; i < textureNames.Length; i++) {
                        //if(matTex.Contains(i) == false) { continue; }

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
                        else {
                            Debug.WriteLine($"not exist: {path}");
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

        private static UniTask BuildAndAttachArrayTexture(Model3D model, ReadOnlySpan<IImageSource?> images, Vector2i size, FrameTimingPoint timingPoint, CancellationToken ct)
        {
            var arrayTexture = new ArrayTexture(TextureConfig.BilinearRepeat);
            model.AddComponent(arrayTexture);
            if(images.IsEmpty) {
                return UniTask.CompletedTask;
            }
            var count = 0;
            var buffer = new ValueTypeRentMemory<ColorByte>(size.X * size.Y * images.Length, false);
            try {
                foreach(var image in images) {
                    ct.ThrowIfCancellationRequested();
                    if(image == null) { continue; }
                    var dest = buffer.AsSpan(count * size.X * size.Y);
                    if(image.Width != size.X || image.Height != size.Y) {
                        using var resized = Image.Resized(image, size);
                        resized.GetPixels().CopyTo(dest);
                    }
                    else {
                        image.GetPixels().CopyTo(dest);
                    }
                    count++;
                }
            }
            catch {
                buffer.Dispose();
                throw;
            }
            return BackToMainThread(arrayTexture, new Vector3i(size.X, size.Y, count), buffer, timingPoint, ct);

            static async UniTask BackToMainThread(ArrayTexture arrayTexture, Vector3i size, ValueTypeRentMemory<ColorByte> buffer, FrameTimingPoint timingPoint, CancellationToken ct)
            {
                try {
                    await timingPoint.Next(ct);
                    arrayTexture.Load(new(size.X, size.Y), size.Z, buffer.AsSpan());
                }
                finally {
                    buffer.Dispose();
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

        private struct ModelData : IDisposable
        {
            public UnsafeRawArray<SkinnedVertex> Vertices;
            public UnsafeRawArray<Components.Bone> Bones;
            public RefTypeRentMemory<IImageSource?> Images;

            public ModelData(UnsafeRawArray<SkinnedVertex> vertices, UnsafeRawArray<Components.Bone> bones, RefTypeRentMemory<IImageSource?> images)
            {
                Vertices = vertices;
                Bones = bones;
                Images = images;
            }

            public void Dispose()
            {
                Vertices.Dispose();
                Bones.Dispose();
                foreach(var image in Images.AsSpan()) {
                    image?.Dispose();
                }
                Images.Dispose();
            }
        }
    }
}
