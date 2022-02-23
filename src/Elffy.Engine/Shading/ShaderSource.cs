#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class ShaderSource : IShaderSource
    {
        // [NOTE]
        // ShaderSource don't have any opengl resources. (e.g. ProgramObject)
        // Keep it thread-independent and context-free.

        private int _sourceHashCache;

        protected abstract string VertexShaderSource { get; }

        protected abstract string FragmentShaderSource { get; }

        protected virtual string? GeometryShaderSource { get; } = null;

        string IShaderSource.VertexShaderSource => VertexShaderSource;

        string IShaderSource.FragmentShaderSource => FragmentShaderSource;

        string? IShaderSource.GeometryShaderSource => GeometryShaderSource;

        protected abstract void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType);

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(ProgramObject program, Renderable target, Type vertexType)
        {
            DefineLocation(new VertexDefinition(program), target, vertexType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(VertexDefinition definition, Renderable target, Type vertexType)
        {
            DefineLocation(definition, target, vertexType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnRenderingInternal(ProgramObject program, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            OnRendering(new ShaderDataDispatcher(program), target, model, view, projection);
        }

        ShaderProgram IShaderSource.Compile(Renderable owner) => Compile(owner);

        internal ShaderProgram Compile(Renderable owner)
        {
            return ShaderProgram.Create(owner);
        }

        int IShaderSource.GetSourceHash() => GetSourceHash();

        internal int GetSourceHash()
        {
            if(_sourceHashCache == 0) {
                _sourceHashCache = HashCode.Combine(VertexShaderSource, FragmentShaderSource, GeometryShaderSource);
            }
            return _sourceHashCache;
        }
    }
}
