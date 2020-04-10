#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Elffy.Core;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Shading
{
    public abstract class ShaderSource
    {
        public static PhongShaderSource Phong => PhongShaderSource.Instance;

        /// <summary>派生クラスで定義された、頂点シェーダーのソースコードを取得します</summary>
        /// <returns>頂点シェーダーのソースコード</returns>
        protected abstract string VertexShaderSource();

        /// <summary>派生クラスで定義された、フラグメントシェーダ―のソースコードを取得します</summary>
        /// <returns>フラグメントシェーダ―のソースコード</returns>
        protected abstract string FragmentShaderSource();

        protected abstract void DefineLocation(VertexDefinition definition);

        /// <summary>派生クラスでオーバーライドされた場合、このシェーダーに uniform 変数を送ります。</summary>
        /// <param name="uniform"></param>
        /// <param name="target"></param>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        protected abstract void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        internal void SendUniformsInternal(int program, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
            => SendUniforms(new Uniform(program), target, model, view, projection);

        internal void DefineLocationInternal(int program) => DefineLocation(new VertexDefinition(program));

        /// <summary>頂点シェーダー・フラグメントシェーダ―の読み込み、リンク、プログラムの作成を行います</summary>
        public Shader Compile()
        {
            var vertShader = Consts.NULL;
            var fragShader = Consts.NULL;
            try {
                vertShader = CompileSource(VertexShaderSource(), ShaderType.VertexShader);
                fragShader = CompileSource(FragmentShaderSource(), ShaderType.FragmentShader);

                var program = GL.CreateProgram();
                GL.AttachShader(program, vertShader);
                GL.AttachShader(program, fragShader);
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
                if(linkStatus == Consts.ShaderProgramLinkFailed) {
                    var log = GL.GetProgramInfoLog(program);
                    throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
                }
                return new Shader(this, program);
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
                var log = GL.GetShaderInfoLog(shaderID);
                throw new InvalidDataException($"Compiling shader is Failed.{Environment.NewLine}{log}{Environment.NewLine}{source}");
            }
            return shaderID;
        }
    }
}
