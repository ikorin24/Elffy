#nullable enable

namespace Elffy.UI
{
    internal sealed class ButtonDefaultShader : TextContentShaderBase
    {
        public ButtonDefaultShader()
        {
        }

        protected override Color4 GetBackground(Control target)
        {
            if(target is Executable executable) {
                var bg = executable.Background;
                if(executable.IsKeyPressed) {
                    const float P = 0.4f;
                    return new Color4(bg.R - P * 0.299f, bg.G - P * 0.587f, bg.B - P * 0.114f, bg.A);
                }
                else if(executable.IsMouseOver) {
                    const float P = 0.25f;
                    return new Color4(bg.R - P * 0.299f, bg.G - P * 0.587f, bg.B - P * 0.114f, bg.A);
                }
                else {
                    return bg;
                }
            }
            return base.GetBackground(target);
        }
    }
}
