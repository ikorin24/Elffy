#nullable enable
using System;

namespace Elffy.Shading.Forward;

public sealed class WireframeShader : SingleTargetRenderingShader
{
    private Color3 _wireColor = new Color3(0f, 0.5f, 1f);

    public Color3 WireColor { get => _wireColor; set => _wireColor = value; }

    public WireframeShader()
    {
    }

    protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, "_pos", VertexSpecialField.Position);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
    {
        dispatcher.SendUniform("_mvp", projection * view * model);
        dispatcher.SendUniform("_wireColor", _wireColor);
    }

    protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer) => layer switch
    {
        DeferredRenderLayer => throw new NotSupportedException(),
        ForwardRenderLayer => ForwardRenderingSource(),
        _ => throw new NotSupportedException(),
    };

    protected override void OnTargetAttached(Renderable target) { }

    protected override void OnTargetDetached(Renderable detachedTarget) { }

    private static ShaderSource ForwardRenderingSource() => new()
    {
        VertexShader =
@"#version 410
in vec3 _pos;
uniform mat4 _mvp;
void main()
{
    gl_Position = _mvp * vec4(_pos, 1);
}
",
        GeometryShader =
@"#version 460
layout (triangles) in;
layout (line_strip, max_vertices = 3) out;

out vec3 _fcolor;
void main()
{
    gl_Position = gl_in[0].gl_Position;
    EmitVertex();
    gl_Position = gl_in[1].gl_Position;
    EmitVertex();
    gl_Position = gl_in[2].gl_Position;
    EmitVertex();
    EndPrimitive();
}
",
        FragmentShader =
@"#version 410
out vec4 _outColor;
uniform vec3 _wireColor;
void main()
{
    _outColor = vec4(_wireColor, 1);
}
",
    };
}
