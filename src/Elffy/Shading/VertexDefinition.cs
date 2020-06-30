#nullable enable
using System;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Core;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
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
        public unsafe void Position(int index)
        {
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Vertex.PositionOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Normal(int index)
        {
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Vertex.NormalOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Color(int index)
        {
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 4, VertexAttribPointerType.Float, false, sizeof(Vertex), Vertex.ColorOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void TexCoord(int index)
        {
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), Vertex.TexCoordOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Position(string name)
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0) { throw new ArgumentException($"Name not found : {name}"); }
            Position(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Normal(string name)
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0) { throw new ArgumentException($"Name not found : {name}"); }
            Normal(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Color(string name)
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0) { throw new ArgumentException($"Name not found : {name}"); }
            Color(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void TexCoord(string name)
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0) { throw new ArgumentException($"Name not found : {name}"); }
            TexCoord(index);
        }


        public override int GetHashCode() => HashCode.Combine(_program);

        public override string ToString() => _program.ToString();
    }
}
