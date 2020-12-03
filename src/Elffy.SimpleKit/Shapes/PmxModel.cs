#nullable enable
using System.Diagnostics;
using System.Drawing;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Components;
using Elffy.OpenGL;
using Elffy.Serialization;
using Elffy.Effective.Unsafes;
using Cysharp.Threading.Tasks;
using UnmanageUtility;
using MMDTools.Unmanaged;
using System.Runtime.CompilerServices;

namespace Elffy.Shapes
{
    public class PmxModel : Renderable
    {
        private PMXObject? _pmxObject;
        private RefTypeRentMemory<Bitmap> _textureBitmaps;

        private UnmanagedArray<RenderableParts>? _parts;
        private MultiTexture? _textures;

        internal PmxModel(PMXObject pmxObject, in RefTypeRentMemory<Bitmap> textureBitmaps)
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

            Debug.Assert(HostScreen.IsThreadMain());

            await UniTask.SwitchToThreadPool();

            // オリジナルのデータを書き換えているので注意、このメソッドは1回しか通らない前提
            PmxModelLoadHelper.ReverseTrianglePolygon(_pmxObject!.SurfaceList.AsSpan().AsWritable());
            var (vertices, parts, bonePositions) = await UniTask.WhenAll(

                // build vertices
                UniTask.Run(() => _pmxObject!.VertexList
                                .AsSpan()
                                .SelectToUnmanagedArray(v => v.ToRigVertex()), configureAwait: false),

                // build each parts
                UniTask.Run(() => _pmxObject!.MaterialList
                                .AsSpan()
                                .SelectToUnmanagedArray(m => new RenderableParts(m.VertexCount, m.Texture)), configureAwait: false),
                // build bones
                //UniTask.Run(() => _pmxObject!.BoneList
                //                .AsSpan()
                //                .SelectToUnmanagedArray(b => new Vector4(b.Position.ToVector3(), 0f)), configureAwait: false)
                UniTask.Run(() => _pmxObject!.BoneList
                                .AsSpan()
                                .SelectToUnmanagedArray(b => new Components.Bone(UnsafeEx.As<MMDTools.Unmanaged.Vector3, Vector3>(in b.Position),
                                                                                 b.ParentBone != 65535 ? b.ParentBone : null,
                                                                                 b.ConnectedBone != 65535 ? b.ConnectedBone : null)))
                );
            await HostScreen.AsyncBack.ToFrameLoopEvent(Threading.Tasks.FrameLoopTiming.Update);

            Debug.Assert(HostScreen.IsThreadMain());

            _parts = parts;
            var needLoading = LifeState != FrameObjectLifeState.Terminated &&
                              LifeState != FrameObjectLifeState.Dead;
            if(needLoading) {
                var skeleton = new Skeleton();
                skeleton.Load(bonePositions.AsSpan());
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
                    Debug.Assert(v is UnmanagedArray<RigVertex>);
                    var vertices = Unsafe.As<UnmanagedArray<RigVertex>>(v);
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
                    ShaderProgram!.Apply(this, in model, in view, in projection);
                    DrawElements(parts[i].VertexCount, pos * sizeof(int));
                    pos += parts[i].VertexCount;
                }
            }
            VAO.Unbind();
            IBO.Unbind();
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
