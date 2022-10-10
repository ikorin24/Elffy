#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.UI;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class UIRenderingShader : IRenderingShader
    {
        protected abstract void DefineLocation(VertexDefinition definition, Control target, Type vertexType);

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        protected virtual void OnProgramDisposed() { }  // nop

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(ProgramObject program, Control target, Type vertexType)
        {
            DefineLocation(new VertexDefinition(program), target, vertexType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnRenderingInternal(ProgramObject program, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            OnRendering(new ShaderDataDispatcher(program), target, model, view, projection);
        }

        protected abstract ShaderSource GetShaderSource(in ShaderGetterContext context);

        void IRenderingShader.OnProgramDisposedInternal() => OnProgramDisposed();

        void IRenderingShader.OnAttachedInternal(Renderable target) { }   // nop

        void IRenderingShader.OnDetachedInternal(Renderable target) { }   // nop

        ShaderSource IRenderingShader.GetShaderSourceInternal(in ShaderGetterContext context) => GetShaderSource(in context);
    }
}
