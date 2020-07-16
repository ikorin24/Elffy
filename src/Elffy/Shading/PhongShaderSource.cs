#nullable enable
using Elffy.Core;
using Elffy.Components;
using System;
using System.Runtime.CompilerServices;
using Elffy.OpenGL;
using Elffy.Diagnostics;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(Vertex))]
    public sealed class PhongShaderSource : ShaderSource
    {
        private static PhongShaderSource? _instance;
        public static PhongShaderSource Instance => _instance ??= new PhongShaderSource();

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        private PhongShaderSource() { }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<Vertex>(nameof(Vertex.Position), "vPos");
            definition.Map<Vertex>(nameof(Vertex.Normal), "vNormal");
            definition.Map<Vertex>(nameof(Vertex.TexCoord), "vUV");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(target.TryGetComponent<Material>(out var m)) {
                uniform.Send("ma", Unsafe.As<Color4, Color3>(ref Unsafe.AsRef(m.Ambient)));
                uniform.Send("md", Unsafe.As<Color4, Color3>(ref Unsafe.AsRef(m.Diffuse)));
                uniform.Send("ms", Unsafe.As<Color4, Color3>(ref Unsafe.AsRef(m.Specular)));
                uniform.Send("shininess", m.Shininess);
            }
            else {
                uniform.Send("ma", new Color3(0.8f));
                uniform.Send("md", new Color3(0.35f));
                uniform.Send("ms", new Color3(0.5f));
                uniform.Send("shininess", 10f);
            }

            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);

            if(lights.Length > 0) {
                var light = lights[0];
                uniform.Send("lPos", light.Position4);
                uniform.Send("la", light.Ambient);
                uniform.Send("ld", light.Diffuse);
                uniform.Send("ls", light.Specular);
            }
            else {
                uniform.Send("lPos", new Vector4(0, 1, 0, 0));
                uniform.Send("la", new Vector3());
                uniform.Send("ld", new Vector3());
                uniform.Send("ls", new Vector3());
            }

            uniform.Send("tex_sampler", TextureUnitNumber.Unit0);
        }

        private const string VertSource =
@"#version 440

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
@"#version 440

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

    fragColor = vec4(color, 1.0) * texture(tex_sampler, UV);
}
";

    }
}
