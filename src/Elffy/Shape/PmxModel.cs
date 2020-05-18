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
using UnmanageUtility;
using Cysharp.Text;
using PMXParser = MMDTools.PMXParser;
using PMXObject = MMDTools.PMXObject;

namespace Elffy.Shape
{
    public class PmxModel : MultiPartsRenderable
    {
        private PMXObject? _pmxObject;
        private Bitmap[]? _textureBitmaps;

        private unsafe PmxModel(PMXObject pmxObject, Bitmap[] textureBitmaps)
        {
            Debug.Assert(pmxObject != null);
            Debug.Assert(textureBitmaps != null);
            _pmxObject = pmxObject;
            _textureBitmaps = textureBitmaps;
            Activated += OnActivated;
            Terminated += OnTerminated;
        }

        private void OnTerminated(FrameObject sender)
        {
            if(TryGetComponent<IComponentInternal<MultiTexture>>(out var textures)) {
                RemoveComponent<IComponentInternal<MultiTexture>>();
                textures.Self.Dispose();
            }
        }

        private async void OnActivated(FrameObject frameObject)
        {
            // Here is main thread

            var (vertices, parts) = await Task.Factory.StartNew(() =>
            {
                var pmx = _pmxObject!;
                ReverseTrianglePolygon(pmx.SurfaceList.Span);       // オリジナルのデータを書き換えているので注意、このメソッドは1回しか通らない前提
                var vertices = pmx.VertexList.Span.SelectToUnmanagedArray(ToVertex);
                var parts = pmx.MaterialList.Span.SelectToArray(m => new RenderableParts(m.VertexCount, m.Texture));
                return (vertices, parts);
            }).ConfigureAwait(true);
            // ↑ ConfigureAwait true

            // Here is main thread
            if(!IsTerminated) {
                Debug.Assert(vertices != null);
                LoadGraphicBuffer(vertices!.AsSpan(), _pmxObject!.SurfaceList.Span.MarshalCast<MMDTools.Surface, int>());
                SetParts(parts);
                var texture = new MultiTexture();
                texture.Load(_textureBitmaps);
                AddOrReplaceComponent<IComponentInternal<MultiTexture>>(texture, out _);
            }

            // not await
            _ = Task.Factory.StartNew(v =>
            {
                // メモリ開放後始末 (非同期で問題ないので別スレッド)
                var vertices = (UnmanagedArray<Vertex>)v;
                vertices.Dispose();
                var textureBitmaps = _textureBitmaps;
                if(textureBitmaps != null) {
                    foreach(var t in textureBitmaps) {
                        t.Dispose();
                    }
                    _textureBitmaps = null;
                }
            }, vertices);
            _pmxObject = null;
        }

        public static Task<PmxModel> LoadResourceAsync(string name)
        {
            return Task.Factory.StartNew(n =>
            {
                var name = (string)n;
                PMXObject pmx;
                using(var stream = Resources.GetStream(name)) {
                    pmx = PMXParser.Parse(stream);     // PMXParser.Parse はスレッドセーフ
                }
                var textureNames = pmx.TextureList.Span;
                var dir = Resources.GetDirectoryName(name);
                var bitmaps = new Bitmap[textureNames.Length];
                for(int i = 0; i < textureNames.Length; i++) {
                    var texturePath = PathConcat(dir, textureNames[i].AsSpan());
                    using(var tStream = Resources.GetStream(texturePath)) {
                        bitmaps[i] = BitmapHelper.StreamToBitmap(tStream, textureNames[i].FilePathExtension());
                    }
                }
                return new PmxModel(pmx, bitmaps);
            }, name);
        }

        private static string PathConcat(ReadOnlySpan<char> dir, ReadOnlySpan<char> name)
        {
            const char Splitter = '/';

            // name = "hoge\\piyo.foo" ----> n = "hoge/piyo.foo"
            var n = name.Replace('\\', Splitter, stackalloc char[name.Length]);

            // $"{dir}/{n}"
            var length = dir.Length + n.Length + 1;
            var sb = ZString.CreateStringBuilder();
            var buf = sb.GetSpan(length);
            dir.CopyTo(buf);
            buf[dir.Length] = Splitter;
            n.CopyTo(buf.Slice(dir.Length + 1));
            sb.Advance(length);
            return sb.ToString();
        }

        /// <summary>三角ポリゴンの表裏を反転させます</summary>
        /// <param name="surfaceList">頂点インデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReverseTrianglePolygon(ReadOnlySpan<MMDTools.Surface> surfaceList)
        {
            // (a, b, c) を (a, c, b) に書き換える
            fixed(MMDTools.Surface* s = surfaceList) {
                int* p = (int*)s;
                for(int i = 0; i < surfaceList.Length; i++) {
                    var i1 = i * 3 + 1;
                    var i2 = i * 3 + 2;
                    (p[i1], p[i2]) = (p[i2], p[i1]);
                }
            }
        }

        //private static Material ToMaterial(MMDTools.Material m) => new Material(ToColor4(m.Ambient), ToColor4(m.Diffuse), ToColor4(m.Specular), m.Shininess);

        private static Bone ToBone(MMDTools.Bone bone) => new Bone(bone);

        private static VertexBoneInfo ToVertexBoneInfo(MMDTools.Vertex v) => new VertexBoneInfo(v);

        private static Vertex ToVertex(MMDTools.Vertex v) => new Vertex(ToVector3(v.Position), ToVector3(v.Normal), ToVector2(v.UV));

        private static Color4 ToColor4(MMDTools.Color color) => Unsafe.As<MMDTools.Color, Color4>(ref color);

        private static Vector3 ToVector3(MMDTools.Vector3 vector) => new Vector3(vector.X, vector.Y, -vector.Z);

        private static Vector2 ToVector2(MMDTools.Vector2 vector) => new Vector2(vector.X, vector.Y);

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
            public VertexBoneInfo(MMDTools.Vertex v)
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

            private static WeightTransformType ToWeightType(MMDTools.WeightTransformType t)
            {
                return t switch
                {
                    MMDTools.WeightTransformType.BDEF1 => WeightTransformType.BDEF1,
                    MMDTools.WeightTransformType.BDEF2 => WeightTransformType.BDEF2,
                    MMDTools.WeightTransformType.BDEF4 => WeightTransformType.BDEF4,
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

            public Bone(MMDTools.Bone bone)
            {
                Position = ToVector3(bone.Position);
                Parent = bone.ParentBone;
            }
        }
    }
}
