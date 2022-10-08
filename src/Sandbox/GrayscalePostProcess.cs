#nullable enable
using Elffy;
using Elffy.Shading;
using Elffy.Graphics.OpenGL;

namespace Sandbox;

public sealed class GrayscalePostProcess : PostProcess
{
    private IOffscreenBuffer _input;
    public GrayscalePostProcess(IOffscreenBuffer input)
    {
        _input = input;
    }

    protected override void OnRendering(PostProcessRenderContext context)
    {
        var screen = context.Screen;
        var dispatcher = context.Dispatcher;
        dispatcher.SendUniform("_screenSize", screen.FrameBufferSize);
        dispatcher.SendUniformTexture2D("_input", _input.RenderTargetTexture, TextureUnitNumber.Unit0);
    }

    protected override PostProcessSource GetPostProcessSource(PostProcessGetterContext context) => new()
    {
        FragmentShader = """
        #version 410
        in V2f
        {
            vec2 uv;
        } _v2f;
        out vec4 _fragColor;
        uniform ivec2 _screenSize;
        uniform sampler2D _input;
        const vec3 GrayFactor = vec3(0.299, 0.587, 0.114);
        void main()
        {
            vec4 color = textureLod(_input, _v2f.uv, 0);
            float gray = dot(color.rgb, GrayFactor);
            _fragColor = vec4(gray, gray, gray, color.a);
        }
        """u8,
    };
}
