using System;
using Elffy.Core;
using Elffy.Threading;
using Elffy.Exceptions;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace Elffy
{
    /// <summary>シェーダープログラムを表すクラス</summary>
    /// <remarks>
    /// シェーダープログラムに関連付けられた各種シェーダー情報は VRAM 側に保持されます。
    /// このシェーダープログラムは、自身に関連付けられた各種シェーダのインスタンスをメモリ上に持ちません。
    /// そのため、各種シェーダーが関連付けられているかどうかの API のみを提供します。
    /// </remarks>
    public class ShaderProgram : IDisposable
    {
        private const int LINK_FAILED = 0;
        private bool _disposed;
        /// <summary>現在適用されている <see cref="ShaderProgram"/> の識別番号</summary>
        private static int _currentProgramID;

        #region Property
        /// <summary>このシェーダープログラムの OpenGL の識別番号を取得します</summary>
        internal int ProgramID
        {
            get
            {
                ThrowIfDisposed();
                return _programID;
            }
            private set => _programID = value;
        }
        private int _programID;

        /// <summary>シェーダーがリンクされているかどうかを取得します</summary>
        public bool IsShaderLinked
        {
            get
            {
                ThrowIfDisposed();
                return _isShaderLinked;
            }
            private set => _isShaderLinked = value;
        }
        private bool _isShaderLinked;

        /// <summary>このシェーダープログラムに頂点シェーダーが関連付けられているかどうかを取得します</summary>
        public bool HasVertexShader
        {
            get
            {
                ThrowIfDisposed();
                return _hasVertexShader;
            }
            private set => _hasVertexShader = value;
        }
        private bool _hasVertexShader;

        /// <summary>このシェーダープログラムにフラグメントシェーダーが関連付けられているかどうかを取得します</summary>
        public bool HasFragmentShader
        {
            get
            {
                ThrowIfDisposed();
                return _hasFragmentShader;
            }
            set => _hasFragmentShader = value;
        }
        private bool _hasFragmentShader;
        #endregion

        /// <summary>シェーダープログラムを生成します</summary>
        private ShaderProgram()
        {
        }

        ~ShaderProgram() => Dispose(false);

        #region public Method
        /// <summary>シェーダープログラムを新しく作成します</summary>
        /// <returns>シェーダープログラム</returns>
        public static ShaderProgram Create()
        {
            Dispatcher.ThrowIfNotMainThread();
            var program = new ShaderProgram();
            return program;
        }

        /// <summary>
        /// このシェーダープログラムに頂点シェーダーとフラグメントシェーダーを関連付けます<para/>
        /// シェーダーを関連付けた後は、シェーダーオブジェクトを破棄しても問題ありません<para/>
        /// </summary>
        /// <param name="vertexShader">頂点シェーダー</param>
        /// <param name="fragmentShader">フラグメントシェーダー</param>
        public void Link(VertexShader vertexShader, FragmentShader fragmentShader)
        {
            ThrowIfDisposed();
            Dispatcher.ThrowIfNotMainThread();
            ArgumentChecker.ThrowIfNullArg(vertexShader, nameof(vertexShader));
            ArgumentChecker.ThrowIfNullArg(fragmentShader, nameof(fragmentShader));
            ArgumentChecker.ThrowIf(vertexShader.IsCompiled == false, new ArgumentException($"{nameof(vertexShader)} is not compiled."));
            ArgumentChecker.ThrowIf(fragmentShader.IsCompiled == false, new ArgumentException($"{nameof(fragmentShader)} is not compiled."));
            if(IsShaderLinked) { throw new InvalidOperationException("Shaders are already linked."); }
            LinkShaders(vertexShader.ShaderID, fragmentShader.ShaderID);
        }

        /// <summary>
        /// このシェーダープログラムに頂点シェーダーを関連付けます<para/>
        /// シェーダーを関連付けた後は、シェーダーオブジェクトを破棄しても問題ありません<para/>
        /// </summary>
        /// <param name="vertexShader">頂点シェーダー</param>
        public void Link(VertexShader vertexShader)
        {
            ThrowIfDisposed();
            Dispatcher.ThrowIfNotMainThread();
            ArgumentChecker.ThrowIfNullArg(vertexShader, nameof(vertexShader));
            ArgumentChecker.ThrowIf(vertexShader.IsCompiled == false, new ArgumentException($"{nameof(vertexShader)} is not compiled."));
            if(IsShaderLinked) { throw new InvalidOperationException("Shaders are already linked."); }
            LinkShaders(vertexShader.ShaderID, null);
        }

        /// <summary>
        /// このシェーダープログラムにフラグメントシェーダーを関連付けます<para/>
        /// シェーダーを関連付けた後は、シェーダーオブジェクトを破棄しても問題ありません<para/>
        /// </summary>
        /// <param name="fragmentShader">フラグメントシェーダー</param>
        public void Link(FragmentShader fragmentShader)
        {
            ThrowIfDisposed();
            Dispatcher.ThrowIfNotMainThread();
            ArgumentChecker.ThrowIfNullArg(fragmentShader, nameof(fragmentShader));
            ArgumentChecker.ThrowIf(fragmentShader.IsCompiled == false, new ArgumentException($"{nameof(fragmentShader)} is not compiled."));
            if(IsShaderLinked) { throw new InvalidOperationException("Shaders are already linked."); }
            LinkShaders(fragmentShader.ShaderID, null);
        }

        /// <summary>このシェーダープログラムを適用します</summary>
        internal void Apply()
        {
            ThrowIfDisposed();
            if(IsShaderLinked == false) { throw new InvalidOperationException("Shader program is not linked."); }
            if(_currentProgramID != ProgramID) {
                _currentProgramID = ProgramID;
                GL.UseProgram(ProgramID);
            }
        }

        /// <summary>現在 OpenGL に適用されているシェーダープログラムをクリアします</summary>
        internal static void Clear()
        {
            if(_currentProgramID != Consts.NULL) {
                _currentProgramID = Consts.NULL;
                GL.UseProgram(Consts.NULL);
            }
        }
        #endregion

        #region private Method
        private void LinkShaders(int? vertexShaderID, int? fragmentShaderID)
        {
            Debug.Assert(!(vertexShaderID == null && fragmentShaderID == null));        // 両方 null はありえない
            ProgramID = GL.CreateProgram();
            if(vertexShaderID != null) {
                HasVertexShader = true;
                GL.AttachShader(ProgramID, vertexShaderID.Value);
            }
            if(fragmentShaderID != null) {
                HasFragmentShader = true;
                GL.AttachShader(ProgramID, fragmentShaderID.Value);
            }
            // シェーダーのリンク
            GL.LinkProgram(ProgramID);

            // リンク結果のチェック
            GL.GetProgram(ProgramID, GetProgramParameterName.LinkStatus, out int linkStatus);
            if(linkStatus == LINK_FAILED) {
                throw new ArgumentException("Linking shader is failed.");
            }
            IsShaderLinked = true;
        }

        /// <summary>このインスタンスが既に破棄されている場合例外を投げます</summary>
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ShaderProgram)); }
        }
        #endregion

        #region Dispose pattern
        /// <summary>このシェーダープログラムを破棄します</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>このシェーダープログラムを破棄します</summary>
        /// <param name="disposing"><see cref="Dispose"/> メソッドからの呼び出しかどうか</param>
        protected void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }
                // release unmanaged resource here
                GL.DeleteProgram(ProgramID);
                ProgramID = Consts.NULL;
                _disposed = true;
            }
        }
        #endregion
    }
}
