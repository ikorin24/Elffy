#nullable enable
using OpenTK.Graphics.OpenGL4;
using System.Runtime.CompilerServices;

namespace Elffy.Shading
{
    /// <summary>Required: OpenGL 4.3</summary>
    public interface IComputeShader
    {
        protected void SendUniforms(Uniform uniform, ComputeShaderContext context);

        protected string GetShaderSource();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetShaderSourceInternal(IComputeShader shader)
        {
            return shader.GetShaderSource();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SendUniformsInternal(IComputeShader shader, Uniform uniform, ComputeShaderContext context)
        {
            shader.SendUniforms(uniform, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DispatchCompute(int xGroupCount, int yGroupCount, int zGroupCount)
        {
            GL.DispatchCompute(xGroupCount, yGroupCount, zGroupCount);
        }
    }

    public static class ComputeShaderExtensions
    {
        public static ComputeShaderDispatcher CreateDispatcher(this IComputeShader shader)
        {
            return ComputeShaderDispatcher.Create(shader);
        }
    }
}
