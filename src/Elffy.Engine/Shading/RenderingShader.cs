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

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context);

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
        internal void OnRenderingInternal(ProgramObject program, in RenderingContext context)
        {
            OnRendering(new ShaderDataDispatcher(program), context);
        }
    }

    public readonly ref struct RenderingContext
    {
        private readonly IHostScreen _screen;
        private readonly Renderable _target;
        private readonly ref readonly Matrix4 _model;
        private readonly ref readonly Matrix4 _view;
        private readonly ref readonly Matrix4 _projection;

        public IHostScreen Screen => _screen;
        public Renderable Target => _target;
        public ref readonly Matrix4 Model => ref _model;
        public ref readonly Matrix4 View => ref _view;
        public ref readonly Matrix4 Projection => ref _projection;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderingContext() => throw new NotSupportedException("Don't use defaut constructor.");

        internal RenderingContext(
            IHostScreen screen,
            Renderable target,
            in Matrix4 model,
            in Matrix4 view,
            in Matrix4 projection
        )
        {
            _screen = screen;
            _target = target;
            _model = ref model;
            _view = ref view;
            _projection = ref projection;
        }
    }
}
