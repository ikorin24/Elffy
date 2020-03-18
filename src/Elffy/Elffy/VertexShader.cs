#nullable enable
using System;
using System.IO;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>GLSL の頂点シェーダーを表すクラス</summary>
    public sealed class VertexShader : IDisposable
    {
        private const int COMPILE_FAILED = 0;

        private bool _disposed;

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
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ShaderID = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(ShaderID, shaderSource);
            GL.CompileShader(ShaderID);
            GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == COMPILE_FAILED) {
                throw new InvalidDataException("Compiling vertex shader is Failed");
            }
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
        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }
                // Release unmanaged resource here.
                CurrentScreen.Dispatcher.Invoke(() =>
                {
                    if(ShaderID != Consts.NULL) {
                        GL.DeleteShader(ShaderID);
                        ShaderID = Consts.NULL;
                    }
                });
                _disposed = true;
            }
        }
        #endregion

        /// <summary>このインスタンスが既に破棄されている場合例外を投げます</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ShaderProgram)); }
        }
    }
}
