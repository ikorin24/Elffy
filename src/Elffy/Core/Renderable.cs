using OpenTK;
using System;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using Elffy.Threading;
using Elffy.Exceptions;

namespace Elffy.Core
{
    /// <summary>
    /// 画面に描画されるオブジェクトの基底クラス<para/>
    /// 描画に関する操作を提供します<para/>
    /// </summary>
    public abstract class Renderable : Positionable, IDisposable
    {
        #region private member
        private int _indexArrayLength;
        /// <summary>VBOバッファ番号</summary>
        private int _vertexBuffer;
        /// <summary>IBO番号</summary>
        private int _indexBuffer;
        /// <summary>VAO</summary>
        private int _vao;
        private bool _disposed;
        private bool _isLoaded;
        #endregion

        #region Property
        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>マテリアルを取得または設定します</summary>
        /// <exception cref="ArgumentNullException"></exception>
        public Material Material
        {
            get => _material;
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                if(_material != value) {
                    _material = value;
                    MaterialAttached?.Invoke(this);
                }
            }
        }
        private Material _material = Material.Default;

        /// <summary>テクスチャ</summary>
        /// <exception cref="ArgumentNullException"></exception>
        public TextureBase Texture
        {
            get => _texture;
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                if(_texture != value) {
                    _texture = value;
                    TextureAttached?.Invoke(this);
                }
            }
        }
        private TextureBase _texture = TextureBase.Empty;

        /// <summary>シェーダー</summary>
        /// <exception cref="ArgumentNullException"></exception>
        public ShaderProgram Shader
        {
            get => _shader;
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                if(_shader != value) {
                    _shader = value;
                    ShaderAttached?.Invoke(this);
                }
            }
        }
        private ShaderProgram _shader = ShaderProgram.Default;

        /// <summary>頂点色を使用するかどうか</summary>
        public bool EnableVertexColor
        {
            get => _enableVertexColor;
            set
            {
                _enableVertexColor = value;
                if(_vao == Consts.NULL) { return; }
                if(_enableVertexColor) {
                    GL.BindVertexArray(_vao);
                    GL.EnableClientState(ArrayCap.ColorArray);
                }
                else {
                    GL.BindVertexArray(_vao);
                    GL.DisableClientState(ArrayCap.ColorArray);
                }
            }
        }
        private bool _enableVertexColor;
        #endregion

        public event ActionEventHandler<Renderable> MaterialAttached;
        public event ActionEventHandler<Renderable> TextureAttached;
        public event ActionEventHandler<Renderable> ShaderAttached;
        public event ActionEventHandler<Renderable> Rendering;
        public event ActionEventHandler<Renderable> Rendered;

        ~Renderable() => Dispose(false);

        #region Render
        /// <summary>このインスタンスを描画します</summary>
        internal void Render()
        {
            Rendering?.Invoke(this);
            // 座標と回転を適用
            GL.Translate(Position);
            var rot = Matrix4.CreateFromQuaternion(Rotation);
            GL.MultMatrix(ref rot);
            GL.Scale(Scale);

            if(_isLoaded) {
                Material.Apply();
                Texture.SwitchBind();
                Shader.Apply();
                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.DrawElements(BeginMode.Triangles, _indexArrayLength, DrawElementsType.UnsignedInt, 0);
            }

            if(HasChild) {
                foreach(var child in Children.OfType<Renderable>()) {
                    GL.PushMatrix();
                    child.Render();
                    GL.PopMatrix();
                }
            }
            Rendered?.Invoke(this);
        }
        #endregion

        #region InitGraphicBuffer
        /// <summary>描画する3Dモデル(頂点データ)をGPUメモリにロードします</summary>
        /// <param name="vertexArray">頂点配列</param>
        /// <param name="indexArray">頂点インデックス配列</param>
        protected void InitGraphicBuffer(Vertex[] vertexArray, int[] indexArray)
        {
            ArgumentChecker.ThrowIfNullArg(vertexArray, nameof(vertexArray));
            ArgumentChecker.ThrowIfNullArg(indexArray, nameof(indexArray));
            Dispatcher.ThrowIfNotMainThread();
            unsafe {
                fixed(Vertex* vertexPtr = vertexArray)
                fixed(int* indexPtr = indexArray) {
                    InitGraphicBufferPrivate((IntPtr)vertexPtr, vertexArray.Length, (IntPtr)indexPtr, indexArray.Length);
                }
            }
        }

        /// <summary>描画する3Dモデル(頂点データ)をGPUメモリにロードします</summary>
        /// <param name="vertexArray">頂点配列</param>
        /// <param name="indexArray">頂点インデックス配列</param>
        protected void InitGraphicBuffer(IntPtr vertexArray, int vertexArrayLength, IntPtr indexArray, int indexArrayLength)
        {
            Dispatcher.ThrowIfNotMainThread();
            InitGraphicBufferPrivate(vertexArray, vertexArrayLength, indexArray, indexArrayLength);
        }
        #endregion

        private void InitGraphicBufferPrivate(IntPtr vertexArray, int vertexArrayLength, IntPtr indexArray, int indexArrayLength)
        {
            _indexArrayLength = indexArrayLength;
            // 頂点バッファ(VBO)生成
            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            int vertexSize = vertexArrayLength * Vertex.Size;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexSize, vertexArray, BufferUsageHint.StaticDraw);

            // 頂点indexバッファ(IBO)生成
            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            int indexSize = indexArrayLength * sizeof(int);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexSize, indexArray, BufferUsageHint.StaticDraw);

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.NormalArray);
            if(EnableVertexColor) { GL.EnableClientState(ArrayCap.ColorArray); }
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            Vertex.GLSetStructLayout();                          // 頂点構造体のレイアウトを指定

            _isLoaded = true;
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }

                // Release unmanaged resource
                if(_isLoaded) {
                    // OpenGLのバッファの削除はメインスレッドで行う必要がある
                    var vbo = _vertexBuffer;
                    var ibo = _indexBuffer;
                    var vao = _vao;
                    Dispatcher.Invoke(() => {
                        GL.DeleteBuffer(vbo);
                        GL.DeleteBuffer(ibo);
                        GL.DeleteVertexArray(vao);
                    });
                }
                _disposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
