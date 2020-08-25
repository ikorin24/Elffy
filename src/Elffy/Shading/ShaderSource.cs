#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Elffy.Core;
using Elffy.OpenGL;
using OpenToolkit.Graphics.OpenGL4;

namespace Elffy.Shading
{
    public abstract class ShaderSource
    {
        protected abstract string VertexShaderSource { get; }

        protected abstract string FragmentShaderSource { get; }

        /// <summary>派生クラスでオーバーライドされた場合、このシェーダーに渡される頂点属性変数を定義します</summary>
        /// <param name="definition">頂点属性定義用オブジェクト</param>
        /// <param name="target">描画対象の <see cref="Renderable"/></param>
        protected abstract void DefineLocation(VertexDefinition definition, Renderable target);

        internal void DefineLocation(ProgramObject program, Renderable target)
            => DefineLocation(new VertexDefinition(program), target);

        /// <summary>派生クラスでオーバーライドされた場合、このシェーダーに uniform 変数を送ります。</summary>
        /// <param name="uniform">uniform 変数の送信用オブジェクト</param>
        /// <param name="target">描画対象の <see cref="Renderable"/></param>
        /// <param name="lights">ライト</param>
        /// <param name="model">model 行列</param>
        /// <param name="view">view 行列</param>
        /// <param name="projection">projection 行列</param>
        protected abstract void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection);

        internal void SendUniforms(ProgramObject program, Renderable target, ReadOnlySpan<Light> lights,
                                   in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            SendUniforms(new Uniform(program), target, lights, model, view, projection);
        }

        /// <summary>頂点シェーダー・フラグメントシェーダ―の読み込み、リンク、プログラムの作成を行います</summary>
        internal ShaderProgram Compile()
        {
            return new ShaderProgram(this, CompilePrivate);
        }

        //public Task CreateCacheAsync()
        //{
        //    var program = CompilePrivate();
        //    return ShaderPrecompileHelper.CreateCacheFromProgramAsync(GetType(), program);
        //}

        //public async Task<ShaderProgram> CompileOrGetCacheAsync()
        //{
        //    var (program, success) = await ShaderPrecompileHelper.TryLoadProgramCacheAsync(GetType())
        //        .ConfigureAwait(true);
        //    if(success) {
        //        return new ShaderProgram(this, program);
        //    }
        //    else {
        //        return Compile();
        //    }
        //}

        private ProgramObject CompilePrivate()
        {
            var vertShader = Consts.NULL;
            var fragShader = Consts.NULL;
            try {
                vertShader = CompileSource(VertexShaderSource, ShaderType.VertexShader);
                fragShader = CompileSource(FragmentShaderSource, ShaderType.FragmentShader);

                var program = ProgramObject.Create();
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
