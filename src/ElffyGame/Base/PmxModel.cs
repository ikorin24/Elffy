#nullable enable
using System;
using Elffy;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Imaging;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using UnmanageUtility;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Cysharp.Text;
using PMXParser = MMDTools.PMXParser;
using PMXObject = MMDTools.PMXObject;

namespace ElffyGame.Base
{
    public class PmxModel : Renderable
    {
        private Material[]? _materials;
        private int[]? _partVertexCount;

        private int[]? _textureBufs;

        private PMXObject? _pmxObject;
        private Bitmap[]? _textureBitmaps;
        private int[]? _textureIndex;

        private unsafe PmxModel(PMXObject pmxObject, Bitmap[] textureBitmaps)
        {
            Debug.Assert(pmxObject != null);
            Debug.Assert(textureBitmaps != null);
            _pmxObject = pmxObject;
            _textureBitmaps = textureBitmaps;
            Activated += OnActivated;
            Terminated += OnTerminated;
            Rendered += OnRendered;
        }

        private unsafe void CreateTextures(ReadOnlySpan<Bitmap> images)
        {
            var textures = new int[images.Length];
            for(int i = 0; i < textures.Length; i++) {
                textures[i] = GL.GenTexture();
                using(var pixels = images[i].GetPixels(ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                    GL.BindTexture(TextureTarget.Texture2D, textures[i]);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixels.Width, pixels.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pixels.Ptr);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
            }
            _textureBufs = textures;
        }


        private unsafe void OnRendered(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
            var partVertexCount = _partVertexCount!;
            var textureIndex = _textureIndex!;
            var textureBufs = _textureBufs!;
            var pos = 0;


            //GL.ActiveTexture(TextureUnit.Texture0);       // これが uniform にわたされる sampler になる。この場合 0を渡す。実際に得られるテクスチャは Bind されたテクスチャ

            for(int i = 0; i < partVertexCount.Length; i++) {
                GL.BindTexture(TextureTarget.Texture2D, textureBufs[textureIndex[i]]);
                GL.DrawElements(BeginMode.Triangles, partVertexCount[i], DrawElementsType.UnsignedInt, pos * sizeof(int));
                pos += partVertexCount[i];
            }
        }

        private async void OnActivated(FrameObject frameObject)
        {
            // Hete is main thread
            IsEnableRendering = false;

            var vertices = await Task.Factory.StartNew(LoadData).ConfigureAwait(true);
            // ↑ ConfigureAwait true

            // Hete is main thread
            if(!IsTerminated) {
                // vertices が null になるのはリソースが解放済みの時、つまり既に IsTerminated == true の時のはずなので、ここは not null
                Debug.Assert(vertices != null);
                LoadGraphicBuffer(vertices!.AsSpan(), _pmxObject!.SurfaceList.Span.MarshalCast<MMDTools.Surface, int>());
            }

            var textureBitmaps = _textureBitmaps!;
            CreateTextures(textureBitmaps);
            foreach(var t in textureBitmaps) {
                t.Dispose();
            }

            // not await
            _ = Task.Factory.StartNew(v => ((UnmanagedArray<Vertex>)v)?.Dispose(), vertices);
            //_pmxObject = null;
        }

        private void OnTerminated(FrameObject frameObject)
        {
            if(_textureBufs != null) {
                GL.DeleteTextures(_textureBufs.Length, _textureBufs);
                _textureBufs = null;
            }
            //Task.Factory.StartNew(ReleaseResource);
        }

        private UnmanagedArray<Vertex>? LoadData()
        {
            var pmxObject = _pmxObject!;

            _materials = pmxObject.MaterialList.Span.SelectToArray(ToMaterial);
            _partVertexCount = pmxObject.MaterialList.Span.SelectToArray(m => m.VertexCount);
            _textureIndex = pmxObject.MaterialList.Span.SelectToArray(m => m.Texture);

            // オリジナルのデータを書き換えているので注意
            // このメソッドは1回しか通らない前提
            ReverseTrianglePolygon(pmxObject.SurfaceList.Span);

            return pmxObject.VertexList.Span.SelectToUnmanagedArray(ToVertex);
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
                    bitmaps[i].Save(textureNames[i] + ".png");
                }
                return new PmxModel(pmx, bitmaps);
            }, name);
        }

        private static string PathConcat(ReadOnlySpan<char> dir, ReadOnlySpan<char> name)
        {
            // This method means  $"{dir}/{name}"
            var length = dir.Length + name.Length + 1;

            var sb = ZString.CreateStringBuilder();
            var buf = sb.GetSpan(length);
            dir.CopyTo(buf);
            buf[dir.Length] = '/';
            name.CopyTo(buf.Slice(dir.Length + 1));
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

        private static Material ToMaterial(MMDTools.Material m) => new Material(ToColor4(m.Ambient), ToColor4(m.Diffuse), ToColor4(m.Specular), m.Shininess);

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
