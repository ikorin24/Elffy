#nullable enable
using Elffy.Graphics.OpenGL;
using Elffy.Imaging;
using Elffy.Features.Implementation;
using OpenTK.Graphics.OpenGL4;

namespace Elffy;

public static class TextureHelpers
{
    public static Vector2i GetSize(TextureObject to) => GetSize(to, 0);

    public static Vector2i GetSize(TextureObject to, int level)
    {
        GL.GetTextureLevelParameter(to.Value, level, GetTextureParameter.TextureWidth, out int width);
        GL.GetTextureLevelParameter(to.Value, level, GetTextureParameter.TextureHeight, out int height);
        return new Vector2i(width, height);
    }

    public static unsafe Image GetImage(TextureObject to) => GetImage(to, 0);

    public static unsafe Image GetImage(TextureObject to, int level)
    {
        var size = GetSize(to, level);
        var image = new Image(size.X, size.Y);
        try {
            TextureCore.GetPixels(to, new RectI(0, 0, size.X, size.Y), image.GetPixels());
        }
        catch {
            image.Dispose();
        }
        return image;
    }
}
