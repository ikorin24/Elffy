#nullable enable
using System;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
using Elffy.Shapes;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Effective.Unsafes;
using Elffy.Components;
using Elffy.OpenGL;
using Cysharp.Threading.Tasks;
using UnmanageUtility;
using MMDTools.Unmanaged;
using PmxVector3 = MMDTools.Unmanaged.Vector3;

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

            return Model3D.Create(obj,
                builder: static async (obj, model, load) =>
            {
                obj.CancellationToken.ThrowIfCancellationRequested();

                // Run on thread pool
                await UniTask.SwitchToThreadPool();
                // ------------------------------
                //      ↓ thread pool

                obj.CancellationToken.ThrowIfCancellationRequested();

                // Parse pmx file
                var pmx = PMXParser.Parse(obj.ResourceLoader.GetStream(obj.Name));
                await BuildCore(pmx, model, load, obj);
            },
                onRendering: static (Model3D model3D, in Matrix4 model, in Matrix4 view, in Matrix4 projection, Model3DDrawElementsDelegate drawElements) =>
            {
                var info = model3D.GetComponent<PmxModelParts>();
                var vertexCountArray = info.VertexCountArray;
                var textureIndexArray = info.TextureIndexArray;
                var textures = info.Textures;

                VAO.Bind(model3D.VAO);
                IBO.Bind(model3D.IBO);
                for(int i = 0; i < vertexCountArray.Length; i++) {
                    TextureObject.Bind2D(textures[textureIndexArray[i]]);
                    model3D.ShaderProgram!.Apply(model3D, in model, in view, in projection);
                }
                VAO.Unbind();
                IBO.Unbind();
            });
        }

        private static UniTask BuildCore(PMXObject pmx, Model3D model, Model3DLoadDelegate load, ModelState obj)
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

            return LoadToModel(pmx, model, load, obj, bitmaps);
        }

        private static async UniTask LoadToModel(PMXObject pmx, Model3D model, Model3DLoadDelegate load, ModelState obj, RefTypeRentMemory<Bitmap> bitmaps)
        {
            // ------------------------------
            //      ↓ thread pool
            Debug.Assert(model.HostScreen.IsThreadMain() == false);

            UnmanagedArray<RigVertex>? vertices = default;
            ValueTypeRentMemory<int> vertexCountArray = default;
            ValueTypeRentMemory<int> textureIndexArray = default;
            ValueTypeRentMemory<TextureObject> textures = default;
            UnmanagedArray<Components.Bone>? bones = default;
            try {
                (vertices, (vertexCountArray, textureIndexArray), bones) = await UniTask.WhenAll(
                    // build vertices
                    UniTask.Run(pmx =>
                    {
                        Debug.Assert(pmx is PMXObject);
                        return Unsafe.As<PMXObject>(pmx).VertexList.AsSpan().SelectToUnmanagedArray(v => v.ToRigVertex());
                    }, pmx, configureAwait: false),
                    
                    // build each parts
                    UniTask.Run(pmx =>
                    {
                        Debug.Assert(pmx is PMXObject);
                        var materials = Unsafe.As<PMXObject>(pmx).MaterialList.AsSpan();
                        var vertexCountArray = new ValueTypeRentMemory<int>(materials.Length);
                        var textureIndexArray = new ValueTypeRentMemory<int>(materials.Length);
                        var vSpan = vertexCountArray.Span;
                        var tSpan = textureIndexArray.Span;
                        for(int i = 0; i < materials.Length; i++) {
                            vSpan[i] = materials[i].VertexCount;
                            tSpan[i] = materials[i].Texture;
                        }
                        return (vertexCountArray, textureIndexArray);
                    }, pmx, configureAwait: false),

                    // build bones
                    UniTask.Run(pmx =>
                    {
                        Debug.Assert(pmx is PMXObject);
                        return Unsafe.As<PMXObject>(pmx)
                                     .BoneList
                                     .AsSpan()
                                     .SelectToUnmanagedArray(b => new Components.Bone(UnsafeEx.As<PmxVector3, Vector3>(in b.Position),
                                                                                      b.ParentBone != 65535 ? b.ParentBone : null,
                                                                                      b.ConnectedBone != 65535 ? b.ConnectedBone : null));
                    }, pmx, configureAwait: false));
                //      ↑ thread pool
                // ------------------------------
                await model.HostScreen.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.Update, obj.CancellationToken);
                // ------------------------------
                //      ↓ main thread
                Debug.Assert(model.HostScreen.IsThreadMain());
                if(model.IsActivated || model.IsAlive) {
                    // create skeleton
                    var skeleton = new Skeleton();
                    skeleton.Load(bones.AsSpan());
                    model.AddComponent(skeleton);

                    // create parts
                    textures = new ValueTypeRentMemory<TextureObject>(bitmaps.Length);
                    textures.Span.Clear();  // must be cleared
                    for(int i = 0; i < textures.Length; i++) {
                        var t = TextureObject.Create();
                        TextureObject.Bind2D(t);
                        TextureObject.Parameter2DMinFilter(TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear);
                        TextureObject.Parameter2DMagFilter(TextureExpansionMode.Bilinear);
                        TextureObject.Image2D(bitmaps[i]);
                        TextureObject.GenerateMipmap2D();
                        textures[i] = t;
                    }
                    var partsComponent = new PmxModelParts(ref vertexCountArray, ref textureIndexArray, ref textures);
                    model.AddComponent(partsComponent);

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
                for(int i = 0; i < bitmaps.Length; i++) {
                    bitmaps[i].Dispose();
                }
                bitmaps.Dispose();
                pmx.Dispose();
                vertices?.Dispose();
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
    }
}
