#nullable enable
using System;
using OpenTK.Graphics.OpenGL;
using Elffy.Threading;
using Elffy.Exceptions;
using Elffy.Shading;
using Elffy.OpenGL;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        /// <summary>Vertex Buffer Object</summary>
        public VBO VBO => _vbo;
        /// <summary>Index Buffer Object</summary>
        public IBO IBO => _ibo;
        /// <summary>VAO</summary>
        public VAO VAO => _vao;
        public bool IsLoaded { get; private set; }

        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        protected bool IsEnableRendering { get; set; } = true;

        ///// <summary>マテリアルを取得または設定します</summary>
        //public Material Material
        //{
        //    get => _material;
        //    set
        //    {
        //        if(_material == value) { return; }
        //        var old = _material;
        //        _material = value;
        //        MaterialChanged?.Invoke(this, new ValueChangedEventArgs<Material>(old, value));
        //    }
        //}
        //private Material _material = Material.Default;

        ///// <summary>テクスチャ</summary>
        ///// <exception cref="ArgumentNullException"></exception>
        //public TextureBase Texture
        //{
        //    get => _texture;
        //    set
        //    {
        //        ArgumentChecker.ThrowIfNullArg(value, nameof(value));
        //        if(_texture == value) { return; }
        //        var old = _texture;
        //        _texture = value;
        //        TextureChanged?.Invoke(this, new ValueChangedEventArgs<TextureBase>(old, value));
        //    }
        //}
        //private TextureBase _texture = TextureBase.Empty;

        //private Components.Material? _m;

        //private Components.Texture? _t;

        public ShaderSource Shader
        {
            get => _shader;
            set
            {
                if(value is null) { throw new ArgumentNullException(nameof(value)); }
                if(_shader == value) { return; }
                if(!VBO.IsEmpty) {
                    SetShaderProgram();
                }
                var old = _shader;
                _shader = value;
                ShaderChanged?.Invoke(this, new ValueChangedEventArgs<ShaderSource>(old, value));
            }
        }
        private ShaderSource _shader = ShaderSource.Normal;

        /// <summary>Not null if <see cref="IsLoaded"/> == true</summary>
        protected ShaderProgram? ShaderProgram { get; private set; }

        ///// <summary>Material changed event</summary>
        //public event ActionEventHandler<Renderable, ValueChangedEventArgs<Material>>? MaterialChanged;
        ///// <summary>Texture changed event</summary>
        //public event ActionEventHandler<Renderable, ValueChangedEventArgs<TextureBase>>? TextureChanged;
        ///// <summary>Shader changed event</summary>
        public event ActionEventHandler<Renderable, ValueChangedEventArgs<ShaderSource>>? ShaderChanged;

        /// <summary>Before-rendering event</summary>
        public event RenderingEventHandler? Rendering;
        /// <summary>After-rendering event</summary>
        public event RenderingEventHandler? Rendered;

        public Renderable()
        {
            Terminated += OnTerminated;
        }

        /// <summary>描画を行います</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">view 行列</param>
        /// <param name="modelParent">親の model 行列</param>
        internal unsafe void Render(in Matrix4 projection, in Matrix4 view, in Matrix4 modelParent)
        {
            var model = new Matrix4(Scale.X, 0, 0, 0,
                                    0, Scale.Y, 0, 0,
                                    0, 0, Scale.Z, 0,
                                    0, 0, 0, 1) *
                        Rotation.ToMatrix4() *
                        new Matrix4(1, 0, 0, Position.X,
                                    0, 1, 0, Position.Y,
                                    0, 0, 1, Position.Z,
                                    0, 0, 0, 1) *
                        modelParent;

            if(IsLoaded && IsVisible) {
                VAO.Bind(_vao);
                IBO.Bind(_ibo);
                //Texture.Apply();
                ShaderProgram!.Apply(this, Layer!.Lights, model, view, projection);
                Rendering?.Invoke(this, in model, in view, in projection);
                if(IsEnableRendering) {
                    GL.DrawElements(BeginMode.Triangles, IBO.Length, DrawElementsType.UnsignedInt, 0);
                }
                Rendered?.Invoke(this, in model, in view, in projection);
            }

            if(HasChild) {
                foreach(var child in Children.AsReadOnlySpan()) {
                    if(child is Renderable renderable) {
                        renderable.Render(projection, view, model);
                    }
                }
            }
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
                SetShaderProgram();
            }
        }

        private void OnTerminated(FrameObject _)
        {
            ShaderProgram?.Dispose();
            ShaderProgram = null;
            if(IsLoaded) {
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
        }

        private void SetShaderProgram()
        {
            Debug.Assert(VBO.IsEmpty == false);
            var program = _shader.Compile();
            program.AssociateVBO(VBO);
            ShaderProgram?.Dispose();
            ShaderProgram = program;
        }
    }

    public delegate void RenderingEventHandler(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection);
}
