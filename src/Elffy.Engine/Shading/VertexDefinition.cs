#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Graphics.OpenGL;
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
        public void Map(Type vertexType, int index, VertexSpecialField specialField)
        {
            if(vertexType is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(vertexType));
            }
            if(index < 0) {
                ThrowInvalidIndex();
            }
            VertexMapper.Map(vertexType, index, specialField);

            [DoesNotReturn] static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Type vertexType, string name, VertexSpecialField specialField)
        {
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0 && DevEnv.IsEnabled) {
                DevEnv.ForceWriteLine($"[warning] '{name}' vertex field input is not found in shader program({_program.Value}).");
            }
            else {
                VertexMapper.Map(vertexType, index, specialField);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TVertex>(int index, string vertexFieldName) where TVertex : unmanaged
        {
            if(index < 0) {
                ThrowInvalidIndex();
            }
            VertexMapper.Map<TVertex>(index, vertexFieldName);

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
                VertexMapper.Map<TVertex>(index, vertexFieldName);
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
            VertexMapper.Map<TVertex>(index, vertexFieldName);

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
                VertexMapper.Map<TVertex>(index, vertexFieldName);
            }
        }

        public override int GetHashCode() => _program.GetHashCode();

        public override bool Equals(object? obj) => false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(VertexDefinition<TVertex> d) => _program.Equals(d._program);

        public override string ToString() => _program.ToString();
    }
}
