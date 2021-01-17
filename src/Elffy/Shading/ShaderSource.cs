#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Elffy.Core;
using Elffy.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    public abstract class ShaderSource : IShaderSource
    {
        // [NOTE]
        // ShaderSource don't have any opengl resources. (e.g. ProgramObject)
        // Keep it thread-independent and context-free.

        private int _sourceHashCache;

        public abstract string VertexShaderSource { get; }

        public abstract string FragmentShaderSource { get; }

        protected abstract void DefineLocation(VertexDefinition definition, Renderable target);

        protected abstract void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(ProgramObject program, Renderable target)
        {
            DefineLocation(new VertexDefinition(program), target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DefineLocationInternal(VertexDefinition definition, Renderable target)
        {
            DefineLocation(definition, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniformsInternal(ProgramObject program, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            SendUniforms(new Uniform(program), target, model, view, projection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniformsInternal(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            SendUniforms(uniform, target, model, view, projection);
        }

        ShaderProgram IShaderSource.Compile() => Compile();

        internal ShaderProgram Compile()
        {
            return ShaderProgram.Create(this);
        }

        int IShaderSource.GetSourceHash() => GetSourceHash();

        internal int GetSourceHash()
        {
            if(_sourceHashCache == 0) {
                _sourceHashCache = HashCode.Combine(VertexShaderSource, FragmentShaderSource);
            }
            return _sourceHashCache;
        }

        internal static ProgramObject CompileToProgramObject(string vertSource, string fragSource)
        {
            var vertShader = Consts.NULL;
            var fragShader = Consts.NULL;
            var program = ProgramObject.Empty;
            try {
                vertShader = CompileSource(vertSource, ShaderType.VertexShader);
                fragShader = CompileSource(fragSource, ShaderType.FragmentShader);

                program = ProgramObject.Create();
                GL.AttachShader(program.Value, vertShader);
                GL.AttachShader(program.Value, fragShader);
                GL.LinkProgram(program.Value);
                GL.GetProgram(program.Value, GetProgramParameterName.LinkStatus, out int linkStatus);

                if(linkStatus == Consts.ShaderProgramLinkFailed) {
                    var log = GL.GetProgramInfoLog(program.Value);
                    throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
                }
                return program;
            }
            catch {
                ProgramObject.Delete(ref program);
                throw;
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


        /// <summary>Compile specified type of shader source</summary>
        /// <param name="source">shader source code</param>
        /// <param name="shaderType">shader type (must be <see cref="ShaderType.VertexShader"/> or <see cref="ShaderType.FragmentShader"/>.)</param>
        /// <returns>shader program id</returns>
        private static int CompileSource(string source, ShaderType shaderType)
        {
            Debug.Assert(shaderType == ShaderType.FragmentShader || shaderType == ShaderType.VertexShader, "Not supported");

            var shaderID = GL.CreateShader(shaderType);
            GL.ShaderSource(shaderID, source);
            GL.CompileShader(shaderID);
            GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == Consts.ShaderCompileFailed) {
                throw ShaderCompileFailure(shaderID, source);
            }
            return shaderID;

            static InvalidDataException ShaderCompileFailure(int shaderID, string source)
            {
                var log = GL.GetShaderInfoLog(shaderID);
                using var sb = ZString.CreateStringBuilder();
                sb.AppendLine("Compiling shader is Failed.");
                sb.AppendLine(log);
                var lines = source.Split('\n');
                for(int l = 0; l < lines.Length; l++) {
                    sb.Append(string.Format("{0, 3}\t", l));
                    sb.Append(lines[l]);
                    sb.Append('\n');
                }
                return new InvalidDataException(sb.ToString());
            }
        }
    }
}
