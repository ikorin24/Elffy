#nullable enable
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Components;
using Elffy.OpenGL;
using Elffy.Serialization;
using Elffy.Effective.Unsafes;
using Elffy.Threading;
using Cysharp.Threading.Tasks;
using UnmanageUtility;
using MMDTools.Unmanaged;

namespace Elffy.Shapes
{
    public class PmxModel : Renderable
    {
        private PMXObject? _pmxObject;
        private RefTypeRentMemory<Bitmap> _textureBitmaps;

        private UnmanagedArray<RenderableParts>? _parts;
        private MultiTexture? _textures;

        private PmxModel(PMXObject pmxObject, in RefTypeRentMemory<Bitmap> textureBitmaps)
        {
            Debug.Assert(pmxObject != null);
            _pmxObject = pmxObject;
            _textureBitmaps = textureBitmaps;
        }

        protected override void OnDead()
        {
            base.OnDead();
            _textures = null;
            _parts?.Dispose();
            _parts = null;
        }

        protected override async void OnActivated()
        {
            base.OnActivated();

            Debug.Assert(Dispatcher.IsMainThread());

            await UniTask.SwitchToThreadPool();

            // オリジナルのデータを書き換えているので注意、このメソッドは1回しか通らない前提
            PmxModelLoadHelper.ReverseTrianglePolygon(_pmxObject!.SurfaceList.AsSpan().AsWritable());
            var (vertices, parts, bonePositions) = await UniTask.WhenAll(
                
                // build vertices
                UniTask.Run(() => _pmxObject!.VertexList
                                .AsSpan()
                                .SelectToUnmanagedArray(v => v.ToRigVertex())),
                
                // build each parts
                UniTask.Run(() => _pmxObject!.MaterialList
                                .AsSpan()
                                .SelectToUnmanagedArray(m => new RenderableParts(m.VertexCount, m.Texture))),
                // build bones
                UniTask.Run(() => _pmxObject!.BoneList
                                .AsSpan()
                                .SelectToUnmanagedArray(b => new Vector4(b.Position.ToVector3(), 0f)))
                );
            await AsyncHelper.SwitchToMain();

            Debug.Assert(Dispatcher.IsMainThread());
            _parts = parts;
            var needLoading = LifeState != FrameObjectLifeSpanState.Terminated &&
                              LifeState != FrameObjectLifeSpanState.Dead;
            if(needLoading) {
                var skeleton = new Skeleton();
                skeleton.Load(bonePositions);
                AddComponent(skeleton);

                // multi texture
                var textures = new MultiTexture();
                textures.Load(_textureBitmaps.Span);
                AddComponent(textures);
                _textures = textures;

                // load vertex
                LoadGraphicBuffer(vertices.AsSpan(), _pmxObject!.SurfaceList.AsSpan().MarshalCast<Surface, int>());
            }
            else {
                bonePositions.Dispose();
            }

            // メモリ開放後始末 (非同期で問題ないので別スレッド)
            UniTask.WhenAll(
                UniTask.Run(v =>
                {
                    var vertices = SafeCast.As<UnmanagedArray<RigVertex>>(v)!;
                    vertices.Dispose();
                }, vertices, false),
                UniTask.Run(() =>
                {
                    foreach(var t in _textureBitmaps.Span) {
                        t.Dispose();
                    }
                    _textureBitmaps.Dispose();
                    _textureBitmaps = default;
                }),
                UniTask.Run(() =>
                {
                    _pmxObject?.Dispose();
                    _pmxObject = null;
                })
            ).Forget();
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            VAO.Bind(VAO);
            IBO.Bind(IBO);
            var parts = _parts;
            if(parts != null) {
                var pos = 0;
                var textures = _textures;
                Debug.Assert(textures is null == false);
                for(int i = 0; i < parts.Length; i++) {
                    textures.Current = parts[i].TextureIndex;
                    ShaderProgram!.Apply(this, Layer.Lights, in model, in view, in projection);
                    DrawElements(parts[i].VertexCount, pos * sizeof(int));
                    pos += parts[i].VertexCount;
                }
            }
            VAO.Unbind();
            IBO.Unbind();
        }

        public static PmxModel LoadResource(string name)
        {
            PMXObject pmx;
            using(var stream = Resources.GetStream(name)) {
                pmx = PMXParser.Parse(stream);
            }
            var textureNames = pmx.TextureList.AsSpan();
            var dir = Resources.GetDirectoryName(name);
            var bitmaps = new RefTypeRentMemory<Bitmap>(textureNames.Length);
            var bitmapSpan = bitmaps.Span;

            for(int i = 0; i < bitmapSpan.Length; i++) {

                using var pooledArray = new PooledArray<char>(dir.Length + 1 + textureNames[i].GetCharCount());

                var texturePath = pooledArray.AsSpan();
                dir.CopyTo(texturePath);
                texturePath[dir.Length] = '/';
                textureNames[i].ToString(texturePath.Slice(dir.Length + 1));
                texturePath.Replace('\\', '/');
                var textureExt = texturePath.AsReadOnly().FilePathExtension();

                using var tStream = Resources.GetStream(texturePath.ToString());
                bitmapSpan[i] = BitmapHelper.StreamToBitmap(tStream, textureExt);
            }
            return new PmxModel(pmx, bitmaps);
        }

        public static UniTask<PmxModel> LoadResourceAsync(string name)
        {
            return UniTask.Run(n => LoadResource(SafeCast.As<string>(n)),
                               name,
                               configureAwait: false);
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
