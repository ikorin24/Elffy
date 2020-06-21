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

namespace Elffy.Shape
{
    public class PmxModel : Renderable
    {
        private PMXObject? _pmxObject;
        private Bitmap[]? _textureBitmaps;

        private RenderableParts[]? _parts;
        private MultiTexture? _textures;

        private unsafe PmxModel(PMXObject pmxObject, Bitmap[] textureBitmaps)
        {
            Debug.Assert(pmxObject != null);
            Debug.Assert(textureBitmaps != null);
            _pmxObject = pmxObject;
            _textureBitmaps = textureBitmaps;
        }

        protected override void OnDead()
        {
            base.OnDead();
            _textures?.Dispose();
            _textures = null;
        }

        protected override async void OnActivated()
        {
            base.OnActivated();

            // Here is main thread

            var vertices = await Task.Factory.StartNew(() =>
            {
                var pmx = _pmxObject!;
                ReverseTrianglePolygon(pmx.SurfaceList.AsSpan());       // オリジナルのデータを書き換えているので注意、このメソッドは1回しか通らない前提
                var vertices = pmx.VertexList.AsSpan().SelectToUnmanagedArray(ToVertex);
                _parts = pmx.MaterialList.AsSpan().SelectToArray(m => new RenderableParts(m.VertexCount, m.Texture));
                return vertices;
            }).ConfigureAwait(true);
            // ↑ ConfigureAwait true

            // Here is main thread
            if(!LifeState.HasTerminatingBit() && !LifeState.HasDeadBit()) {
                LoadGraphicBuffer(vertices.AsSpan(), _pmxObject!.SurfaceList.AsSpan().MarshalCast<MMDTools.Unmanaged.Surface, int>());
                var textures = new MultiTexture();
                textures.Load(_textureBitmaps);
                _textures = textures;
            }

            // not await
            _ = Task.Factory.StartNew(v =>
            {
                // メモリ開放後始末 (非同期で問題ないので別スレッド)
                var vertices = Unsafe.As<UnmanagedArray<Vertex>>(v)!;
                vertices.Dispose();
                var textureBitmaps = _textureBitmaps;
                if(textureBitmaps != null) {
                    foreach(var t in textureBitmaps) {
                        t.Dispose();
                    }
                    _textureBitmaps = null;
                }
                _pmxObject!.Dispose();
                _pmxObject = null;
            }, vertices);
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            VAO.Bind(VAO);
            IBO.Bind(IBO);
            var parts = _parts;
            if(parts != null) {
                var pos = 0;
                var textures = _textures;
                foreach(var p in parts) {
                    textures?.Apply(p.TextureIndex);
                    ShaderProgram!.Apply(this, InternalLayer!.Lights, in model, in view, in projection);
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
                var bitmaps = new Bitmap[textureNames.Length];
                for(int i = 0; i < textureNames.Length; i++) {

                    using(var pooledArray = new PooledArray<char>(dir.Length + 1 + textureNames[i].GetCharCount())) {
                        var texturePath = pooledArray.AsSpan();
                        dir.CopyTo(texturePath);
                        texturePath[dir.Length] = '/';
                        textureNames[i].ToString(texturePath.Slice(dir.Length + 1));
                        texturePath.Replace('\\', '/');
                        var textureExt = texturePath.AsReadOnly().FilePathExtension();

                        using(var tStream = Resources.GetStream(texturePath.ToString())) {
                            bitmaps[i] = BitmapHelper.StreamToBitmap(tStream, textureExt);
                        }
                    }
                }
                return new PmxModel(pmx, bitmaps);
            }, name);
        }

        /// <summary>三角ポリゴンの表裏を反転させます</summary>
        /// <param name="surfaceList">頂点インデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReverseTrianglePolygon(ReadOnlySpan<MMDTools.Unmanaged.Surface> surfaceList)
        {
            // (a, b, c) を (a, c, b) に書き換える
            fixed(MMDTools.Unmanaged.Surface* s = surfaceList) {
                int* p = (int*)s;
                for(int i = 0; i < surfaceList.Length; i++) {
                    var i1 = i * 3 + 1;
                    var i2 = i * 3 + 2;
                    (p[i1], p[i2]) = (p[i2], p[i1]);
                }
            }
        }

        //private static Material ToMaterial(MMDTools.Material m) => new Material(ToColor4(m.Ambient), ToColor4(m.Diffuse), ToColor4(m.Specular), m.Shininess);

        private static Bone ToBone(MMDTools.Unmanaged.Bone bone) => new Bone(bone);

        private static VertexBoneInfo ToVertexBoneInfo(MMDTools.Unmanaged.Vertex v) => new VertexBoneInfo(v);

        private static Vertex ToVertex(MMDTools.Unmanaged.Vertex v) => new Vertex(ToVector3(v.Position), ToVector3(v.Normal), ToVector2(v.UV));

        private static Color4 ToColor4(MMDTools.Unmanaged.Color color) => Unsafe.As<MMDTools.Unmanaged.Color, Color4>(ref color);

        private static Vector3 ToVector3(MMDTools.Unmanaged.Vector3 vector) => new Vector3(vector.X, vector.Y, -vector.Z);

        private static Vector2 ToVector2(MMDTools.Unmanaged.Vector2 vector) => new Vector2(vector.X, vector.Y);

        private readonly struct VertexBoneInfo : IEquatable<VertexBoneInfo>
        {
            public readonly int Bone1;
            public readonly int Bone2;
            public readonly int Bone3;
            public readonly int Bone4;
            public readonly float Weight1;
            public readonly float Weight2;
            public readonly float Weight3;
            public readonly float Weight4;
            public readonly WeightTransformType Type;
            public VertexBoneInfo(MMDTools.Unmanaged.Vertex v)
            {
                Bone1 = v.BoneIndex1;
                Bone2 = v.BoneIndex2;
                Bone3 = v.BoneIndex3;
                Bone4 = v.BoneIndex4;
                Weight1 = v.Weight1;
                Weight2 = v.Weight2;
                Weight3 = v.Weight3;
                Weight4 = v.Weight4;
                Type = ToWeightType(v.WeightTransformType);
            }

            private static WeightTransformType ToWeightType(MMDTools.Unmanaged.WeightTransformType t)
            {
                return t switch
                {
                    MMDTools.Unmanaged.WeightTransformType.BDEF1 => WeightTransformType.BDEF1,
                    MMDTools.Unmanaged.WeightTransformType.BDEF2 => WeightTransformType.BDEF2,
                    MMDTools.Unmanaged.WeightTransformType.BDEF4 => WeightTransformType.BDEF4,
                    _ => throw new NotSupportedException($"Not supported weight type. Type : {t}"),
                };
            }

            public override bool Equals(object? obj) => obj is VertexBoneInfo info && Equals(info);

            public bool Equals(VertexBoneInfo other) => (Bone1 == other.Bone1) && (Bone2 == other.Bone2) && (Bone3 == other.Bone3) && (Bone4 == other.Bone4) &&
                                                        (Weight1 == other.Weight1) && (Weight2 == other.Weight2) && (Weight3 == other.Weight3) && (Weight4 == other.Weight4) &&
                                                        (Type == other.Type);

            public override int GetHashCode() => HashCodeEx.Combine(Bone1, Bone2, Bone3, Bone4, Weight1, Weight2, Weight3, Weight4, Type);

            public static bool operator ==(VertexBoneInfo left, VertexBoneInfo right) => left.Equals(right);

            public static bool operator !=(VertexBoneInfo left, VertexBoneInfo right) => !(left == right);
        }

        private readonly struct Bone
        {
            public readonly Vector3 Position;
            public readonly int Parent;

            public Bone(MMDTools.Unmanaged.Bone bone)
            {
                Position = ToVector3(bone.Position);
                Parent = bone.ParentBone;
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
