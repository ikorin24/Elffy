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

using System.Linq;

namespace Elffy.Serialization
{
    public static class PmxModelBuilder
    {
        private sealed record ModelState(ResourceFile File, CancellationToken CancellationToken);

        private static readonly Model3DBuilderDelegate<ModelState> _build = Build;

        public static Model3D CreateLazyLoadingPmx(ResourceFile file, CancellationToken cancellationToken = default)
        {
            ResourceFile.ThrowArgumentExceptionIfNone(file);
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
            var screen = model.GetValidScreen();

            // Don't make the followings parallel like UniTask.WhenAll.
            // ModelData will be disposed when an exception is throw, but another parallel code could not know that.
            using var vertices = GetVertices(pmx);
            using var bones = GetBones(pmx);
            await BuildTexture(pmx, obj.File, model, vertices, obj.CancellationToken);
            await UniTask.SwitchToThreadPool();

            var skeleton = new HumanoidSkeleton();
            model.AddComponent(skeleton);
            await skeleton.LoadAsync(bones.AsSpanLike(), screen.Timings, obj.CancellationToken);

            //      ↑ thread pool
            // ------------------------------
            await screen.Timings.Update.NextOrNow(obj.CancellationToken);
            // ------------------------------
            //      ↓ main thread
            Debug.Assert(Engine.CurrentContext == screen);
            Debug.Assert(model.LifeState == LifeState.Activating);

            model.Shader ??= new PmxModelShader();

            // load vertices and indices
            load.Invoke(vertices.AsSpan().AsReadOnly(), pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>());

            //      ↑ main thread
            // ------------------------------
        }

        private static UnsafeRawArray<SkinnedVertex> GetVertices(PMXObject pmx)
        {
            // build vertices
            var pmxVertices = pmx.VertexList.AsSpan();
            var vertices = new UnsafeRawArray<SkinnedVertex>(pmxVertices.Length, false);
            try {
                for(int i = 0; i < pmxVertices.Length; i++) {
                    vertices[i] = pmxVertices[i].ToRigVertex();
                }
            }
            catch {
                vertices.Dispose();
                throw;
            }
            return vertices;
        }

        private static UnsafeRawArray<Components.Bone> GetBones(PMXObject pmx)
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
            return bones;
        }

        private static UniTask BuildTexture(PMXObject pmx, ResourceFile pmxFile, Model3D model, UnsafeRawArray<SkinnedVertex> vertices, CancellationToken ct)
        {
            var arrayTexture = new ArrayTexture(TextureConfig.BilinearRepeat);
            model.AddComponent(arrayTexture);

            var dir = ResourcePath.GetDirectoryName(pmxFile.Name);
            var textureNames = pmx.TextureList.AsSpan();
            var resourcePackage = pmxFile.Package;

            var materials = pmx.MaterialList.AsSpan();
            using var matTexMem = new ValueTypeRentMemory<int>(materials.Length, true);
            var matTex = matTexMem.AsSpan();
            for(int i = 0; i < materials.Length; i++) {
                matTex[i] = materials[i].Texture;
            }
            matTex.Sort();
            var texCount = 0;
            if(matTex.Length > 0) {
                texCount = 1;
                for(int i = 1; i < matTex.Length; i++) {
                    if(matTex[i - 1] != matTex[i]) {
                        texCount++;
                    }
                }
            }

            using var textureUsed = new ValueTypeRentMemory<bool>(textureNames.Length, true);

            var size = new Vector2i(1024, 1024);    // TODO:
            var count = 0;
            var buffer = new ValueTypeRentMemory<ColorByte>(size.X * size.Y * texCount, false);
            try {
                for(int i = 0; i < textureNames.Length; i++) {
                    if(matTex.Contains(i) == false) { continue; }

                    using var _ = GetTexturePath(dir, textureNames[i], out var texturePath, out var ext);
                    var path = texturePath.ToString();

                    // Some pmx have the texture paths that don't exist. (Nobody references them.)
                    // So skip them.
                    if(resourcePackage.TryGetStream(path, out var stream) == false) { continue; }
                    textureUsed[i] = true;
                    var dest = buffer.AsSpan(count * size.X * size.Y);
                    try {
                        using var image = Image.LoadToImageSource(stream, Image.GetTypeFromExt(ext));
                        if(image.Width != size.X || image.Height != size.Y) {
                            using var resized = Image.Resized(image, size);
                            resized.GetPixels().CopyTo(dest);
                        }
                        else {
                            image.GetPixels().CopyTo(dest);
                        }
                        count++;
                    }
                    finally {
                        stream.Dispose();
                    }
                }

                // Calculate texture index of each vertex
                {
                    var indices = pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>();
                    int i = 0;
                    foreach(var mat in materials) {
                        for(int j = 0; j < mat.VertexCount; j++) {
                            var unusedCount = 0;
                            foreach(var isUsed in textureUsed.AsSpan(0, mat.Texture)) {
                                if(isUsed == false) {
                                    unusedCount++;
                                }
                            }
                            vertices[indices[i++]].TextureIndex = mat.Texture - unusedCount;
                        }
                    }
                }

            }
            catch {
                buffer.Dispose();
                throw;
            }
            return BackToMainThread(arrayTexture, new Vector3i(size.X, size.Y, count), buffer, model.Screen!.Timings.Update, ct);

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
    }
}
