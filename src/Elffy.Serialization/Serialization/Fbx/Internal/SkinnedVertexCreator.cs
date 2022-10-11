#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Serialization.Fbx.Internal
{
    internal unsafe readonly struct SkinnedVertexCreator<TVertex> where TVertex : unmanaged, IVertex
    {
        public VertexFieldAccessor<Vector3> Pos { get; init; }
        public VertexFieldAccessor<Vector3> Normal { get; init; }
        public VertexFieldAccessor<Vector2> UV { get; init; }
        public VertexFieldAccessor<int> TexIndex { get; init; }
        public VertexFieldAccessor<Vector4i> Bone { get; init; }
        public VertexFieldAccessor<Vector4> Weight { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector3 PositionOf(ref TVertex v) => ref Pos.FieldRef(ref v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector3 NormalOf(ref TVertex v) => ref Normal.FieldRef(ref v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector2 UVOf(ref TVertex v) => ref UV.FieldRef(ref v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int TextureIndexOf(ref TVertex v) => ref TexIndex.FieldRef(ref v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector4i BoneOf(ref TVertex v) => ref Bone.FieldRef(ref v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector4 WeightOf(ref TVertex v) => ref Weight.FieldRef(ref v);

        public static SkinnedVertexCreator<TVertex> Build()
        {
            return new()
            {
                Pos = TVertex.GetPositionAccessor(),
                Normal = TVertex.GetNormalAccessor(),
                UV = TVertex.GetUVAccessor(),
                TexIndex = TVertex.GetAccessor<int>(VertexFieldSemantics.TextureIndex),
                Bone = TVertex.GetAccessor<Vector4i>(VertexFieldSemantics.Bone),
                Weight = TVertex.GetAccessor<Vector4>(VertexFieldSemantics.Weight),
            };
        }
    }
}
