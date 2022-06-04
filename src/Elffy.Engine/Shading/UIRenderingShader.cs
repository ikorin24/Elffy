#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.UI;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class UIRenderingShader : IRenderingShader
    {
        private int _sourceHashCache;

        protected abstract string VertexShaderSource { get; }

        protected abstract string FragmentShaderSource { get; }

        string IRenderingShader.VertexShaderSource => VertexShaderSource;

        string IRenderingShader.FragmentShaderSource => FragmentShaderSource;

        string? IRenderingShader.GeometryShaderSource => null;

        protected abstract void DefineLocation(VertexDefinition<VertexSlim> definition, Control target);

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        protected virtual void OnProgramDisposed() { }  // nop

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(ProgramObject program, Control target)
        {
            DefineLocation(new VertexDefinition<VertexSlim>(program), target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnRenderingInternal(ProgramObject program, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            OnRendering(new ShaderDataDispatcher(program), target, model, view, projection);
        }

        int IRenderingShader.GetSourceHash()
        {
            if(_sourceHashCache == 0) {
                _sourceHashCache = HashCode.Combine(VertexShaderSource, FragmentShaderSource);
            }
            return _sourceHashCache;
        }

        void IRenderingShader.InvokeOnProgramDisposed() => OnProgramDisposed();
    }
}
