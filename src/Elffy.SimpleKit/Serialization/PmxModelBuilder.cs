#nullable enable
using System;
using System.Linq;
using System.IO;
using Elffy.Exceptions;
using Elffy.Core;
using Elffy.Shapes;
using Elffy.Effective;
using StringLiteral;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using MMDTools.Unmanaged;
using System.Drawing;
using Elffy.Imaging;
using Elffy.Effective.Unsafes;
using Elffy.Components;
using UnmanageUtility;

namespace Elffy.Serialization
{
    public static class PmxModelBuilder
    {
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

            return Model3D.Create(obj, static async (obj, model, load) =>
            {
                obj.CancellationToken.ThrowIfCancellationRequested();

                // Run on thread pool
                await UniTask.SwitchToThreadPool();
                // ------------------------------
                //      ↓ thread pool

                obj.CancellationToken.ThrowIfCancellationRequested();

                // Parse pmx file
                var pmx = PMXParser.Parse(obj.ResourceLoader.GetStream(obj.Name));
                BuildCore(pmx, model, load, obj);
                //      ↑ thread pool
                // ------------------------------
            });
        }

        private static void BuildCore(PMXObject pmx, Model3D model, Model3DLoadDelegate load, ModelState obj)
        {
            // ------------------------------
            //      ↓ thread pool
            Debug.Assert(model.HostScreen.IsThreadMain() == false);

            var textureNames = pmx.TextureList.AsSpan();
            var dir = ResourcePath.GetDirectoryName(obj.Name);
            var bitmaps = new RefTypeRentMemory<Bitmap>(textureNames.Length);
            var bitmapSpan = bitmaps.Span;

            for(int i = 0; i < bitmapSpan.Length; i++) {
                using(GetTexturePath(dir, textureNames[i], out var texturePath, out var ext))
                using(var stream = obj.ResourceLoader.GetStream(texturePath.ToString())) {
                    bitmapSpan[i] = BitmapHelper.StreamToBitmap(stream, ext);
                }
            }

            // [NOTE] Though pmx is read only, overwrite pmx data.
            PmxModelLoadHelper.ReverseTrianglePolygon(pmx!.SurfaceList.AsSpan().AsWritable());

            LoadToModel(pmx, model, load, obj, bitmaps).Forget();
            //      ↑ thread pool
            // ------------------------------

            // Don't use bitmaps here (It may be disposed.)
        }

        private static async UniTaskVoid LoadToModel(PMXObject pmx, Model3D model, Model3DLoadDelegate load, ModelState obj, RefTypeRentMemory<Bitmap> bitmaps)
        {
            // ------------------------------
            //      ↓ thread pool
            Debug.Assert(model.HostScreen.IsThreadMain() == false);

            UnmanagedArray<RigVertex>? vertices = default;
            UnmanagedArray<RenderableParts>? parts = default;
            UnmanagedArray<Components.Bone>? bones = default;
            try {
                (vertices, parts, bones) = await UniTask.WhenAll(
                    // build vertices
                    UniTask.Run(() => pmx.VertexList
                                    .AsSpan()
                                    .SelectToUnmanagedArray(v => v.ToRigVertex()),
                                configureAwait: false),
                    // build each parts
                    UniTask.Run(() => pmx.MaterialList
                                    .AsSpan()
                                    .SelectToUnmanagedArray(m => new RenderableParts(m.VertexCount, m.Texture)),
                                configureAwait: false),
                    // build bones
                    UniTask.Run(() => pmx!.BoneList
                                    .AsSpan()
                                    .SelectToUnmanagedArray(b => new Components.Bone(UnsafeEx.As<MMDTools.Unmanaged.Vector3, Vector3>(in b.Position),
                                                                                     b.ParentBone != 65535 ? b.ParentBone : null,
                                                                                     b.ConnectedBone != 65535 ? b.ConnectedBone : null)),
                                configureAwait: false));

                //      ↑ thread pool
                // ------------------------------
                await model.HostScreen.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.Update, obj.CancellationToken);
                // ------------------------------
                //      ↓ main thread
                var sw = new Stopwatch();
                sw.Start();
                Debug.Assert(model.HostScreen.IsThreadMain());
                if(model.IsActivated || model.IsAlive) {
                    // create skeleton
                    var skeleton = new Skeleton();
                    skeleton.Load(bones.AsSpan());
                    model.AddComponent(skeleton);

                    // create multi texture
                    var textures = new MultiTexture();
                    textures.Load(bitmaps.Span);
                    model.AddComponent(textures);

                    parts.Dispose();    // TODO: how do i use 'parts' ?

                    // load vertices and indices
                    load.Invoke(vertices.AsSpan().AsReadOnly(), pmx.SurfaceList.AsSpan().MarshalCast<Surface, int>());
                }
                Debug.WriteLine($"{sw.ElapsedMilliseconds} ms");

                //      ↑ main thread
                // ------------------------------
            }
            finally {
                await UniTask.SwitchToThreadPool();
                // ------------------------------
                //      ↓ thread pool
                ReleaseMemory(pmx, bitmaps, vertices);

                static void ReleaseMemory(PMXObject pmx, in RefTypeRentMemory<Bitmap> bitmaps, UnmanagedArray<RigVertex>? vertices)
                {
                    foreach(var b in bitmaps.Span) {
                        b.Dispose();
                    }
                    bitmaps.Dispose();
                    pmx.Dispose();
                    vertices?.Dispose();
                }
                //      ↑ thread pool
                // ------------------------------
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

        private sealed class ModelState
        {
            public IResourceLoader ResourceLoader { get; }
            public string Name { get; }
            public CancellationToken CancellationToken { get; }

            public ModelState(IResourceLoader resourceLoader, string name, CancellationToken token)
            {
                ResourceLoader = resourceLoader;
                Name = name;
                CancellationToken = token;
            }
        }

        private readonly struct RenderableParts
        {
            public readonly int VertexCount;
            public readonly int TextureIndex;

            public RenderableParts(int vertexCount, int textureIndex)
            {
                VertexCount = vertexCount;
                TextureIndex = textureIndex;
            }
        }
    }
}
