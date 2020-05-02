#nullable enable
using System;
using Elffy;
using Elffy.Core;
using Elffy.Effective;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using UnmanageUtility;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ElffyGame.Base
{
    public class PmxModel : Renderable
    {
        private UnmanagedArray<VertexBoneInfo>? _vertexBoneInfo;
        private UnmanagedArray<Bone>? _bones;
        private UnmanagedArray<Material>? _materials;
        private UnmanagedArray<int>? _partVertexCount;

        private MMDTools.PMXObject? _pmxObject;

        private bool _resourceReleased;

        private readonly object _syncRoot = new object();

        private unsafe PmxModel(MMDTools.PMXObject pmxObject)
        {
            Debug.Assert(pmxObject != null);
            _pmxObject = pmxObject;
            Activated += OnActivated;
            Terminated += OnTerminated;
            Rendered += OnRendered;
        }

        private unsafe void OnRendered(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
            var partVertexCount = _partVertexCount!.AsSpan();
            var pos = 0;
            foreach(var count in partVertexCount) {
                GL.DrawElements(BeginMode.Triangles, count, DrawElementsType.UnsignedInt, pos * sizeof(int));
                pos += count;
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
            // not await
            _ = Task.Factory.StartNew(v => ((UnmanagedArray<Vertex>)v)?.Dispose(), vertices);
            _pmxObject = null;
        }

        private void OnTerminated(FrameObject frameObject)
        {
            Task.Factory.StartNew(ReleaseResource);
        }

        private UnmanagedArray<Vertex>? LoadData()
        {
            var pmxObject = _pmxObject!;
            lock(_syncRoot) {
                if(_resourceReleased) { return null; }
                _vertexBoneInfo = pmxObject.VertexList.Span.SelectToUnmanagedArray(ToVertexBoneInfo);
                _bones = pmxObject.BoneList.Span.SelectToUnmanagedArray(ToBone);
                _materials = pmxObject.MaterialList.Span.SelectToUnmanagedArray(ToMaterial);
                _partVertexCount = pmxObject.MaterialList.Span.SelectToUnmanagedArray(m => m.VertexCount);
            }
            ReverseTrianglePolygon(pmxObject.SurfaceList.Span);
            return pmxObject.VertexList.Span.SelectToUnmanagedArray(ToVertex);
        }

        private void ReleaseResource()
        {
            lock(_syncRoot) {
                _resourceReleased = true;    // Writing boolean is atomic.
                _vertexBoneInfo?.Dispose();
                _bones?.Dispose();
                _materials?.Dispose();
                _partVertexCount?.Dispose();
            }
        }

        public static Task<PmxModel> LoadResourceAsync(string name)
        {
            return Task.Factory.StartNew(n =>
            {
                using var stream = Resources.GetStream((string)n);
                var pmx = MMDTools.PMXParser.Parse(stream);     // PMXParser.Parse is thread-independed, thread-safe.
                return new PmxModel(pmx);
            }, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReverseTrianglePolygon(ReadOnlySpan<MMDTools.Surface> surfaceList)
        {
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

            public Bone(MMDTools.Bone bone)
            {
                Position = ToVector3(bone.Position);
            }
        }
    }
}
