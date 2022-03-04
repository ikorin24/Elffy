#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Shading;
using Elffy.Shading.Forward;
using Elffy.Graphics.OpenGL;
using Elffy.UI;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>Base class that is rendered on the screen.</summary>
    public abstract class Renderable : Positionable
    {
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;
        private RendererData _rendererData;
        private int _instancingCount;
        private bool _isLoaded;
        private RenderVisibility _visibility;
        private Type? _vertexType;

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

        public Type? VertexType => _vertexType;

        /// <summary>Get whether the <see cref="Renderable"/> is loaded and ready to be rendered.</summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>Get or set visibility in rendering.</summary>
        public RenderVisibility Visibility { get => _visibility; set => _visibility = value; }

        /// <summary>Get or set a shader source</summary>
        public RenderingShader? Shader
        {
            get => SafeCast.As<RenderingShader>(_rendererData.Shader);
            set
            {
                if(_isLoaded || _rendererData.IsCompiled) { ThrowAlreadyLoaded(); }
                _rendererData = new RendererData(value);
            }
        }

        internal IRenderingShader? ShaderInternal
        {
            get => _rendererData.Shader;
            set
            {
                if(_isLoaded || _rendererData.IsCompiled) { ThrowAlreadyLoaded(); }
                _rendererData = new RendererData(value);
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

        internal ref readonly RendererData RendererData => ref _rendererData;

        public Renderable() : base(FrameObjectInstanceType.Renderable)
        {
        }

        public RenderingShader GetValidShader()
        {
            Debug.Assert(this is not UIRenderable, $"{nameof(UIRenderable)} should not get here.");
            var shader = Shader;
            if(shader is null) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException("It does not have shader.");
            }
            return shader;
        }

        public unsafe bool TryGetMesh([MaybeNullWhen(false)] out Mesh mesh) => MeshHelper.TryGetMesh(this, out mesh);

        public unsafe bool TryGetMeshRaw(void* vertices, ulong verticesByteSize,
                                         int* indices, ulong indicesByteSize,
                                         [MaybeNullWhen(false)] out Type vertexType,
                                         out ulong verticesByteSizeActual,
                                         out uint indicesByteSizeActual)
        {
            return MeshHelper.TryGetMeshRaw(
                this, vertices, verticesByteSize, indices, indicesByteSize,
                out vertexType, out verticesByteSizeActual, out indicesByteSizeActual);
        }

        public unsafe (int VertexCount, int IndexCount) GetMesh<TVertex>(Span<TVertex> vertices, Span<int> indices) where TVertex : unmanaged
            => MeshHelper.GetMesh(this, vertices, indices);

        public unsafe (ulong VertexCount, uint IndexCount) GetMesh<TVertex>(TVertex* vertices, ulong vertexCount, int* indices, uint indexCount) where TVertex : unmanaged
            => MeshHelper.GetMesh(this, vertices, vertexCount, indices, indexCount);

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
                        if(child.IsRenderable(out var renderable)) {
                            renderable.Render(withoutScale, view, projection);
                        }
                    }
                }
            }
        }

        protected virtual void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            Debug.Assert(this is not UIRenderable, $"{nameof(UIRenderable)} should not get here.");
            var shader = GetValidShader();
            var program = _rendererData.Program;
            if(program.IsEmpty) {
                throw new InvalidOperationException();
            }

            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            ProgramObject.UseProgram(program);
            shader.OnRenderingInternal(program, this, model, view, projection);
            DrawElements(0, IBO.Length);
            VAO.Unbind();
            IBO.Unbind();
        }

        protected void DrawElements(int byteOffset, uint count)
        {
            if(_instancingCount == 0) {
                GL.DrawElements(BeginMode.Triangles, (int)count, DrawElementsType.UnsignedInt, byteOffset);
            }
            else {
                GL.DrawElementsInstanced(PrimitiveType.Triangles, (int)count, DrawElementsType.UnsignedInt, (IntPtr)byteOffset, _instancingCount);
            }
        }

        protected unsafe void LoadMesh<TVertex>(TVertex* vertices, ulong vertexCount, int* indices, uint indexCount) where TVertex : unmanaged
        {
            ContextMismatchException.ThrowIfContextNotEqual(Engine.GetValidCurrentContext(), GetValidScreen());

            if(_isLoaded) { ThrowAlreadyLoaded(); }

            var lifeState = LifeState;
            if(lifeState.Is(LifeState.New) || lifeState.IsSameOrAfter(LifeState.Terminating)) {
                return;
            }

            _vbo = VBO.Create();
            VBO.BindBufferData(ref _vbo, vertices, vertexCount, BufferHint.StaticDraw);
            _ibo = IBO.Create();
            IBO.BindBufferData(ref _ibo, indices, indexCount, BufferHint.StaticDraw);
            _vao = VAO.Create();
            VAO.Bind(_vao);

            Debug.Assert(_rendererData.IsCompiled == false);

            var vertexType = typeof(TVertex);
            if(this is UIRenderable uIRenderable) {
                Debug.Assert(typeof(TVertex) == typeof(VertexSlim));
                var shader = SafeCast.As<UIRenderingShader>(_rendererData.Shader);
                if(shader is null) {
                    shader = DefaultUIShader.Instance;
                    _rendererData = new RendererData(shader);
                }
                if(_rendererData.IsCompiled == false) {
                    RendererData.Compile(ref _rendererData);
                }
                VAO.Bind(_vao);
                VBO.Bind(_vbo);
                shader.DefineLocationInternal(_rendererData.Program, uIRenderable.Control);
                VAO.Unbind();
                VBO.Unbind();
            }
            else {
                var shader = SafeCast.As<RenderingShader>(_rendererData.Shader);
                if(shader is null) {
                    shader = EmptyShader.Instance;
                    _rendererData = new RendererData(shader);
                }
                if(_rendererData.IsCompiled == false) {
                    RendererData.Compile(ref _rendererData);
                }
                VAO.Bind(_vao);
                VBO.Bind(_vbo);
                shader.DefineLocationInternal(_rendererData.Program, this, vertexType);
                VAO.Unbind();
                VBO.Unbind();

            }
            _vertexType = vertexType;
            _isLoaded = true;
        }

        /// <summary>Load mesh data</summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertex data to load</param>
        /// <param name="indices">index data to load</param>
        protected unsafe void LoadMesh<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            fixed(TVertex* v = vertices)
            fixed(int* i = indices) {
                LoadMesh(v, (ulong)vertices.Length, i, (uint)indices.Length);
            }
        }

        protected void LoadMesh(Mesh mesh)
        {
            throw new NotImplementedException();
        }

        protected override void OnDead()
        {
            var isLoaded = IsLoaded;
            _isLoaded = false;
            base.OnDead();
            RendererData.Release(ref _rendererData);
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
