#nullable enable
using System.Diagnostics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Elffy.OpenGL
{
    internal static class GLAssert
    {
        [Conditional("DEBUG")]
        public static unsafe void EnsureContext()
        {
            Debug.Assert(GLFW.GetCurrentContext() != null);
        }
    }
}
