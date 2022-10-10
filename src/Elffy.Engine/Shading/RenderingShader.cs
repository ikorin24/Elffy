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

        protected abstract ShaderSource GetShaderSource(in ShaderGetterContext context);

        protected abstract void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context);

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context);

        protected virtual void OnAttached(Renderable target) { }

        protected virtual void OnDetached(Renderable detachedTarget) { }

        protected virtual void OnProgramDisposed() { }      // nop

        ShaderSource IRenderingShader.GetShaderSourceInternal(in ShaderGetterContext context) => GetShaderSource(in context);
        void IRenderingShader.OnProgramDisposedInternal() => OnProgramDisposed();
        void IRenderingShader.OnAttachedInternal(Renderable target) => OnAttached(target);
        void IRenderingShader.OnDetachedInternal(Renderable detachedTarget) => OnDetached(detachedTarget);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(ProgramObject program, in LocationDefinitionContext context)
        {
            var definition = new VertexDefinition(program);
            DefineLocation(definition, in context);
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
        private readonly ObjectLayer _layer;
        private readonly Renderable _target;
        private readonly ref readonly Matrix4 _model;
        private readonly ref readonly Matrix4 _view;
        private readonly ref readonly Matrix4 _projection;

        public IHostScreen Screen => _screen;
        public ObjectLayer Layer => _layer;
        public Renderable Target => _target;
        public ref readonly Matrix4 Model => ref _model;
        public ref readonly Matrix4 View => ref _view;
        public ref readonly Matrix4 Projection => ref _projection;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderingContext() => throw new NotSupportedException("Don't use defaut constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderingContext(
            IHostScreen screen,
            ObjectLayer layer,
            Renderable target,
            in Matrix4 model,
            in Matrix4 view,
            in Matrix4 projection
        )
        {
            _screen = screen;
            _layer = layer;
            _target = target;
            _model = ref model;
            _view = ref view;
            _projection = ref projection;
        }
    }

    public readonly ref struct LocationDefinitionContext
    {
        private readonly IHostScreen _screen;
        private readonly ObjectLayer _layer;
        private readonly Renderable _target;
        private readonly Type _vertexType;

        public IHostScreen Screen => _screen;
        public ObjectLayer Layer => _layer;
        public Renderable Target => _target;
        public Type VertexType => _vertexType;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LocationDefinitionContext() => throw new NotSupportedException("Don't use default constructor.");

        public LocationDefinitionContext(IHostScreen screen, ObjectLayer layer, Renderable target, Type vertexType)
        {
            _screen = screen;
            _layer = layer;
            _target = target;
            _vertexType = vertexType;
        }
    }

    public readonly ref struct ShaderGetterContext
    {
        private readonly IHostScreen _screen;
        private readonly ObjectLayer _layer;
        private readonly Renderable _target;

        public IHostScreen Screen => _screen;
        public ObjectLayer Layer => _layer;
        public Renderable Target => _target;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ShaderGetterContext() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ShaderGetterContext(IHostScreen screen, ObjectLayer layer, Renderable target)
        {
            _screen = screen;
            _layer = layer;
            _target = target;
        }
    }
}
