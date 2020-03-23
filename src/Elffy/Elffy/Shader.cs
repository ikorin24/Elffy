#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Elffy.Core;
using OpenTK.Graphics.OpenGL;

namespace Elffy
{
    public sealed class Shader : IDisposable
    {
        private static int _currentProgram;
        private int _program = Consts.NULL;
        private bool _disposed;

        private Shader(int program)
        {
            _program = program;
        }

        ~Shader() => Dispose(false);

        public static Shader CreateFromResource(string vertShaderFile, string fragShaderFile)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();

            string vertShaderSource;
            using(var stream = Resources.GetStream(vertShaderFile))
            using(var reader = new StreamReader(stream)) {
                vertShaderSource = reader.ReadToEnd();
            }

            string fragShaderSource;
            using(var stream = Resources.GetStream(fragShaderFile))
            using(var reader = new StreamReader(stream)) {
                fragShaderSource = reader.ReadToEnd();
            }

            var vertShader = Consts.NULL;
            var fragShader = Consts.NULL;
            try {
                vertShader = Compile(vertShaderSource, ShaderType.VertexShader);
                fragShader = Compile(fragShaderSource, ShaderType.FragmentShader);

                var program = GL.CreateProgram();
                GL.AttachShader(program, vertShader);
                GL.AttachShader(program, fragShader);
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
                if(linkStatus == Consts.ShaderProgramLinkFailed) {
                    var log = GL.GetProgramInfoLog(program);
                    throw new InvalidOperationException($"{vertShaderFile}, {fragShaderFile} : Linking shader is failed.{Environment.NewLine}{log}");
                }

                var blockIndexVS = GL.GetUniformBlockIndex(program, "matrix");
                var blockIndexFS = GL.GetUniformBlockIndex(program, "material");
                GL.UniformBlockBinding(program, blockIndexVS, 0);
                GL.UniformBlockBinding(program, blockIndexFS, 1);

                return new Shader(program);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Apply()
        {
            ThrowIfDisposed();
            //if(_currentProgram != _program) {
            //    _currentProgram = _program;
            //    GL.UseProgram(_program);
            //}
            GL.UseProgram(_program);
        }

        public void Dispose()
        {
            if(_program == Consts.NULL) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }
            if(_program == Consts.NULL) { return; }
            CurrentScreen.Dispatcher.Invoke(() =>
            {
                GL.DeleteProgram(_program);
                _program = Consts.NULL;
            });
            _disposed = true;
        }

        private static int Compile(string source, ShaderType shaderType)
        {
            Debug.Assert(shaderType == ShaderType.FragmentShader || shaderType == ShaderType.VertexShader, "Not supported");

            var shaderID = GL.CreateShader(shaderType);
            GL.ShaderSource(shaderID, source);
            GL.CompileShader(shaderID);
            GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == Consts.ShaderCompileFailed) {
                var log = GL.GetShaderInfoLog(shaderID);
                throw new InvalidDataException($"{source} : Compiling shader is Failed.{Environment.NewLine}{log}");
            }
            return shaderID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ShaderProgram)); }
        }
    }
}
