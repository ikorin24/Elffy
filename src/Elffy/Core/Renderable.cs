#nullable enable
using System;
using OpenToolkit.Graphics.OpenGL4;
using Elffy.Threading;
using Elffy.Shading;
using Elffy.OpenGL;
using Elffy.Effective;
using Elffy.Diagnostics;
using Elffy.AssemblyServices;

namespace Elffy.Core
{
    /// <summary>
    /// 画面に描画されるオブジェクトの基底クラス<para/>
    /// 描画に関する操作を提供します<para/>
    /// </summary>
    public abstract class Renderable : Positionable
    {
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;
        private ShaderSource _shader = PhongShaderSource.Instance;
        private ShaderProgram? _shaderProgram;
        private int _instancingCount;

        /// <summary>Vertex Buffer Object</summary>
        public ref readonly VBO VBO => ref _vbo;
        /// <summary>Index Buffer Object</summary>
        public ref readonly IBO IBO => ref _ibo;
        /// <summary>VAO</summary>
        public ref readonly VAO VAO => ref _vao;

        public bool IsLoaded { get; private set; }

        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        public ShaderSource Shader
        {
            get => _shader;
            set
            {
                if(value is null) { ThrowNullArg(); }
                if(IsLoaded) { ThrowAlreadyLoaded(); }
                _shader = value!;

                static void ThrowNullArg() => throw new ArgumentNullException(nameof(value));
                static void ThrowAlreadyLoaded() => throw new InvalidOperationException("already loaded");
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

                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        /// <summary>Not null if <see cref="IsLoaded"/> == true</summary>
        protected ShaderProgram? ShaderProgram => _shaderProgram;

        /// <summary>Before-rendering event</summary>
        public event RenderingEventHandler? Rendering;
        /// <summary>After-rendering event</summary>
        public event RenderingEventHandler? Rendered;

        public Renderable()
        {
        }

        /// <summary>描画を行います</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">view 行列</param>
        /// <param name="modelParent">親の model 行列</param>
        internal unsafe void Render(in Matrix4 projection, in Matrix4 view, in Matrix4 modelParent)
        {
            var withoutScale = modelParent * Position.ToTranslationMatrix4() * Rotation.ToMatrix4();
            var model = withoutScale * Scale.ToScaleMatrix4();

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
            _shaderProgram!.Apply(this, Layer.Lights, in model, in view, in projection);
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


        protected unsafe void LoadGraphicBuffer<TVertex>(Span<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
            => LoadGraphicBuffer(vertices.AsReadOnly(), indices);

        /// <summary>指定の頂点配列とインデックス配列で VBO, IBO を作成し、VAO を作成します</summary>
        /// <param name="vertices">頂点配列</param>
        /// <param name="indices">インデックス配列</param>
        protected unsafe void LoadGraphicBuffer<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            Dispatcher.ThrowIfNotMainThread();
            if(IsLoaded) { throw new InvalidOperationException("already loaded"); }

            // checking target vertex type of shader is valid.
            if(DevEnv.IsEnabled) {
                ShaderTargetVertexTypeAttribute.CheckVertexType(_shader.GetType(), typeof(TVertex));
            }

            _vbo = VBO.Create();
            VBO.BindBufferData(ref _vbo, vertices, BufferUsageHint.StaticDraw);
            _ibo = IBO.Create();
            IBO.BindBufferData(ref _ibo, indices, BufferUsageHint.StaticDraw);
            _vao = VAO.Create();
            VAO.Bind(_vao);
            IsLoaded = true;
            _shaderProgram?.Dispose();
            _shaderProgram = _shader.Compile();
            _shaderProgram.Initialize(this);
        }


        protected override void OnDead()
        {
            base.OnDead();
            _shaderProgram?.Dispose();
            _shaderProgram = null;
            if(IsLoaded) {
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
        }
    }

    public delegate void RenderingEventHandler(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection);
}
