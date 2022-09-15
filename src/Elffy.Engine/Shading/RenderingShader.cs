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
        // TODO: Change into 'ref feald' in the future for C# 11
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

    public readonly struct ShaderSource : IEquatable<ShaderSource>
    {
        public string? VertexShader { get; init; }
        public string? FragmentShader { get; init; }
        public string? GeometryShader { get; init; }

        public bool IsEmpty => VertexShader is null && FragmentShader is null && GeometryShader is null;

        public static ShaderSource Empty => default;

        public override bool Equals(object? obj) => obj is ShaderSource source && Equals(source);

        public bool Equals(ShaderSource other) =>
            VertexShader == other.VertexShader &&
            FragmentShader == other.FragmentShader &&
            GeometryShader == other.GeometryShader;

        public override int GetHashCode() => HashCode.Combine(VertexShader, FragmentShader, GeometryShader);

        public static bool operator ==(ShaderSource left, ShaderSource right) => left.Equals(right);

        public static bool operator !=(ShaderSource left, ShaderSource right) => !(left == right);
    }
}
