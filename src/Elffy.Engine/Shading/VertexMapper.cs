#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    internal static class VertexMapper
    {
        private static readonly int[] _attribTypes;

        static VertexMapper()
        {
            const int Count = 7;    // count of VertexFieldMarshalType elements
            _attribTypes = new int[Count];

            // float type
            _attribTypes[(int)VertexFieldMarshalType.Float] = (int)VertexAttribPointerType.Float;
            _attribTypes[(int)VertexFieldMarshalType.HalfFloat] = (int)VertexAttribPointerType.HalfFloat;

            // int type
            _attribTypes[(int)VertexFieldMarshalType.Uint32] = (int)VertexAttribIntegerType.UnsignedInt;
            _attribTypes[(int)VertexFieldMarshalType.Int32] = (int)VertexAttribIntegerType.Int;
            _attribTypes[(int)VertexFieldMarshalType.Byte] = (int)VertexAttribIntegerType.UnsignedByte;
            _attribTypes[(int)VertexFieldMarshalType.Int16] = (int)VertexAttribIntegerType.Short;
            _attribTypes[(int)VertexFieldMarshalType.Uint16] = (int)VertexAttribIntegerType.UnsignedShort;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map<TVertex>(int index, VertexFieldSemantics semantics) where TVertex : unmanaged, IVertex
        {
            var field = TVertex.VertexTypeData.GetField(semantics);
            MapPrivate(index, field, TVertex.VertexSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map(Type vertexType, int index, VertexFieldSemantics semantics)
        {
            var typeData = VertexTypeData.GetVertexTypeData(vertexType);
            var field = typeData.GetField(semantics);
            MapPrivate(index, field, typeData.VertexSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map<TVertex>(int index, string vertexFieldName) where TVertex : unmanaged, IVertex
        {
            var field = TVertex.VertexTypeData.GetField(vertexFieldName);
            MapPrivate(index, field, TVertex.VertexSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map(Type vertexType, int index, string vertexFieldName)
        {
            ArgumentNullException.ThrowIfNull(vertexFieldName);
            var typeData = VertexTypeData.GetVertexTypeData(vertexType);
            var field = typeData.GetField(vertexFieldName);
            MapPrivate(index, field, typeData.VertexSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MapPrivate(int index, VertexFieldData field, int vertexSize)
        {
            GL.EnableVertexAttribArray(index);

            var marshalType = field.MarshalType;
            if(marshalType <= VertexFieldMarshalType.HalfFloat) {
                // float or half
                GL.VertexAttribPointer(index, field.MarshalCount, (VertexAttribPointerType)_attribTypes[(int)marshalType], false, vertexSize, field.ByteOffset);
            }
            else {
                GL.VertexAttribIPointer(index, field.MarshalCount, (VertexAttribIntegerType)_attribTypes[(int)marshalType], vertexSize, (IntPtr)field.ByteOffset);
            }
        }
    }
}
