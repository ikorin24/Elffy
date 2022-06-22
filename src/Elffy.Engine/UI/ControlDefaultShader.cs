#nullable enable

namespace Elffy.UI
{
    internal sealed class ControlDefaultShader : ControlDefaultShaderBase
    {
        public static ControlDefaultShader Instance { get; } = new();

        private ControlDefaultShader()
        {
            // Don't use base.LoadImage, base.UpdateImage, base.ReleaseImage
            // for the singleton instance
        }
    }
}
