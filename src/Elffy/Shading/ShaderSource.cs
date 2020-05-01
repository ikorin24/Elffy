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

        public static NormalShaderSource Normal => NormalShaderSource.Instance;

        public static VertexColorShaderSource VertexColor => VertexColorShaderSource.Instance;

        /// <summary>派生クラスでオーバーライドされた場合、このシェーダーに渡される頂点属性変数を定義します</summary>
        /// <param name="definition">頂点属性定義用オブジェクト</param>
        protected abstract void DefineLocation(VertexDefinition definition);

        internal void DefineLocation(int program) => DefineLocation(new VertexDefinition(program));

        /// <summary>派生クラスでオーバーライドされた場合、このシェーダーに uniform 変数を送ります。</summary>
        /// <param name="uniform">uniform 変数の送信用オブジェクト</param>
        /// <param name="target">描画対象の <see cref="Renderable"/></param>
        /// <param name="lights">ライト</param>
        /// <param name="model">model 行列</param>
        /// <param name="view">view 行列</param>
        /// <param name="projection">projection 行列</param>
        protected abstract void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        internal void SendUniforms(int program, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
            => SendUniforms(new Uniform(program), target, lights, model, view, projection);

        /// <summary>頂点シェーダー・フラグメントシェーダ―の読み込み、リンク、プログラムの作成を行います</summary>
        public abstract ShaderProgram Compile();

        protected ShaderProgram CompileShaderSources(string vertexShaderSource, string fragmentShaderSource)
        {
            var vertShader = Consts.NULL;
            var fragShader = Consts.NULL;
            try {
                vertShader = CompileSource(vertexShaderSource, ShaderType.VertexShader);
                fragShader = CompileSource(fragmentShaderSource, ShaderType.FragmentShader);

                var program = GL.CreateProgram();
                GL.AttachShader(program, vertShader);
                GL.AttachShader(program, fragShader);
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
                if(linkStatus == Consts.ShaderProgramLinkFailed) {
                    var log = GL.GetProgramInfoLog(program);
                    throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
                }
                return new ShaderProgram(this, program);
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
