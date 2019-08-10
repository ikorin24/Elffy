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
        /// <summary>VBOバッファ番号</summary>
        private int _vertexBuffer;
        /// <summary>IBO番号</summary>
        private int _indexBuffer;
        /// <summary>VAO</summary>
        private int _vao;
        /// <summary>頂点配列</summary>
        private Vertex[] _vertexArray;
        /// <summary>頂点番号配列</summary>
        private int[] _indexArray;
        private bool _disposed;
        private bool _isLoaded;
        #endregion

        #region Property
        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;
        /// <summary>マテリアル</summary>
        public Material Material { get; set; }
        /// <summary>テクスチャ</summary>
        public Texture Texture { get; set; }
        #endregion

        ~Renderable() => Dispose(false);

        #region Render
        /// <summary>このインスタンスを描画します</summary>
        internal void Render()
        {
            OnRendering();
            // 座標と回転を適用
            var view = Camera.Current.Matrix;
            GL.LoadMatrix(ref view);
            GL.Translate(Position);
            var rot = Matrix4.CreateFromQuaternion(Rotation);
            GL.MultMatrix(ref rot);
            GL.Scale(Scale);

            // マテリアルの適用
            Material?.Apply();
            // 頂点を描画
            DrawVertexAndTexture();
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
            _vertexArray = vertexArray ?? throw new ArgumentNullException(nameof(vertexArray));
            _indexArray = indexArray ?? throw new ArgumentNullException(nameof(indexArray));
            ThrowIfNotMainThread(nameof(InitGraphicBuffer));

            // 頂点バッファ(VBO)生成
            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            int vertexSize = _vertexArray.Length * Vertex.Size;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexSize, _vertexArray, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, Consts.NULL);

            // 頂点indexバッファ(IBO)生成
            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            int indexSize = _indexArray.Length * sizeof(int);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexSize, _indexArray, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.NormalArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            Vertex.GLSetStructLayout();                          // 頂点構造体のレイアウトを指定
            GL.BindBuffer(BufferTarget.ArrayBuffer, Consts.NULL);
            GL.BindVertexArray(Consts.NULL);

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
                GL.DrawElements(BeginMode.Triangles, _indexArray.Length, DrawElementsType.UnsignedInt, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);
                GL.BindVertexArray(Consts.NULL);
                GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);   // テクスチャのバインド解除
            }
        }
        #endregion

        #region ThrowIfNotMainThread
        private void ThrowIfNotMainThread(string funcName)
        {
            if(GameThread.IsMainThread() == false) { throw new InvalidOperationException($"'{funcName}' must be called from Main Thread."); }
        }
        #endregion

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                    Texture?.Dispose();
                }

                // Release unmanaged resource
                if(_isLoaded) {
                    // OpenGLのバッファの削除はメインスレッドで行う必要がある
                    var vbo = _vertexBuffer;
                    var ibo = _indexBuffer;
                    var vao = _vao;
                    GameThread.Invoke(() => {
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
