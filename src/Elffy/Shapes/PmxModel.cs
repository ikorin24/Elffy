#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Components;
using Elffy.OpenGL;
using OpenToolkit.Graphics.OpenGL;
using UnmanageUtility;
using PMXParser = MMDTools.Unmanaged.PMXParser;
using PMXObject = MMDTools.Unmanaged.PMXObject;
using Elffy.Serialization;
using System.Runtime.InteropServices;
using System.Linq;
using Elffy.Effective.Unsafes;

namespace Elffy.Shapes
{
    public class PmxModel : Renderable
    {
        private PMXObject? _pmxObject;
        private RefTypeRentMemory<Bitmap> _textureBitmaps;

        private UnmanagedArray<RenderableParts>? _parts;
        private MultiTexture? _textures;

        private unsafe PmxModel(PMXObject pmxObject, in RefTypeRentMemory<Bitmap> textureBitmaps)
        {
            Debug.Assert(pmxObject != null);
            _pmxObject = pmxObject;
            _textureBitmaps = textureBitmaps;
        }

        protected override void OnDead()
        {
            base.OnDead();
            _textures?.Dispose();
            _textures = null;
            _parts?.Dispose();
            _parts = null;
        }

        protected override async void OnActivated()
        {
            base.OnActivated();

            (UnmanagedArray<RigVertex>, UnmanagedArray<RenderableParts>) BuildModelParts()
            {
                var pmx = _pmxObject!;

                // オリジナルのデータを書き換えているので注意、このメソッドは1回しか通らない前提
                PmxModelLoadHelper.ReverseTrianglePolygon(pmx.SurfaceList.AsSpan().AsWritable());

                var vertices = pmx.VertexList.AsSpan().SelectToUnmanagedArray(v => v.ToRigVertex());
                var parts = pmx.MaterialList.AsSpan().SelectToUnmanagedArray(m => new RenderableParts(m.VertexCount, m.Texture));
                return (vertices, parts);
            }

            UnmanagedArray<Vector4> BuildBonePositions()
            {
                var bones = _pmxObject!.BoneList.AsSpan();
                return bones.SelectToUnmanagedArray(b => new Vector4(b.Position.X, b.Position.Y, b.Position.Z, 0));
            }


            // Here is main thread

            var (vertices, parts) = await Task.Factory.StartNew(BuildModelParts);
            var bonePositions = await Task.Factory.StartNew(BuildBonePositions);
            _parts = parts;

            // Here is main thread
            if(LifeState != FrameObjectLifeSpanState.Terminated &&
               LifeState != FrameObjectLifeSpanState.Dead) {
                // skeleton component
                //var skeleton = new Skeleton(TextureUnitNumber.Unit1);
                //skeleton.Load(bonePositions.AsSpan());
                var skeleton = new Skeleton();
                skeleton.Load(bonePositions.AsSpan());
                AddComponent(skeleton);

                // multi texture
                var textures = new MultiTexture();
                textures.Load(_textureBitmaps.Span);
                _textures = textures;

                // load vertex
                LoadGraphicBuffer(vertices.AsSpan(), _pmxObject!.SurfaceList.AsSpan().MarshalCast<MMDTools.Unmanaged.Surface, int>());
            }

            // not await
            _ = ReleaseTemporaryBufferAsync(vertices, bonePositions);
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            VAO.Bind(VAO);
            IBO.Bind(IBO);
            var parts = _parts;
            if(parts != null) {
                var pos = 0;
                var textures = _textures;
                foreach(var p in parts.AsSpan()) {
                    textures?.Apply(p.TextureIndex, TextureUnitNumber.Unit0);
                    ShaderProgram!.Apply(this, Layer.Lights, in model, in view, in projection);
                    GL.DrawElements(BeginMode.Triangles, p.VertexCount, DrawElementsType.UnsignedInt, pos * sizeof(int));
                    pos += p.VertexCount;
                }
            }
        }

        public static Task<PmxModel> LoadResourceAsync(string name)
        {
            return Task.Factory.StartNew(n =>
            {
                var name = Unsafe.As<string>(n)!;
                PMXObject pmx;
                using(var stream = Resources.GetStream(name)) {
                    pmx = PMXParser.Parse(stream);
                }
                var textureNames = pmx.TextureList.AsSpan();
                var dir = Resources.GetDirectoryName(name);
                var bitmaps = new RefTypeRentMemory<Bitmap>(textureNames.Length);
                var bitmapSpan = bitmaps.Span;
                for(int i = 0; i < bitmapSpan.Length; i++) {

                    using(var pooledArray = new PooledArray<char>(dir.Length + 1 + textureNames[i].GetCharCount())) {
                        var texturePath = pooledArray.AsSpan();
                        dir.CopyTo(texturePath);
                        texturePath[dir.Length] = '/';
                        textureNames[i].ToString(texturePath.Slice(dir.Length + 1));
                        texturePath.Replace('\\', '/');
                        var textureExt = texturePath.AsReadOnly().FilePathExtension();

                        using(var tStream = Resources.GetStream(texturePath.ToString())) {
                            bitmapSpan[i] = BitmapHelper.StreamToBitmap(tStream, textureExt);
                        }
                    }
                }
                return new PmxModel(pmx, bitmaps);
            }, name);
        }

        /// <summary>ロードのための一時バッファを非同期で解放します</summary>
        /// <param name="vertices"></param>
        /// <param name="bonePositions"></param>
        /// <returns></returns>
        private Task ReleaseTemporaryBufferAsync(UnmanagedArray<RigVertex> vertices, UnmanagedArray<Vector4> bonePositions)
        {
            // メモリ開放後始末 (非同期で問題ないので別スレッド)
            return Task.Factory.StartNew(v =>
            {
                Debug.Assert(v is UnmanagedArray<RigVertex>);
                var vertices = Unsafe.As<UnmanagedArray<RigVertex>>(v)!;
                vertices.Dispose();

                foreach(var t in _textureBitmaps.Span) {
                    t.Dispose();
                }
                _textureBitmaps.Dispose();
                _textureBitmaps = default;

                _pmxObject?.Dispose();
                _pmxObject = null;
            }, vertices).ContinueWith((task, state) =>
            {
                Debug.Assert(state is UnmanagedArray<Vector4>);
                var bonePosition = Unsafe.As<UnmanagedArray<Vector4>>(state);
                bonePosition.Dispose();
            }, bonePositions);
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
