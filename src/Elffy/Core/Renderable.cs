using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using Elffy.Threading;

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
        /// <summary>マテリアル</summary>
        public Material Material
        {
            get => _material;
            set
            {
                if(value != null && _material != value) {
                    MaterialAttached?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private Material _material;
        /// <summary>テクスチャ</summary>
        public TextureBase Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                if(value != null && _texture != value) {
                    TextureAttached?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private TextureBase _texture;
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

        public event EventHandler MaterialAttached;
        public event EventHandler TextureAttached;

        ~Renderable() => Dispose(false);

        #region Render
        /// <summary>このインスタンスを描画します</summary>
        internal void Render()
        {
            OnRendering();
            // 座標と回転を適用
            GL.Translate(Position);
            var rot = Matrix4.CreateFromQuaternion(Rotation);
            GL.MultMatrix(ref rot);
            GL.Scale(Scale);

            // マテリアルの適用
            if(Material != null) {
                Material.Apply();
            }
            else {
                Material.Default.Apply();
            }
            // 頂点を描画
            DrawVertexAndTexture();
            if(HasChild) {
                foreach(var child in Children.OfType<Renderable>()) {
                    GL.PushMatrix();
                    child.Render();
                    GL.PopMatrix();
                }
            }
            OnRendered();
        }
        #endregion

        /// <summary>派生クラスでoverrideされると、このインスタンスの描画前に実行されます。overrideされない場合、何もしません。</summary>
        protected virtual void OnRendering() { }

        /// <summary>派生クラスでoverrideされると、このインスタンスの描画後に実行されます。overrideされない場合、何もしません。</summary>
        protected virtual void OnRendered() { }

        #region InitGraphicBuffer
        /// <summary>描画する3Dモデル(頂点データ)をGPUメモリにロードします</summary>
        /// <param name="vertexArray">頂点配列</param>
        /// <param name="indexArray">頂点インデックス配列</param>
        protected void InitGraphicBuffer(Vertex[] vertexArray, int[] indexArray)
        {
            if(vertexArray == null) { throw new ArgumentNullException(nameof(vertexArray)); }
            if(indexArray == null) { throw new ArgumentNullException(nameof(indexArray)); }
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

        #region InitGraphicBufferPrivate
        private void InitGraphicBufferPrivate(IntPtr vertexArray, int vertexArrayLength, IntPtr indexArray, int indexArrayLength)
        {
            _indexArrayLength = indexArrayLength;
            // 頂点バッファ(VBO)生成
            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            int vertexSize = vertexArrayLength * Vertex.Size;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexSize, vertexArray, BufferUsageHint.StaticDraw);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, Consts.NULL);

            // 頂点indexバッファ(IBO)生成
            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            int indexSize = indexArrayLength * sizeof(int);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexSize, indexArray, BufferUsageHint.StaticDraw);
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.NormalArray);
            if(EnableVertexColor) { GL.EnableClientState(ArrayCap.ColorArray); }
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            Vertex.GLSetStructLayout();                          // 頂点構造体のレイアウトを指定
            //GL.BindBuffer(BufferTarget.ArrayBuffer, Consts.NULL);
            //GL.BindVertexArray(Consts.NULL);

            _isLoaded = true;
        }
        #endregion

        #region DrawVertexAndTexture
        /// <summary>Vertex および Textureを描画します</summary>
        private void DrawVertexAndTexture()
        {
            if(_isLoaded) {
                if(Texture != null) {
                    Texture.SwitchBind();                     // GLのテクスチャをこのテクスチャに切り替え
                }
                else {
                    GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);
                }
                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.DrawElements(BeginMode.Triangles, _indexArrayLength, DrawElementsType.UnsignedInt, 0);
                //GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);
                //GL.BindVertexArray(Consts.NULL);
                //GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);   // テクスチャのバインド解除
            }
        }
        #endregion

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                    (Texture as IDisposable)?.Dispose();
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
