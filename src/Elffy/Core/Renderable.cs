#nullable enable
using System;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Threading;
using Elffy.Exceptions;
using Elffy.Shading;
using Elffy.OpenGL;
using Elffy.Components;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Threading.Tasks;

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
        private TextureObject _to;
        private ShaderSource _shader = ShaderSource.Phong;
        private ShaderProgram? _shaderProgram;

        /// <summary>Vertex Buffer Object</summary>
        public VBO VBO => _vbo;
        /// <summary>Index Buffer Object</summary>
        public IBO IBO => _ibo;
        /// <summary>VAO</summary>
        public VAO VAO => _vao;
        /// <summary>Texture Object</summary>
        public TextureObject TextureObject => _to;

        public bool IsLoaded { get; private set; }

        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        public ShaderSource Shader
        {
            get => _shader;
            set
            {
                if(value is null) { throw new ArgumentNullException(nameof(value)); }
                //Dispatcher.ThrowIfNotMainThread();
                _shader = value;
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
            var withoutScale = modelParent *
                               new Matrix4(1, 0, 0, Position.X,
                                           0, 1, 0, Position.Y,
                                           0, 0, 1, Position.Z,
                                           0, 0, 0, 1) *
                               Rotation.ToMatrix4();
            var model = withoutScale * 
                        new Matrix4(Scale.X, 0, 0, 0,
                                    0, Scale.Y, 0, 0,
                                    0, 0, Scale.Z, 0,
                                    0, 0, 0, 1);

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
            if(!_to.IsEmpty) {
                TextureObject.Bind(_to);
            }
            else {
                TextureObject.Bind(Engine.WhiteEmptyTexture);
            }
            _shaderProgram!.Apply(this, Layer.Lights, in model, in view, in projection);
            GL.DrawElements(BeginMode.Triangles, IBO.Length, DrawElementsType.UnsignedInt, 0);
            VAO.Unbind();
            IBO.Unbind();
            TextureObject.Unbind();
        }

        /// <summary>指定の頂点配列とインデックス配列で VBO, IBO を作成し、VAO を作成します</summary>
        /// <param name="vertices">頂点配列</param>
        /// <param name="indices">インデックス配列</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe void LoadGraphicBuffer(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<int> indices)
        {
            Dispatcher.ThrowIfNotMainThread();
            if(IsLoaded) {
                VBO.BindBufferData(ref _vbo , vertices, BufferUsageHint.StaticDraw);
                IBO.BindBufferData(ref _ibo, indices, BufferUsageHint.StaticDraw);
            }
            else {
                _vbo = VBO.Create();
                VBO.BindBufferData(ref _vbo, vertices, BufferUsageHint.StaticDraw);
                _ibo = IBO.Create();
                IBO.BindBufferData(ref _ibo, indices, BufferUsageHint.StaticDraw);
                _vao = VAO.Create();
                VAO.Bind(_vao);
                IsLoaded = true;
                BeginSetShaderProgram(_shader);
            }
        }

        protected void LoadTexture(Bitmap bitmap)
        {
            Dispatcher.ThrowIfNotMainThread();
            if(_to.IsEmpty) {
                _to = TextureObject.Create();
            }
            TextureObject.Load(_to, bitmap);
        }

        protected override void OnDead()    // TODO: 全体の終了時に呼ばれていない
        {
            base.OnDead();
            _shaderProgram?.Dispose();
            _shaderProgram = null;
            if(IsLoaded) {
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
            if(!_to.IsEmpty) {
                TextureObject.Delete(ref _to);
            }
        }

        private void BeginSetShaderProgram(ShaderSource source)
        {
            source.CompileOrGetCacheAsync()
                .ContinueWith(task => Dispatcher.Invoke(() =>
                {
                    Debug.Assert(VAO.IsEmpty == false);     // TODO: ロード前に Terminate されている可能性もあるので変えないといけない
                    try {
                        var program = task.Result;
                        program.AssociateVAO(VAO);
                        _shaderProgram?.Dispose();
                        _shaderProgram = program;
                    }
                    catch(AggregateException ex) {
                        if(ex.InnerExceptions.Count == 1) {
                            throw ex.InnerExceptions[0];
                        }
                        else {
                            throw ex;
                        }
                    }
                }));
        }
    }

    public delegate void RenderingEventHandler(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection);
}
