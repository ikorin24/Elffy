#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics
{
    public static class Graphic
    {
        public static void BlitDepthBuffer(Vector2i size)
        {
            GL.BlitFramebuffer(0, 0, size.X, size.Y, 0, 0, size.X, size.Y, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        }
    }
}
