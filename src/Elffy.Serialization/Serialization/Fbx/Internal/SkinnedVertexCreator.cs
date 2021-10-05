#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Serialization.Fbx.Internal
{
    internal unsafe readonly struct SkinnedVertexCreator<TVertex> where TVertex : unmanaged
    {
        private int OffsetPos { get; init; }
        private int OffsetNormal { get; init; }
        private int OffsetUV { get; init; }
        private int OffsetTextureIndex { get; init; }
        private int OffsetBone { get; init; }
        private int OffsetWeight { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector3 PositionOf(ref TVertex v) => ref FieldOf<Vector3>(ref v, OffsetPos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector3 NormalOf(ref TVertex v) => ref FieldOf<Vector3>(ref v, OffsetNormal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector2 UVOf(ref TVertex v) => ref FieldOf<Vector2>(ref v, OffsetUV);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int TextureIndexOf(ref TVertex v) => ref FieldOf<int>(ref v, OffsetTextureIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector4i BoneOf(ref TVertex v) => ref FieldOf<Vector4i>(ref v, OffsetBone);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector4 WeightOf(ref TVertex v) => ref FieldOf<Vector4>(ref v, OffsetWeight);

        public static SkinnedVertexCreator<TVertex> Build()
        {
            if(VertexMarshalHelper.TryGetVertexTypeData(typeof(TVertex), out var typeData) == false) {
                ThrowInvalidVertexType();
            }

            return new SkinnedVertexCreator<TVertex>()
            {
                OffsetPos = typeData.GetField(VertexSpecialField.Position).ByteOffset,
                OffsetNormal = typeData.GetField(VertexSpecialField.Normal).ByteOffset,
                OffsetUV = typeData.GetField(VertexSpecialField.UV).ByteOffset,
                OffsetTextureIndex = typeData.GetField(VertexSpecialField.TextureIndex).ByteOffset,
                OffsetBone = typeData.GetField(VertexSpecialField.Bone).ByteOffset,
                OffsetWeight = typeData.GetField(VertexSpecialField.Weight).ByteOffset,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref TField FieldOf<TField>(ref TVertex v, int offset)
        {
            return ref Unsafe.As<byte, TField>(
                        ref Unsafe.Add(
                            ref Unsafe.As<TVertex, byte>(ref v), offset));
        }

        [DoesNotReturn]
        private static void ThrowInvalidVertexType() => throw new InvalidOperationException($"The type is not supported vertex type. (Type = {typeof(TVertex).FullName})");
    }
}
