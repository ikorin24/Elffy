#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;

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
        public static unsafe void Map<TVertex>(int index, VertexSpecialField specialField) where TVertex : unmanaged
        {
            var vertexType = typeof(TVertex);
            Map(typeof(TVertex), index, specialField);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map(Type vertexType, int index, VertexSpecialField specialField)
        {
            // Call static constructor of TVertex to Register layout. (It is called only once)
            RuntimeHelpers.RunClassConstructor(vertexType.TypeHandle);
            var (offset, type, elementCount, vertexSize) = VertexMarshalHelper.GetLayout(vertexType, specialField);
            MapPrivate(index, offset, type, elementCount, vertexSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map<TVertex>(int index, string vertexFieldName) where TVertex : unmanaged
        {
            var vertexType = typeof(TVertex);
            Map(typeof(TVertex), index, vertexFieldName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Map(Type vertexType, int index, string vertexFieldName)
        {
            // Call static constructor of TVertex to Register layout. (It is called only once)
            RuntimeHelpers.RunClassConstructor(vertexType.TypeHandle);

            var (offset, type, elementCount, vertexSize) = VertexMarshalHelper.GetLayout(vertexType, vertexFieldName);
            MapPrivate(index, offset, type, elementCount, vertexSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MapPrivate(int index, int offset, VertexFieldMarshalType type, int elementCount, int vertexSize)
        {
            GL.EnableVertexAttribArray(index);

            if(type <= VertexFieldMarshalType.HalfFloat) {
                // float or half
                GL.VertexAttribPointer(index, elementCount, (VertexAttribPointerType)_attribTypes[(int)type], false, vertexSize, offset);
            }
            else {
                GL.VertexAttribIPointer(index, elementCount, (VertexAttribIntegerType)_attribTypes[(int)type], vertexSize, (IntPtr)offset);
            }
        }
    }
}
