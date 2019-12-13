#nullable enable
using System;
using System.IO;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy
{
    /// <summary>GLSL のフラグメントシェーダーを表すクラス</summary>
    public sealed class FragmentShader : IDisposable
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

        private FragmentShader()
        {
        }

        public static FragmentShader LoadFromResource(string resource)
        {
            var shader = new FragmentShader();
            string shaderSource;
            using(var stream = Resources.GetStream(resource))
            using(var reader = new StreamReader(stream)) {
                shaderSource = reader.ReadToEnd();
            }
            shader.Compile(shaderSource);
            return shader;
        }

        /// <summary>GLSL のフラグメントシェーダーのソースコードを指定してこの <see cref="FragmentShader"/> をコンパイルします</summary>
        /// <param name="shaderSource">GLSL のフラグメントシェーダーのソースコード</param>
        private void Compile(string shaderSource)
        {
            ThrowIfDisposed();
            Game.Dispatcher.ThrowIfNotMainThread();
            ShaderID = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(ShaderID, shaderSource);
            GL.CompileShader(ShaderID);
            GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == COMPILE_FAILED) {
                throw new ArgumentException("Compiling fragment shader is Failed");
            }
        }

        ~FragmentShader() => Dispose(false);

        #region Dispose pattern
        /// <summary>このフラグメントシェーダーを破棄します</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>このフラグメントシェーダーを破棄します</summary>
        /// <param name="disposing"><see cref="Dispose"/> からの呼び出しかどうか</param>
        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }
                // Release unmanaged resource here.
                Game.Dispatcher.Invoke(() =>
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
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ShaderProgram)); }
        }
    }
}
