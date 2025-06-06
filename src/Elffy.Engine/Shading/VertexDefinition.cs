﻿#nullable enable
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
        public void Map(Type vertexType, int index, VertexFieldSemantics semantics)
        {
            if(vertexType is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(vertexType));
            }
            if(index < 0) {
                ThrowInvalidIndex();
            }
            VertexMapper.Map(vertexType, index, semantics);

            [DoesNotReturn] static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Type vertexType, string name, VertexFieldSemantics semantics)
        {
            ArgumentNullException.ThrowIfNull(name);
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0 && DevEnv.IsEnabled) {
                DevEnv.ForceWriteLine($"[warning] '{name}' vertex field input is not found in shader program({_program.Value}).");
            }
            else {
                VertexMapper.Map(vertexType, index, semantics);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TVertex>(int index, VertexFieldSemantics semantics) where TVertex : unmanaged, IVertex
        {
            if(index < 0) {
                ThrowInvalidIndex();
                [DoesNotReturn] static void ThrowInvalidIndex() => throw new ArgumentException($"{nameof(index)} is negative value.");
            }
            VertexMapper.Map<TVertex>(index, semantics);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TVertex>(string name, VertexFieldSemantics semantics) where TVertex : unmanaged, IVertex
        {
            ArgumentNullException.ThrowIfNull(name);
            var index = GL.GetAttribLocation(_program.Value, name);
            if(index < 0 && DevEnv.IsEnabled) {
                DevEnv.ForceWriteLine($"[warning] '{name}' vertex field input is not found in shader program({_program.Value}).");
            }
            else {
                VertexMapper.Map<TVertex>(index, semantics);
            }
        }

        public override int GetHashCode() => _program.GetHashCode();

        public override bool Equals(object? obj) => false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(VertexDefinition d) => _program.Equals(d._program);

        public override string ToString() => _program.ToString();
    }
}
