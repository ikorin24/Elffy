using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace Elffy.Core
{
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
        #endregion private member

        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>マテリアル</summary>
        public Material Material { get; set; }

        static Renderable()
        {
            // 法線の正規化
            GL.Enable(EnableCap.Normalize);
        }

        ~Renderable() => Dispose(false);

        #region Render
        internal void Render()
        {
            // 座標を適用
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref _modelView);

            // マテリアルの適用
            Material?.Apply();

            // 頂点を描画
            Draw();
        }
        #endregion

        #region Load
        protected void Load(Vertex[] vertexArray, int[] indexArray)
        {
            if(vertexArray == null) { throw new ArgumentNullException(nameof(vertexArray)); }
            if(indexArray == null) { throw new ArgumentNullException(nameof(indexArray)); }
            _vertexArray = vertexArray;
            _indexArray = indexArray;

            // 頂点バッファ(VBO)生成
            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            int vertexSize = _vertexArray.Length * Vertex.Size;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexSize, _vertexArray, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // 頂点indexバッファ(IBO)生成
            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            int indexSize = _indexArray.Length * sizeof(int);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexSize, _indexArray, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.NormalArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            Vertex.GLSetPointer();                          // 頂点構造体のレイアウトを指定
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            _isLoaded = true;
        }
        #endregion

        #region Draw
        private void Draw()
        {
            if(_isLoaded) {
                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.DrawElements(BeginMode.Triangles, _indexArray.Length, DrawElementsType.UnsignedInt, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                GL.BindVertexArray(0);
            }
        }
        #endregion

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }

                // Release unmanaged resource
                if(_isLoaded) {
                    GL.DeleteBuffer(_vertexBuffer);
                    GL.DeleteBuffer(_indexBuffer);
                    GL.DeleteVertexArray(_vao);
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
