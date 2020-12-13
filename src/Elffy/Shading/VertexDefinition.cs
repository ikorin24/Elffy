#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Diagnostics;

namespace Elffy.Shading
{
    public readonly ref struct VertexDefinition
    {
        private static readonly int[] _attribTypes;

        static VertexDefinition()
        {
            _attribTypes = new int[7];   // TODO: get element count from enum

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

        private readonly ProgramObject _program;

        internal VertexDefinition(ProgramObject program)
        {
            _program = program;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Map<TVertex>(string vertexFieldName, int index) where TVertex : unmanaged
        {
            if(index < 0) {
                ThrowInvalidIndex();
            }
            MapPrivate<TVertex>(vertexFieldName, index);

            static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Map<TVertex>(string vertexFieldName, string name) where TVertex : unmanaged  // TODO: 引数の順序逆の方がいい、uniform に合わせる
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0 && DevEnv.IsEnabled) {
                DevEnv.ForceWriteLine($"[warning] '{name}' vertex field input is not found in shader program({_program.Value}).");
            }
            else {
                MapPrivate<TVertex>(vertexFieldName, index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void MapPrivate<TVertex>(string vertexFieldName, int index) where TVertex : unmanaged
        {
            // Call static constructor of TVertex to Register layout. (It is called only once)
            RuntimeHelpers.RunClassConstructor(typeof(TVertex).TypeHandle);

            var (offset, type, elementCount) = VertexMarshalHelper<TVertex>.GetLayout(vertexFieldName);
            GL.EnableVertexAttribArray(index);

            if(type <= VertexFieldMarshalType.HalfFloat) {
                // float or half
                GL.VertexAttribPointer(index, elementCount, (VertexAttribPointerType)_attribTypes[(int)type], false, sizeof(TVertex), offset);
            }
            else {
                GL.VertexAttribIPointer(index, elementCount, (VertexAttribIntegerType)_attribTypes[(int)type], sizeof(TVertex), (IntPtr)offset);
            }
        }


        public override int GetHashCode() => HashCode.Combine(_program);

        public override string ToString() => _program.ToString();
    }
}
