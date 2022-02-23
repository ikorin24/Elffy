#nullable enable
using Elffy.Graphics.OpenGL;
using Elffy.Components;

namespace Elffy.Shading
{
    public delegate bool ShaderTextureSelector<TShader>(TShader shader, Renderable target, out TextureObject textureObject) where TShader : RenderingShader;

    public static class DefaultShaderTextureSelector<TShader> where TShader : RenderingShader
    {
        private static ShaderTextureSelector<TShader>? _default;

        public static ShaderTextureSelector<TShader> Default => _default ??=
            (TShader _, Renderable renderable, out TextureObject t) =>
            {
                if(renderable.TryGetComponent<Texture>(out var texture)) {
                    t = texture.TextureObject;
                    return true;
                }
                t = TextureObject.Empty;
                return false;
            };
    }
}
