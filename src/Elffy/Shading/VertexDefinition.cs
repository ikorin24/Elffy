#nullable enable
using System;
using OpenToolkit.Graphics.OpenGL4;
using Elffy.Core;
using System.Runtime.CompilerServices;
using Elffy.OpenGL;

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
        public unsafe void Map<TVertex>(string vertexFieldName, int index) where TVertex : unmanaged
        {
            if(index < 0) {
                ThrowInvalidIndex();
            }
            MapPrivate<TVertex>(vertexFieldName, index);

            static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Map<TVertex>(string vertexFieldName, string name) where TVertex : unmanaged
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0) {
                ThrowVertexShaderFieldNotFound(name);
            }
            MapPrivate<TVertex>(vertexFieldName, index);

            static void ThrowVertexShaderFieldNotFound(string name)
                => throw new ArgumentException($"Shader field of name '{name}' is not found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void MapPrivate<TVertex>(string vertexFieldName, int index) where TVertex : unmanaged
        {
            // Call static constructor of TVertex to Register layout. (It is called only once)
            RuntimeHelpers.RunClassConstructor(typeof(TVertex).TypeHandle);

            var (offset, type, elementCount) = VertexMarshalHelper<TVertex>.GetLayout(vertexFieldName);
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, elementCount, (VertexAttribPointerType)type, false, sizeof(TVertex), offset);
        }


        public override int GetHashCode() => HashCode.Combine(_program);

        public override string ToString() => _program.ToString();
    }
}
