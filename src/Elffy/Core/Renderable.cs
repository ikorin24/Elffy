#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Shading;
using Elffy.OpenGL;
using Elffy.Effective;
using Elffy.Diagnostics;
using System.Diagnostics;

namespace Elffy.Core
{
    /// <summary>Base class that is rendered on the screen.</summary>
    public abstract class Renderable : Positionable
    {
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;
        private ShaderSource? _shader;
        private ShaderProgram? _shaderProgram;
        private int _instancingCount;

        /// <summary>Vertex buffer object</summary>
        public ref readonly VBO VBO => ref _vbo;
        /// <summary>Index buffer object</summary>
        public ref readonly IBO IBO => ref _ibo;
        /// <summary>Vertex array object</summary>
        public ref readonly VAO VAO => ref _vao;

        /// <summary>Get whether the <see cref="Renderable"/> is loaded and ready to be rendered.</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>Get or set whether the <see cref="Renderable"/> is visible in rendering.</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>Get or set a shader source</summary>
        public ShaderSource? Shader
        {
            get => _shader;
            set
            {
                if(IsLoaded) { ThrowAlreadyLoaded(); }
                _shader = value;

                [DoesNotReturn] static void ThrowAlreadyLoaded() => throw new InvalidOperationException("already loaded");
            }
        }

        /// <summary>Get or set instancing count. No instancing if 0.</summary>
        public int InstancingCount
        {
            get => _instancingCount;
            set
            {
                if(value < 0) { ThrowOutOfRange(); }
                _instancingCount = value;

                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        /// <summary>Not null if <see cref="IsLoaded"/> == true</summary>
        public ShaderProgram? ShaderProgram => _shaderProgram;

        /// <summary>Before-rendering event</summary>
        public event RenderingEventHandler? Rendering;
        /// <summary>After-rendering event</summary>
        public event RenderingEventHandler? Rendered;

        public Renderable()
        {
        }

        /// <summary>Render the <see cref="Renderable"/>.</summary>
        /// <param name="projection">projection matrix</param>
        /// <param name="view">view matrix</param>
        /// <param name="modelParent">parent model matrix</param>
        internal unsafe void Render(in Matrix4 projection, in Matrix4 view, in Matrix4 modelParent)
        {
            //var withoutScale = modelParent * Position.ToTranslationMatrix4() * Rotation.ToMatrix4();
            var withoutScale = modelParent;
            withoutScale.M03 += Position.X;
            withoutScale.M13 += Position.Y;
            withoutScale.M23 += Position.Z;
            withoutScale *= Rotation.ToMatrix4();

            // var model = withoutScale * Scale.ToScaleMatrix4();
            var model = withoutScale;
            model.M00 *= Scale.X;
            model.M11 *= Scale.Y;
            model.M22 *= Scale.Z;

            if(IsLoaded && IsVisible && !(_shaderProgram is null)) {
                Rendering?.Invoke(this, in model, in view, in projection);
                OnRendering(in model, in view, in projection);
                Rendered?.Invoke(this, in model, in view, in projection);
            }

            if(HasChild) {
                foreach(var child in Children.AsReadOnlySpan()) {
                    if(child is Renderable renderable) {
                        renderable.Render(projection, view, withoutScale);
                    }
                }
            }
        }

        protected virtual void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            _shaderProgram!.Apply(this, in model, in view, in projection);
            DrawElements(IBO.Length, 0);
            VAO.Unbind();
            IBO.Unbind();
        }

        protected void DrawElements(int count, int byteOffset)
        {
            if(_instancingCount == 0) {
                GL.DrawElements(BeginMode.Triangles, count, DrawElementsType.UnsignedInt, byteOffset);
            }
            else {
                GL.DrawElementsInstanced(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (IntPtr)byteOffset, _instancingCount);
            }
        }

        /// <summary>Load buffer data</summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertex data to load</param>
        /// <param name="indices">index data to load</param>
        protected void LoadGraphicBuffer<TVertex>(Span<TVertex> vertices, Span<int> indices) where TVertex : unmanaged
        {
            LoadGraphicBuffer(vertices.AsReadOnly(), indices.AsReadOnly());
        }

        /// <summary>Load buffer data</summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertex data to load</param>
        /// <param name="indices">index data to load</param>
        protected void LoadGraphicBuffer<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            HostScreen.ThrowIfNotMainThread();
            if(IsLoaded) { throw new InvalidOperationException("already loaded"); }

            var shader = _shader ?? EmptyShaderSource<TVertex>.Instance;

            // checking target vertex type of shader is valid.
            if(DevEnv.IsEnabled) {
                ShaderTargetVertexTypeAttribute.CheckVertexType(shader.GetType(), typeof(TVertex));
            }

            _vbo = VBO.Create();
            VBO.BindBufferData(ref _vbo, vertices, BufferUsageHint.StaticDraw);
            _ibo = IBO.Create();
            IBO.BindBufferData(ref _ibo, indices, BufferUsageHint.StaticDraw);
            _vao = VAO.Create();
            VAO.Bind(_vao);
            Debug.Assert(_shaderProgram is null);
            _shaderProgram = shader.Compile();
            _shaderProgram.Initialize(this);
            IsLoaded = true;
        }


        protected override void OnDead()
        {
            var isLoaded = IsLoaded;
            IsLoaded = false;
            base.OnDead();
            _shaderProgram?.Dispose();
            _shaderProgram = null;
            if(isLoaded) {
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
        }
    }

    public delegate void RenderingEventHandler(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection);
}
