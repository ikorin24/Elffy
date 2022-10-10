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
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Base class that is rendered on the screen.</summary>
    public abstract class Renderable : Positionable
    {
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;
        private RendererData _rendererData;
        private RendererData _shadowRendererData;
        private bool _hasShadow;
        private int _instancingCount;
        private bool _isLoaded;
        private bool _isVisible;

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

        public Type? VertexType => _rendererData.VertexType;

        /// <summary>Get whether the <see cref="Renderable"/> is loaded and ready to be rendered.</summary>
        public bool IsLoaded => _isLoaded;

        public bool HasShadow { get => _hasShadow; set => _hasShadow = value; }

        /// <summary>Get or set visibility of itself</summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        /// <summary>Get or set a shader source</summary>
        public RenderingShader? Shader
        {
            get => SafeCast.As<RenderingShader>(ShaderInternal);
            set => ShaderInternal = value;
        }

        internal IRenderingShader? ShaderInternal
        {
            get => _rendererData.Shader;
            set => _rendererData.SetShader(value);
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
            _isVisible = true;
            _hasShadow = true;
            _rendererData = new RendererData(this);
            _shadowRendererData = new RendererData(this);
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

        /// <summary>Get visibility in rendering.</summary>
        /// <returns>visibility</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderVisibility GetVisibility()
        {
            if(_isVisible == false) {
                return RenderVisibility.InvisibleSelf;
            }
            if(Parent == null) {
                return RenderVisibility.Visible;
            }
            return GetVisibilityInTree(this);

            static RenderVisibility GetVisibilityInTree(Renderable self)
            {
                var parent = self.Parent;
                while(true) {
                    if(parent == null) {
                        return RenderVisibility.Visible;
                    }
                    if(parent.IsRenderable(out var c) && c._isVisible == false) {
                        return RenderVisibility.InvisibleHierarchical;
                    }
                    parent = parent.Parent;
                }
            }

        }

        internal sealed override void RenderRecursively(in Matrix4 modelParent, in Matrix4 view, in Matrix4 projection)
        {
            var visibility = GetVisibility();
            if(visibility != RenderVisibility.Visible) {
                // In case of InvisibleSelf or InvisibleHierarchical, all children are InvisibleHierarchical
                return;
            }
            var children = Children.AsSpan();
            var needToRenderSelf = _isLoaded;
            if(needToRenderSelf == false && children.IsEmpty) {
                return;
            }
            var withoutScale = modelParent * Position.ToTranslationMatrix4() * Rotation.ToMatrix4();
            if(needToRenderSelf && EnsureShaderInitialized()) {
                Debug.Assert(_rendererData.State is RendererDataState.Compiled);
                var model = withoutScale * Scale.ToScaleMatrix4();
                var screen = GetValidScreen();
                var layer = GetValidLayer();
                var context = new RenderingContext(screen, layer, this, in model, in view, in projection);
                BeforeRendering?.Invoke(in context);
                OnRendering(in context);
                Rendered?.Invoke(in context);
            }
            foreach(var child in children) {
                child.RenderRecursively(withoutScale, view, projection);
            }

            bool EnsureShaderInitialized()
            {
                if(_rendererData.State == RendererDataState.Compiled) {
                    return true;
                }
                VAO.Bind(_vao);
                VBO.Bind(_vbo);
                try {
                    if(this is UIRenderable ui) {
                        Debug.Assert(_rendererData.VertexType == typeof(VertexSlim));
                        if(_rendererData.Shader is null) {
                            var shader = ControlShaderSelector.GetDefault(ui.Control.GetType());
                            _rendererData.SetShader(shader);
                        }
                        _rendererData.CompileForUI(ui.Control);
                    }
                    else {
                        if(_rendererData.Shader is null) {
                            _rendererData.SetShader(EmptyShader.Instance);
                        }
                        _rendererData.CompileForRenderable(this);
                    }
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw, ignore exceptions in user code.
                    return false;
                }
                finally {
                    VAO.Unbind();
                    VBO.Unbind();
                }
                Debug.Assert(_rendererData.State == RendererDataState.Compiled);
                return true;
            }
        }

        internal sealed override void RenderShadowMapRecursively(in Matrix4 modelParent, in Matrix4 lightViewProjection)
        {
            var visibility = GetVisibility();
            if(visibility != RenderVisibility.Visible) {
                // In case of InvisibleSelf or InvisibleHierarchical, all children are InvisibleHierarchical
                return;
            }
            var children = Children.AsSpan();
            var needToRenderSelf = _isLoaded && _hasShadow;
            if(!needToRenderSelf && children.IsEmpty) {
                return;
            }
            var withoutScale = modelParent * Position.ToTranslationMatrix4() * Rotation.ToMatrix4();
            if(needToRenderSelf && EnsureShadowRendererInitialized()) {
                Debug.Assert(_shadowRendererData.State == RendererDataState.Compiled);
                var program = _shadowRendererData.GetValidProgram();
                var shader = SafeCast.As<RenderShadowMapShader>(_shadowRendererData.GetValidShader());
                var model = withoutScale * Scale.ToScaleMatrix4();
                VAO.Bind(_vao);
                IBO.Bind(_ibo);
                ProgramObject.UseProgram(program);
                shader.DispatchShader(new ShaderDataDispatcher(program), model, lightViewProjection);
                DrawElements(0, IBO.Length);
                VAO.Unbind();
                IBO.Unbind();
            }
            foreach(var child in children) {
                child.RenderShadowMapRecursively(withoutScale, lightViewProjection);
            }

            bool EnsureShadowRendererInitialized()
            {
                if(_shadowRendererData.State == RendererDataState.Compiled) {
                    return true;
                }
                VAO.Bind(_vao);
                VBO.Bind(_vbo);
                try {
                    Debug.Assert(this is not UIRenderable);
                    if(_shadowRendererData.Shader is null) {
                        _shadowRendererData.SetShader(RenderShadowMapShader.Instance);
                    }
                    _shadowRendererData.CompileForShadowMap(this);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw, ignore exceptions in user code.
                    return false;
                }
                finally {
                    VAO.Unbind();
                    VBO.Unbind();
                }
                Debug.Assert(_shadowRendererData.State == RendererDataState.Compiled);
                return true;
            }
        }

        protected virtual void OnRendering(in RenderingContext context)
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
            shader.OnRenderingInternal(program, in context);
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
            if(lifeState == LifeState.New || lifeState >= LifeState.Terminating) {
                return;
            }
            _vao = VAO.Create();
            VAO.Bind(_vao);
            _vbo = VBO.Create();
            VBO.BindBufferData(ref _vbo, vertices, vertexCount, BufferHint.StaticDraw);
            _ibo = IBO.Create();
            IBO.BindBufferData(ref _ibo, indices, indexCount, BufferHint.StaticDraw);
            VAO.Unbind();

            _rendererData.SetVertexType(typeof(TVertex));
            _shadowRendererData.SetVertexType(typeof(TVertex));
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
            _rendererData.Release();
            _shadowRendererData.Release();
            if(isLoaded) {
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
        }

        [DoesNotReturn]
        private static void ThrowAlreadyLoaded() => throw new InvalidOperationException("already loaded");
    }

    public delegate void RenderingEventHandler(in RenderingContext context);

    public enum RenderVisibility : byte
    {
        Visible = 0,
        InvisibleSelf = 1,
        InvisibleHierarchical = 2,
    }
}
