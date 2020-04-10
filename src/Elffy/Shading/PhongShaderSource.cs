#nullable enable
using Elffy.Core;
using System.Runtime.CompilerServices;

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

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var material = new Material(new Color4(0.6f), new Color4(0.35f), new Color4(0f), 0f);
            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);
            uniform.Send("eyePos", target.HostScreen.Camera.Position);
            uniform.Send("lPos", new Vector4(1, 1, 0, 0));
            uniform.Send("la", new Vector3(0.8f));
            uniform.Send("ld", new Vector3(0.8f));
            uniform.Send("ls", new Vector3(1f));
            uniform.Send("ma", (Vector3)material.Ambient);
            uniform.Send("md", (Vector3)material.Diffuse);
            uniform.Send("ms", (Vector3)material.Specular);
            uniform.Send("shininess", material.Shininess);
        }

        // TODO: shininess の計算方法
        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec3 vNormal;
out vec3 color;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 eyePos;
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
    gl_Position = projection * view * model * vec4(vPos, 1.0);

    vec3 posWorld = (model * vec4(vPos, 1.0)).xyz;
    vec3 normalWorld = (model * vec4(vNormal, 1.0)).xyz;
    vec3 L = (lPos.w == 0.0) ? normalize(-lPos.xyz) : normalize(posWorld - lPos.xyz / lPos.w);
    vec3 N = normalize(normalWorld);
    vec3 R = reflect(L, N);
    vec3 V = normalize(eyePos - posWorld);
    color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(dot(R, V), shininess), 0.0));
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
