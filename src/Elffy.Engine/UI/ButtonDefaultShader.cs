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
                return executable.IsKeyPressed ? Color4.Red :
                       executable.IsMouseOver ? Color4.BlueViolet :
                       executable.Background;
            }
            return base.GetBackground(target);
        }
    }
}
