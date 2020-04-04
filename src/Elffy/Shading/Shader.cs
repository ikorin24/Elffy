#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Elffy.Core;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class Shader : IDisposable
    {
        private int _program = Consts.NULL;

        private bool IsEmpty => _program == Consts.NULL;
        private bool IsDefault => _program == Default._program;

        private static int _currentProgram = Consts.NULL;

        private static readonly Lazy<DefaultShader> _default = new Lazy<DefaultShader>(() => new DefaultShader(), LazyThreadSafetyMode.ExecutionAndPublication);
        
        /// <summary>Get default shader</summary>
        public static Shader Default => _default.Value;

        public Shader()
        {
            CurrentScreen.Dispatcher.Invoke(LoadShader);
        }

        ~Shader() => Dispose(false);

        protected abstract void SendUniforms(in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        /// <summary>
        /// 頂点シェーダーのソースコードを取得します<para/>
        /// [NOTE] このメソッドは、派生クラスのコンストラクタの実行前に呼ばれる可能性があります。<para/>
        /// </summary>
        /// <returns>頂点シェーダーのソースコード</returns>
        protected abstract string VertexShaderSource();

        /// <summary>
        /// フラグメントシェーダ―のソースコードを取得します<para/>
        /// [NOTE] このメソッドは、派生クラスのコンストラクタの実行前に呼ばれる可能性があります。<para/>
        /// </summary>
        /// <returns>フラグメントシェーダ―のソースコード</returns>
        protected abstract string FragmentShaderSource();

        protected void SendUniformNoCheck(string name, float value) => GL.ProgramUniform1(_program, GL.GetUniformLocation(_program, name), value);

        protected void SendUniformNoCheck(string name, int value) => GL.ProgramUniform1(_program, GL.GetUniformLocation(_program, name), value);

        protected void SendUniformNoCheck(string name, in Vector2 value)
            => GL.ProgramUniform2(_program, GL.GetUniformLocation(_program, name), 1, ref Unsafe.As<Vector2, float>(ref Unsafe.AsRef(value)));

        protected void SendUniformNoCheck(string name, in Vector3 value)
            => GL.ProgramUniform3(_program, GL.GetUniformLocation(_program, name), 1, ref Unsafe.As<Vector3, float>(ref Unsafe.AsRef(value)));

        protected void SendUniformNoCheck(string name, in Vector4 value)
            => GL.ProgramUniform4(_program, GL.GetUniformLocation(_program, name), 1, ref Unsafe.As<Vector4, float>(ref Unsafe.AsRef(value)));

        protected void SendUniformNoCheck(string name, in Color4 value)
            => GL.ProgramUniform4(_program, GL.GetUniformLocation(_program, name), 1, ref Unsafe.As<Color4, float>(ref Unsafe.AsRef(value)));

        protected void SendUniformNoCheck(string name, in Matrix4 value)
            => GL.ProgramUniformMatrix4(_program, GL.GetUniformLocation(_program, name), 1, false, ref Unsafe.As<Matrix4, float>(ref Unsafe.AsRef(value)));

        /// <summary>Apply this shader to current VAO.</summary>
        /// <param name="model">model matrix</param>
        /// <param name="view">view matrix</param>
        /// <param name="projection">projection matrix</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Apply(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            ThrowIfEmptyProgram();
            if(_currentProgram != _program) {
                _currentProgram = _program;
                GL.UseProgram(_program);
            }
            GL.UseProgram(_program);
            SendUniforms(model, view, projection);
        }

        /// <summary>頂点シェーダー・フラグメントシェーダ―の読み込み、リンク、プログラムの作成を行います</summary>
        private void LoadShader()
        {
            var vertShader = Consts.NULL;
            var fragShader = Consts.NULL;
            try {
                vertShader = Compile(VertexShaderSource(), ShaderType.VertexShader);
                fragShader = Compile(FragmentShaderSource(), ShaderType.FragmentShader);

                var program = GL.CreateProgram();
                GL.AttachShader(program, vertShader);
                GL.AttachShader(program, fragShader);
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
                if(linkStatus == Consts.ShaderProgramLinkFailed) {
                    var log = GL.GetProgramInfoLog(program);
                    throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
                }
                _program = program;
            }
            finally {
                if(vertShader != Consts.NULL) {
                    GL.DeleteShader(vertShader);
                }
                if(fragShader != Consts.NULL) {
                    GL.DeleteShader(fragShader);
                }
            }
        }

        public void Dispose()
        {
            if(IsEmpty) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing && IsDefault) { return; };         // defaultシェーダーは破棄しない (ファイナライザ経由の時のみ破棄)
            CurrentScreen.Dispatcher.Invoke(DeleteProgram);
        }

        private void DeleteProgram()
        {
            if(IsEmpty) { return; }
            GL.DeleteProgram(_program);
            _program = Consts.NULL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfEmptyProgram()
        {
            if(IsEmpty) { throw new InvalidOperationException("this shader program is empty or deleted."); }
        }

        /// <summary>Compile specified type of shader source</summary>
        /// <param name="source">shader source code</param>
        /// <param name="shaderType">shader type (must be <see cref="ShaderType.VertexShader"/> or <see cref="ShaderType.FragmentShader"/>.)</param>
        /// <returns>shader program id</returns>
        private static int Compile(string source, ShaderType shaderType)
        {
            Debug.Assert(shaderType == ShaderType.FragmentShader || shaderType == ShaderType.VertexShader, "Not supported");

            var shaderID = GL.CreateShader(shaderType);
            GL.ShaderSource(shaderID, source);
            GL.CompileShader(shaderID);
            GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == Consts.ShaderCompileFailed) {
                var log = GL.GetShaderInfoLog(shaderID);
                throw new InvalidDataException($"Compiling shader is Failed.{Environment.NewLine}{log}{Environment.NewLine}{source}");
            }
            return shaderID;
        }
    }
}
