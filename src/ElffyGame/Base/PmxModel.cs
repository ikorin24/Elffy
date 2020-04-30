#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy;
using Elffy.Core;
using Elffy.Shading;
using Elffy.Components;
using Elffy.Serialization;
using Elffy.Effective;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace ElffyGame.Base
{
    public class PmxModel : Renderable
    {
        private readonly UnmanagedArray<Vertex> _vertexArray;
        private readonly UnmanagedArray<int> _indexArray;
        private readonly UnmanagedArray<VertexBoneInfo> _vertexBoneInfo;
        private readonly UnmanagedArray<Bone> _bones;

        private PmxModel(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray, ReadOnlySpan<VertexBoneInfo> vertexBoneInfo, ReadOnlySpan<Bone> bones)
        {
            _vertexArray = vertexArray.ToUnmanagedArray();
            _indexArray = indexArray.ToUnmanagedArray();
            _vertexBoneInfo = vertexBoneInfo.ToUnmanagedArray();
            _bones = bones.ToUnmanagedArray();
            Activated += OnActivated;
            Terminated += OnTerminated;
        }

        public void UpdateVertex(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            LoadGraphicBuffer(vertexArray, indexArray);
        }

        private void OnActivated(FrameObject frameObject)
        {
            LoadGraphicBuffer(_vertexArray.Ptr, _vertexArray.Length, _indexArray.Ptr, _indexArray.Length);
        }

        private void OnTerminated(FrameObject frameObject)
        {
            _vertexArray.Dispose();
            _indexArray.Dispose();
            _vertexBoneInfo.Dispose();
            _bones.Dispose();
        }


        public unsafe static void LoadResource(string name, Layer layer)
        {
            using var stream = Resources.GetStream(name);
            var pmxObject = MMDTools.PMXParser.Parse(stream);
            var vertexListPmx = pmxObject.VertexList.Span;
            var materialListPmx = pmxObject.MaterialList.Span;
            var indexListPmx = pmxObject.SurfaceList.Span.MarshalCast<MMDTools.Surface, int>();
            var boneListPmx = pmxObject.BoneList.Span;

            if(materialListPmx.Length == 0) { throw new ArgumentException(); }

            fixed(int* p = indexListPmx) {
                ReverseTrianglePolygon(new Span<int>(p, indexListPmx.Length));
            }

            using(var modelsPooled = new PooledArray<PmxModel>(materialListPmx.Length))
            using(var verticesPooled = vertexListPmx.SelectToPooledArray(ToVertex))
            using(var vertexBoneInfoPooled = vertexListPmx.SelectToPooledArray(ToVertexBoneInfo))
            using(var bonesPooled = boneListPmx.SelectToPooledArray(ToBone)) {
                var models = modelsPooled.AsSpan();
                var vertices = verticesPooled.AsSpan();
                var vertexBoneInfo = vertexBoneInfoPooled.AsSpan();
                var bones = bonesPooled.AsSpan();
                var sliceStart = 0;
                for(int i = 0; i < materialListPmx.Length; i++) {
                    var material = ToMaterial(materialListPmx[i]);
                    var indexList = indexListPmx.Slice(sliceStart, materialListPmx[i].VertexCount);
                    sliceStart += materialListPmx[i].VertexCount;
                    models[i] = new PmxModel(vertices, indexList, vertexBoneInfo, bones) { Material = material };
                }
                foreach(var m in models) {
                    m.Shader = ShaderSource.Phong;
                    m.Activate(layer);
                }
            }
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
    }
}
