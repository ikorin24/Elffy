#nullable enable
using System;
using Elffy;
using Elffy.Core;
using Elffy.Effective;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using UnmanageUtility;

namespace ElffyGame.Base
{
    public class PmxModel : Renderable
    {
        private UnmanagedArray<VertexBoneInfo>? _vertexBoneInfo;
        private UnmanagedArray<Bone>? _bones;

        private UnmanagedArray<Material>? _materials;
        private UnmanagedArray<Range>? _indexRanges;
        private UnmanagedArray<int>? _ibo;

        private MMDTools.PMXObject? _pmxObject;

        private unsafe PmxModel(MMDTools.PMXObject pmxObject)
        {
            _pmxObject = pmxObject ?? throw new ArgumentNullException(nameof(pmxObject));

            Activated += OnActivated;
            Terminated += OnTerminated;
            Rendered += OnRendered;
        }

        private unsafe void OnRendered(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            GL.BindVertexArray(VAO);
            var ibo = _ibo!.AsSpan();
            var range = _indexRanges!.AsSpan();
            for(int i = 0; i < ibo.Length; i++) {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo[i]);
                GL.DrawElements(BeginMode.Triangles, range[i].Length * sizeof(int), DrawElementsType.UnsignedInt, 0);
            }
        }

        private unsafe void OnActivated(FrameObject frameObject)
        {
            var pmxObject = _pmxObject!;
            _pmxObject = null;

            using(var verticesPooled = pmxObject.VertexList.Span.SelectToPooledArray(ToVertex)) {   // pooled
                var vertices = verticesPooled.AsSpan();
                fixed(Vertex* p = vertices) {
                    LoadGraphicBuffer((IntPtr)p, vertices.Length, IntPtr.Zero, 0);
                }
            }

            _vertexBoneInfo = pmxObject.VertexList.Span.SelectToUnmanagedArray(ToVertexBoneInfo);   // alloc um
            _bones = pmxObject.BoneList.Span.SelectToUnmanagedArray(ToBone);                        // alloc um
            _materials = pmxObject.MaterialList.Span.SelectToUnmanagedArray(ToMaterial);            // alloc um

            var indices = pmxObject.SurfaceList.Span.MarshalCast<MMDTools.Surface, int>();
            fixed(int* p = indices) {
                ReverseTrianglePolygon(new Span<int>(p, indices.Length));
            }

            var materialsPmx = pmxObject.MaterialList.Span;
            int s = 0;
            using var indexRangesPooled = new PooledArray<Range>(pmxObject.MaterialList.Length);    // pooled
            var indexRanges = indexRangesPooled.AsSpan();
            for(int i = 0; i < materialsPmx.Length; i++) {
                indexRanges[i] = new Range(s, materialsPmx[i].VertexCount);
                s += materialsPmx[i].VertexCount;
            }
            _indexRanges = new UnmanagedArray<Range>(indexRanges);          // alloc um
            _ibo = new UnmanagedArray<int>(_indexRanges.Length);            // alloc um

            for(int i = 0; i < indexRanges.Length; i++) {
                var subIndices = indices.Slice(indexRanges[i].Start, indexRanges[i].Length);
                var ibo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
                fixed(int* p = subIndices) {
                    GL.BufferData(BufferTarget.ElementArrayBuffer, subIndices.Length * sizeof(int), (IntPtr)p, BufferUsageHint.StaticDraw);
                }
                _ibo[i] = ibo;
            }
        }

        private void OnTerminated(FrameObject frameObject)
        {
            _vertexBoneInfo?.Dispose();
            _bones?.Dispose();
            _materials?.Dispose();
            _indexRanges?.Dispose();
            _ibo?.Dispose();
        }

        public unsafe static void LoadResource(string name, Layer layer)
        {
            using var stream = Resources.GetStream(name);
            var pmxObject = MMDTools.PMXParser.Parse(stream);
            var pmxModel = new PmxModel(pmxObject);
            pmxModel.Activate(layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReverseTrianglePolygon(Span<int> indexArray)
        {
            if(indexArray.Length % 3 != 0) { throw new ArgumentException($"Length of {nameof(indexArray)} must be divided by three."); }
            var surfaces = indexArray.MarshalCast<int, Int32_3>();
            for(int i = 0; i < surfaces.Length; i++) {
                (surfaces[i].Num1, surfaces[i].Num2) = (surfaces[i].Num2, surfaces[i].Num1);
            }
        }

        private static Material ToMaterial(MMDTools.Material m) => new Material(ToColor4(m.Ambient), ToColor4(m.Diffuse), ToColor4(m.Specular), m.Shininess);

        private static Bone ToBone(MMDTools.Bone bone) => new Bone(bone);
        
        private static VertexBoneInfo ToVertexBoneInfo(MMDTools.Vertex v) => new VertexBoneInfo(v);

        private static Vertex ToVertex(MMDTools.Vertex v) => new Vertex(ToVector3(v.Position), ToVector3(v.Normal), ToVector2(v.UV));

        private static Color4 ToColor4(MMDTools.Color color) => Unsafe.As<MMDTools.Color, Color4>(ref color);

        private static Vector3 ToVector3(MMDTools.Vector3 vector) => new Vector3(vector.X, vector.Y, -vector.Z);

        private static Vector2 ToVector2(MMDTools.Vector2 vector) => new Vector2(vector.X, vector.Y);


        private struct Int32_3 : IEquatable<Int32_3>
        {
#pragma warning disable 0649    // disable warning "field value is not set"
            internal int Num0;
            internal int Num1;
            internal int Num2;
#pragma warning restore 0649

            public readonly override bool Equals(object? obj) => obj is Int32_3 value && Equals(value);

            public readonly bool Equals(Int32_3 other) => Num0 == other.Num0 && Num1 == other.Num1 && Num2 == other.Num2;

            public readonly override int GetHashCode() => HashCode.Combine(Num0, Num1, Num2);
        }

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

            public Bone(MMDTools.Bone bone)
            {
                Position = ToVector3(bone.Position);
            }
        }

        private readonly struct Range : IEquatable<Range>
        {
            public readonly int Start;
            public readonly int Length;

            public Range(int start, int length)
            {
                Start = start;
                Length = length;
            }

            public override bool Equals(object? obj) => obj is Range range && Equals(range);

            public bool Equals(Range other) => Start == other.Start && Length == other.Length;

            public override int GetHashCode() => HashCode.Combine(Start, Length);

            public static bool operator ==(Range left, Range right) => left.Equals(right);

            public static bool operator !=(Range left, Range right) => !(left == right);
        }
    }
}
