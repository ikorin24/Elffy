#nullable enable
using Elffy;
using Elffy.Shading;
using Elffy.Graphics.OpenGL;

namespace Sandbox;

public sealed class GrayscalePostProcess : PostProcess
{
    private IOffscreenBuffer _input;

    public override string FragShaderSource =>
    """
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
    """;
    public GrayscalePostProcess(IOffscreenBuffer input)
    {
        _input = input;
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in Vector2i screenSize)
    {
        dispatcher.SendUniform("_screenSize", screenSize);
        dispatcher.SendUniformTexture2D("_input", _input.RenderTargetTexture, TextureUnitNumber.Unit0);
    }
}
