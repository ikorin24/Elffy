using System;
using System.IO;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy
{
    /// <summary>GLSL の頂点シェーダーを表すクラス</summary>
    public class VertexShader : IDisposable
    {
        private const int COMPILE_FAILED = 0;

        private bool _disposed;

        #region Property
        /// <summary>この頂点シェーダーがコンパイルされているかどうかを取得します</summary>
        public bool IsCompiled
        {
            get
            {
                ThrowIfDisposed();
                return _isCompiled;
            }
            private set => _isCompiled = value;
        }
        private bool _isCompiled;

        /// <summary>このシェーダーの OpenGL の識別番号</summary>
        internal int ShaderID
        {
            get
            {
                ThrowIfDisposed();
                return _shaderID;
            }
            private set => _shaderID = value;
        }
        private int _shaderID;
        #endregion

        private VertexShader()
        {
        }

        ~VertexShader() => Dispose(false);

        public static VertexShader LoadFromResource(string resource)
        {
            var shader = new VertexShader();
            string shaderSource;
            using(var stream = Resources.GetStream(resource))
            using(var reader = new StreamReader(stream)) {
                shaderSource = reader.ReadToEnd();
            }
            shader.Compile(shaderSource);
            return shader;
        }

        /// <summary>GLSL の頂点シェーダーのソースコードを指定してこの <see cref="VertexShader"/> をコンパイルします</summary>
        /// <param name="shaderSource">GLSL の頂点シェーダーのソースコード</param>
        private void Compile(string shaderSource)
        {
            ThrowIfDisposed();
            Dispatcher.ThrowIfNotMainThread();
            if(IsCompiled) { throw new InvalidOperationException($"{nameof(VertexShader)} is already compiled."); }
            ShaderID = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(ShaderID, shaderSource);
            GL.CompileShader(ShaderID);

            // コンパイル結果のチェック
            GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == COMPILE_FAILED) {
                GL.DeleteShader(ShaderID);
                throw new ArgumentException("Compiling vertex shader is Failed");
            }

            IsCompiled = true;
        }

        #region Dispose pattern
        /// <summary>この頂点シェーダーを破棄します</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>この頂点シェーダーを破棄します</summary>
        /// <param name="disposing"><see cref="Dispose"/> からの呼び出しかどうか</param>
        protected void Dispose(bool disposing)
        {
            if(_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }
                // Release unmanaged resource here.
                if(ShaderID != Consts.NULL) {
                    GL.DeleteShader(ShaderID);
                    ShaderID = Consts.NULL;
                }
                _disposed = true;
            }
        }
        #endregion

        /// <summary>このインスタンスが既に破棄されている場合例外を投げます</summary>
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ShaderProgram)); }
        }
    }
}
