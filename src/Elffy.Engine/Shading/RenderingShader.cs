#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class RenderingShader : IRenderingShader
    {
        // [NOTE]
        // ShaderSource don't have any opengl resources. (e.g. ProgramObject)
        // Keep it thread-independent and context-free.

        protected abstract ShaderSource GetShaderSource(Renderable target, ObjectLayer layer);

        protected abstract void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType);

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        protected virtual void OnAttached(Renderable target) { }

        protected virtual void OnDetached(Renderable detachedTarget) { }

        protected virtual void OnProgramDisposed() { }      // nop

        ShaderSource IRenderingShader.GetShaderSourceInternal(Renderable target, ObjectLayer layer) => GetShaderSource(target, layer);
        void IRenderingShader.OnProgramDisposedInternal() => OnProgramDisposed();
        void IRenderingShader.OnAttachedInternal(Renderable target) => OnAttached(target);
        void IRenderingShader.OnDetachedInternal(Renderable detachedTarget) => OnDetached(detachedTarget);

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
    }

    public readonly ref struct RenderingContext
    {
        private readonly ref RenderingContextInternal _context;

        public IHostScreen Screen => _context.Screen;
        public Renderable Target => _context.Target;
        public ref readonly Matrix4 Model => ref _context.Model;
        public ref readonly Matrix4 View => ref _context.View;
        public ref readonly Matrix4 Projection => ref _context.Projection;

        internal ref Matrix4 ModelMut => ref _context.Model;
        internal ref Matrix4 ViewMut => ref _context.View;
        internal ref Matrix4 ProjectionMut => ref _context.Projection;
        internal ref Renderable TargetMut => ref _context.Target;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderingContext() => throw new NotSupportedException("Don't use defaut constructor.");

        internal RenderingContext(ref RenderingContextInternal context)
        {
            _context = ref context;
        }
    }

    internal struct RenderingContextInternal
    {
        public IHostScreen Screen;
        public Renderable Target;
        public Matrix4 Model;
        public Matrix4 View;
        public Matrix4 Projection;
    }
}
