#nullable enable
using Elffy.Core;
using System;

namespace Elffy.Shading
{
    public sealed class PhongShaderSource : ShaderSource
    {
        private static PhongShaderSource? _instance;
        internal static PhongShaderSource Instance => _instance ??= new PhongShaderSource();

        private PhongShaderSource() { }

        protected override string VertexShaderSource() => VertSource;

        protected override string FragmentShaderSource() => FragSource;

        protected override void DefineLocation(VertexDefinition definition)
        {
            definition.Position("vPos");
            definition.Normal("vNormal");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            // TODO: lights を使う (どうやって可変長かつ抽象型をうまく扱えばいいの?)

            var material = new Material(new Color4(0.6f), new Color4(0.35f), new Color4(0.5f), 10f);
            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);
            uniform.Send("lPos", new Vector4(1, 1, 0, 0));
            uniform.Send("la", new Vector3(0.8f));
            uniform.Send("ld", new Vector3(0.8f));
            uniform.Send("ls", new Vector3(1f));
            uniform.Send("ma", (Color3)material.Ambient);
            uniform.Send("md", (Color3)material.Diffuse);
            uniform.Send("ms", (Color3)material.Specular);
            uniform.Send("shininess", material.Shininess);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec3 vNormal;
out vec3 color;
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

void main()
{
    mat4 modelView = view * model;
    gl_Position = projection * modelView * vec4(vPos, 1.0);

    vec3 posView = (modelView * vec4(vPos, 1.0)).xyz;                   // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * vNormal;    // normal in eye space
    vec4 lPosView = view * lPos;                                        // light pos in eye space
    vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
    vec3 N = normalize(normalView);
    vec3 R = reflect(-L, N);
    vec3 V = normalize(-posView);
    color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));
}	

";

        private const string FragSource =
@"#version 440

in vec3 color;
out vec4 fragColor;

void main()
{
    fragColor = vec4(color, 1.0);
}
";

    }
}
