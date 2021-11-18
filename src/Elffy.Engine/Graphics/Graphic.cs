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

        public static void BlitDepthBuffer(RectI srcRect, RectI destRect)
        {
            GL.BlitFramebuffer(srcRect.X, srcRect.Y, srcRect.X + srcRect.Width, srcRect.Y + srcRect.Height,
                               destRect.X, destRect.Y, destRect.X + destRect.Width, destRect.Y + destRect.Height,
                               ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        }
    }
}
