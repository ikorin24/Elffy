#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.UI;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class UIShaderSource : IShaderSource
    {
        private int _sourceHashCache;

        protected abstract string VertexShaderSource { get; }

        protected abstract string FragmentShaderSource { get; }

        string IShaderSource.VertexShaderSource => VertexShaderSource;

        string IShaderSource.FragmentShaderSource => FragmentShaderSource;

        string? IShaderSource.GeometryShaderSource => null;

        protected abstract void DefineLocation(VertexDefinition<VertexSlim> definition, Control target);

        protected abstract void SendUniforms(Uniform uniform, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(ProgramObject program, Control target)
        {
            DefineLocation(new VertexDefinition<VertexSlim>(program), target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniformsInternal(ProgramObject program, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            SendUniforms(new Uniform(program), target, model, view, projection);
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
                _sourceHashCache = HashCode.Combine(VertexShaderSource, FragmentShaderSource);
            }
            return _sourceHashCache;
        }
    }
}
