#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Diagnostics;

namespace Elffy.Shading
{
    public readonly ref struct VertexDefinition
    {
        private readonly ProgramObject _program;

        internal VertexDefinition(ProgramObject program)
        {
            _program = program;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TVertex>(int index, string vertexFieldName) where TVertex : unmanaged
        {
            if(index < 0) {
                ThrowInvalidIndex();
            }
            VertexMapHelper.Map<TVertex>(index, vertexFieldName);

            [DoesNotReturn] static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TVertex>(string name, string vertexFieldName) where TVertex : unmanaged
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0 && DevEnv.IsEnabled) {
                DevEnv.ForceWriteLine($"[warning] '{name}' vertex field input is not found in shader program({_program.Value}).");
            }
            else {
                VertexMapHelper.Map<TVertex>(index, vertexFieldName);
            }
        }

        public override int GetHashCode() => _program.GetHashCode();

        public override bool Equals(object? obj) => false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(VertexDefinition d) => _program.Equals(d._program);

        public override string ToString() => _program.ToString();
    }


    public readonly ref struct VertexDefinition<TVertex> where TVertex : unmanaged
    {
        private readonly ProgramObject _program;

        internal VertexDefinition(ProgramObject program)
        {
            _program = program;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(int index, string vertexFieldName)
        {
            if(index < 0) {
                ThrowInvalidIndex();
            }
            VertexMapHelper.Map<TVertex>(index, vertexFieldName);

            [DoesNotReturn] static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(string name, string vertexFieldName)
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0 && DevEnv.IsEnabled) {
                DevEnv.ForceWriteLine($"[warning] '{name}' vertex field input is not found in shader program({_program.Value}).");
            }
            else {
                VertexMapHelper.Map<TVertex>(index, vertexFieldName);
            }
        }

        public override int GetHashCode() => _program.GetHashCode();

        public override bool Equals(object? obj) => false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(VertexDefinition<TVertex> d) => _program.Equals(d._program);

        public override string ToString() => _program.ToString();
    }

    internal static class VertexMapHelper
    {
        private static readonly int[] _attribTypes;

        static VertexMapHelper()
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
        public static unsafe void Map<TVertex>(int index, string vertexFieldName) where TVertex : unmanaged
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
    }
}
