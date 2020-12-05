#nullable enable
using System;
using OpenTK.Graphics.OpenGL4;
using GL4 = OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    public static class ElffyGL
    {
        /// <summary>Call glClear</summary>
        /// <param name="mask">clear buffer mask</param>
        public static void Clear(ClearBufferMask mask)
        {
            GL.Clear((GL4.ClearBufferMask)mask);
        }

        [Flags]
        public enum ClearBufferMask
        {
            /// <summary>GL_NONE</summary>
            None = GL4.ClearBufferMask.None,
            /// <summary>GL_DEPTH_BUFFER_BIT</summary>
            DepthBufferBit = GL4.ClearBufferMask.DepthBufferBit,
            /// <summary>GL_ACCUM_BUFFER_BIT</summary>
            AccumBufferBit = GL4.ClearBufferMask.AccumBufferBit,
            /// <summary>GL_STENCIL_BUFFER_BIT</summary>
            StencilBufferBit = GL4.ClearBufferMask.StencilBufferBit,
            /// <summary>GL_COLOR_BUFFER_BIT</summary>
            ColorBufferBit = GL4.ClearBufferMask.ColorBufferBit,
        }
    }
}
