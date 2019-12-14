#nullable enable
using System;
using Elffy.Core;
using Elffy.Threading;
using Elffy.Exceptions;
using OpenTK.Graphics.OpenGL;

namespace Elffy
{
    /// <summary>シェーダープログラムを表すクラス</summary>
    /// <remarks>
    /// シェーダープログラムに関連付けられた各種シェーダー情報は VRAM 側に保持されます。
    /// このシェーダープログラムは、自身に関連付けられた各種シェーダのインスタンスをメモリ上に持ちません。
    /// </remarks>
    public sealed class ShaderProgram : IDisposable
    {
        private const int LINK_FAILED = 0;
        private bool _disposed;
        /// <summary>現在適用されている <see cref="ShaderProgram"/> の識別番号</summary>
        private static int _currentProgramID;
        private bool _isDefaultShader;
        /// <summary>このシェーダープログラムの OpenGL の識別番号</summary>
        private int _programID = Consts.NULL;

        /// <summary>シェーダープログラムを生成します</summary>
        private ShaderProgram()
        {
        }

        /// <summary>デフォルトのシェーダーを取得します</summary>
        public static ShaderProgram Default => (_default ??= CreateDefault());
        private static ShaderProgram? _default;

        ~ShaderProgram() => Dispose(false);

        /// <summary>シェーダープログラムを新しく作成します</summary>
        /// <param name="vertexShader">頂点シェーダー</param>
        /// <param name="fragmentShader">フラグメントシェーダ―</param>
        /// <returns>シェーダープログラム</returns>
        public static ShaderProgram Create(VertexShader vertexShader, FragmentShader fragmentShader)
        {
            Engine.CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ArgumentChecker.ThrowIfNullArg(vertexShader, nameof(vertexShader));
            ArgumentChecker.ThrowIfNullArg(fragmentShader, nameof(fragmentShader));
            var program = new ShaderProgram();
            program.LinkShaders(vertexShader.ShaderID, fragmentShader.ShaderID);
            return program;
        }

        /// <summary>このシェーダープログラムを適用します</summary>
        internal void Apply()
        {
            ThrowIfDisposed();
            if(_currentProgramID != _programID) {
                _currentProgramID = _programID;
                GL.UseProgram(_programID);
            }
        }

        private void LinkShaders(int vertexShaderID, int fragmentShaderID)
        {
            _programID = GL.CreateProgram();
            GL.AttachShader(_programID, vertexShaderID);
            GL.AttachShader(_programID, fragmentShaderID);
            GL.LinkProgram(_programID);
            GL.GetProgram(_programID, GetProgramParameterName.LinkStatus, out int linkStatus);
            if(linkStatus == LINK_FAILED) {
                throw new InvalidOperationException("Linking shader is failed.");
            }
        }

        private static ShaderProgram CreateDefault()
        {
            var program = new ShaderProgram();
            program._isDefaultShader = true;
            return program;
        }

        /// <summary>このインスタンスが既に破棄されている場合例外を投げます</summary>
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ShaderProgram)); }
        }

        #region Dispose pattern
        /// <summary>このシェーダープログラムを破棄します</summary>
        public void Dispose()
        {
            if(_isDefaultShader) { return; }        // デフォルトのインスタンスは破棄させない
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>このシェーダープログラムを破棄します</summary>
        /// <param name="disposing"><see cref="Dispose"/> メソッドからの呼び出しかどうか</param>
        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }
                // release unmanaged resource here
                Engine.CurrentScreen.Dispatcher.Invoke(() =>
                {
                    if(_programID != Consts.NULL) {
                        GL.DeleteProgram(_programID);
                        _programID = Consts.NULL;
                    }
                });
                _disposed = true;
            }
        }
        #endregion
    }
}
