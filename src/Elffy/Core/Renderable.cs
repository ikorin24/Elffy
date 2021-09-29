#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Shading;
using Elffy.Shading.Forward;
using Elffy.Graphics.OpenGL;
using Elffy.UI;
using System.Diagnostics;

namespace Elffy.Core
{
    /// <summary>Base class that is rendered on the screen.</summary>
    public abstract class Renderable : Positionable
    {
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;
        private IShaderSource? _shader;     // ShaderSource (`this` is not Renderable) | UIShaderSource (`this` is Renderable) | null
        private ShaderProgram _shaderProgram;
        private int _instancingCount;
        private bool _isLoaded;
        private RenderVisibility _visibility;

        /// <summary>Before-rendering event</summary>
        public event RenderingEventHandler? BeforeRendering;
        /// <summary>After-rendering event</summary>
        public event RenderingEventHandler? Rendered;

        /// <summary>Vertex buffer object</summary>
        public ref readonly VBO VBO => ref _vbo;
        /// <summary>Index buffer object</summary>
        public ref readonly IBO IBO => ref _ibo;
        /// <summary>Vertex array object</summary>
        public ref readonly VAO VAO => ref _vao;

        /// <summary>Get whether the <see cref="Renderable"/> is loaded and ready to be rendered.</summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>Get or set visibility in rendering.</summary>
        public RenderVisibility Visibility { get => _visibility; set => _visibility = value; }

        /// <summary>Get or set a shader source</summary>
        public ShaderSource? Shader
        {
            get => SafeCast.As<ShaderSource>(_shader);
            set
            {
                if(_isLoaded) { ThrowAlreadyLoaded(); }
                _shader = value;
            }
        }

        internal IShaderSource? ShaderInternal
        {
            get => _shader;
            set
            {
                if(_isLoaded) { ThrowAlreadyLoaded(); }
                _shader = value;
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

        public ref readonly ShaderProgram ShaderProgram => ref _shaderProgram;

        public Renderable()
        {
        }

        /// <summary>Render the <see cref="Renderable"/>.</summary>
        /// <param name="modelParent">parent model matrix</param>
        /// <param name="view">view matrix</param>
        /// <param name="projection">projection matrix</param>
        internal void Render(in Matrix4 modelParent, in Matrix4 view, in Matrix4 projection)
        {
            var visibility = _visibility;
            if(IsLoaded && visibility == RenderVisibility.Visible || visibility == RenderVisibility.InvisibleSelf) {
                var withoutScale = modelParent * Position.ToTranslationMatrix4() * Rotation.ToMatrix4();
                if(visibility == RenderVisibility.Visible) {
                    var model = withoutScale * Scale.ToScaleMatrix4();
                    BeforeRendering?.Invoke(this, in model, in view, in projection);
                    OnRendering(in model, in view, in projection);
                    Rendered?.Invoke(this, in model, in view, in projection);
                }
                if(HasChild) {
                    foreach(var child in Children.AsSpan()) {
                        if(child is Renderable renderable) {
                            renderable.Render(withoutScale, view, projection);
                        }
                    }
                }
            }
        }

        protected virtual void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            _shaderProgram.Apply(in model, in view, in projection);
            DrawElements(0, IBO.Length);
            VAO.Unbind();
            IBO.Unbind();
        }

        protected void DrawElements(int byteOffset, int count)
        {
            if(_instancingCount == 0) {
                GL.DrawElements(BeginMode.Triangles, count, DrawElementsType.UnsignedInt, byteOffset);
            }
            else {
                GL.DrawElementsInstanced(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (IntPtr)byteOffset, _instancingCount);
            }
        }

        /// <summary>Load mesh data</summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertex data to load</param>
        /// <param name="indices">index data to load</param>
        protected void LoadMesh<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            if(TryGetHostScreen(out var screen) == false) {
                throw new InvalidOperationException();
            }
            if(Engine.CurrentContext != screen) {
                throw new InvalidOperationException("Invalid current context.");
            }
            if(IsLoaded) { ThrowAlreadyLoaded(); }

            var lifeState = LifeState;
            if(lifeState.Is(LifeState.New) || lifeState.IsSameOrAfter(LifeState.Terminated)) {
                return;
            }

            var isUIRenderable = this is UIRenderable;

            if(_shader is null) {
                if(isUIRenderable) {
                    Debug.Assert(typeof(TVertex) == typeof(VertexSlim));
                    _shader = DefaultUIShader.Instance;
                }
                else {
                    if(VertexMarshalHelper.HasSpecialField(typeof(TVertex), VertexSpecialField.Normal)) {
                        _shader = PhongShader.Instance;
                    }
                    else {
                        _shader = EmptyShader.Instance;
                    }
                }
            }

            _vbo = VBO.Create();
            VBO.BindBufferData(ref _vbo, vertices, BufferUsageHint.StaticDraw);
            _ibo = IBO.Create();
            IBO.BindBufferData(ref _ibo, indices, BufferUsageHint.StaticDraw);
            _vao = VAO.Create();
            VAO.Bind(_vao);
            Debug.Assert(_shaderProgram.IsEmpty);
            _shaderProgram = _shader.Compile(this);

            var vertexType = typeof(TVertex);
            if(isUIRenderable) {
                _shaderProgram.InitializeForUI();
            }
            else {
                _shaderProgram.Initialize(vertexType);
            }
            _isLoaded = true;
        }

        protected override void OnDead()
        {
            var isLoaded = IsLoaded;
            _isLoaded = false;
            base.OnDead();
            _shaderProgram.Release();
            _shaderProgram = ShaderProgram.Empty;
            if(isLoaded) {
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
        }

        [DoesNotReturn]
        private static void ThrowAlreadyLoaded() => throw new InvalidOperationException("already loaded");
    }

    public delegate void RenderingEventHandler(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

    public enum RenderVisibility : byte
    {
        Visible = 0,
        InvisibleSelf = 1,
        InvisibleHierarchical = 2,
    }
}
