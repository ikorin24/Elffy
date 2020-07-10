#nullable enable
using System;
using OpenToolkit.Graphics.OpenGL;
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
            // Call static constructor of TVertex to Register layout. (It is called only once)
            RuntimeHelpers.RunClassConstructor(typeof(TVertex).TypeHandle);

            var (offset, type, elementCount) = VertexMarshalHelper<TVertex>.Layout.Invoke(vertexFieldName);
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 4, (VertexAttribPointerType)type, false, sizeof(TVertex), offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Map<TVertex>(string vertexFieldName, string name) where TVertex : unmanaged
        {
            // Call static constructor of TVertex to Register layout. (It is called only once)
            RuntimeHelpers.RunClassConstructor(typeof(TVertex).TypeHandle);
            
            var (offset, type, elementCount) = VertexMarshalHelper<TVertex>.Layout.Invoke(vertexFieldName);
            var index = GL.GetAttribLocation(_program.Value, name);
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 4, (VertexAttribPointerType)type, false, sizeof(TVertex), offset);
        }


        public override int GetHashCode() => HashCode.Combine(_program);

        public override string ToString() => _program.ToString();
    }
}
