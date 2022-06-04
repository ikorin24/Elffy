#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class RenderingShader : IRenderingShader
    {
        // [NOTE]
        // ShaderSource don't have any opengl resources. (e.g. ProgramObject)
        // Keep it thread-independent and context-free.

        private int _sourceHashCache;

        protected abstract string VertexShaderSource { get; }

        protected abstract string FragmentShaderSource { get; }

        protected virtual string? GeometryShaderSource { get; } = null;

        string IRenderingShader.VertexShaderSource => VertexShaderSource;

        string IRenderingShader.FragmentShaderSource => FragmentShaderSource;

        string? IRenderingShader.GeometryShaderSource => GeometryShaderSource;

        protected abstract void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType);

        protected abstract void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        protected virtual void OnProgramDisposed() { }      // nop

        void IRenderingShader.InvokeOnProgramDisposed() => OnProgramDisposed();

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

        int IRenderingShader.GetSourceHash()
        {
            if(_sourceHashCache == 0) {
                _sourceHashCache = HashCode.Combine(VertexShaderSource, FragmentShaderSource, GeometryShaderSource);
            }
            return _sourceHashCache;
        }
    }

    public readonly ref struct RenderingContext
    {
        // TODO: Change into 'ref feald' in the future for C# 10
        private readonly ReadOnlySpan<RenderingContextInternal> _c;   // Length must be 1
        private ref RenderingContextInternal Context => ref MemoryMarshal.GetReference(_c);

        public IHostScreen Screen => Context.Screen;
        public Renderable Target
        {
            get
            {
                Debug.Assert(Context.Target is not null);
                return Context.Target;
            }
        }
        public ref readonly Matrix4 Model => ref Context.Model;
        public ref readonly Matrix4 View => ref Context.View;
        public ref readonly Matrix4 Projection => ref Context.Projection;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderingContext() => throw new NotSupportedException("Don't use defaut constructor.");

        internal RenderingContext(in RenderingContextInternal contextInternal)
        {
            _c = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in contextInternal), 1);
        }

        internal ref Matrix4 GetModelRef() => ref Context.Model;
        internal ref Matrix4 GetViewRef() => ref Context.View;
        internal ref Matrix4 GetProjectionRef() => ref Context.Projection;

        internal void SetTarget(Renderable? target)
        {
            Context.Target = target;
        }
    }

    internal struct RenderingContextInternal
    {
        public IHostScreen Screen;
        public Renderable? Target;
        public Matrix4 Model;
        public Matrix4 View;
        public Matrix4 Projection;
    }
}
