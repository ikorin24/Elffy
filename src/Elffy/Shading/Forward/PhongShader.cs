#nullable enable
using Elffy.Core;
using Elffy.Components;
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Shading.Forward
{
    public sealed class PhongShader : ShaderSource
    {
        private static PhongShader? _instance;
        public static PhongShader Instance => _instance ??= new();

        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        private PhongShader() { }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "vNormal", VertexSpecialField.Normal);
            definition.Map(vertexType, "vUV", VertexSpecialField.UV);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("ma", new Color3(0.8f));
            uniform.Send("md", new Color3(0.35f));
            uniform.Send("ms", new Color3(0.5f));
            uniform.Send("shininess", 10f);

            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);

            uniform.Send("lPos", new Vector4(0, 1, 0, 0));
            uniform.Send("la", new Vector3(0.8f));
            uniform.Send("ld", new Vector3(0.8f));
            uniform.Send("ls", new Vector3(0.2f));

            var hasTexture = target.TryGetComponent<Texture>(out var texture);
            var texObj = hasTexture ? texture!.TextureObject : TextureObject.Empty;
            uniform.SendTexture2D("tex_sampler", texObj, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", hasTexture);
        }

        private const string VertSource =
@"#version 410

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
out vec3 Pos;
out vec3 Normal;
out vec2 UV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    UV = vUV;
    Pos = vPos;
    Normal = vNormal;
    mat4 modelView = view * model;
    gl_Position = projection * modelView * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 410

in vec2 UV;
in vec3 Pos;
in vec3 Normal;
out vec4 fragColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec4 lPos;
uniform vec3 la;
uniform vec3 ld;
uniform vec3 ls;
uniform vec3 ma;
uniform vec3 md;
uniform vec3 ms;
uniform float shininess;

uniform sampler2D tex_sampler;
uniform bool hasTexture;

void main()
{
    mat4 modelView = view * model;
    vec3 posView = (modelView * vec4(Pos, 1.0)).xyz;                    // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * Normal;    // normal in eye space
    vec4 lPosView = view * lPos;                                        // light pos in eye space
    vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
    vec3 N = normalize(normalView);
    vec3 R = reflect(-L, N);
    vec3 V = normalize(-posView);
    vec3 color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));

    fragColor = hasTexture ? vec4(color, 1.0) * texture(tex_sampler, UV)
                           : vec4(color, 1.0);
}
";

    }
}
