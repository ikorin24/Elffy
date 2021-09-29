#nullable enable
using System;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
{
    public static class ElffyGL
    {
        /// <summary>Call glClear</summary>
        /// <param name="mask">clear buffer mask</param>
        public static void Clear(ClearMask mask)
        {
            GL.Clear(mask.Compat());
        }
    }
}
