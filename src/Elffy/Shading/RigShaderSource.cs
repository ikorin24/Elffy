#nullable enable
using Elffy.Components;
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.Effective;
using Elffy.OpenGL;
using System;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(RigVertex))]
    public sealed class RigShaderSource : ShaderSource
    {
        private static RigShaderSource? _instance;

        public static RigShaderSource Instance => _instance ??= new RigShaderSource();

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        private RigShaderSource() { }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<RigVertex>(nameof(RigVertex.Position), "vPos");
            definition.Map<RigVertex>(nameof(RigVertex.Normal), "vNormal");
            definition.Map<RigVertex>(nameof(RigVertex.TexCoord), "vUV");
            definition.Map<RigVertex>(nameof(RigVertex.Bone), "bone");
            definition.Map<RigVertex>(nameof(RigVertex.Weight), "weight");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(target.TryGetComponent<Material>(out var m)) {
                uniform.Send("ma", UnsafeEx.As<Color4, Color3>(m.Ambient));
                uniform.Send("md", UnsafeEx.As<Color4, Color3>(m.Diffuse));
                uniform.Send("ms", UnsafeEx.As<Color4, Color3>(m.Specular));
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

            uniform.Send("lPos", new Vector4(0, 1, 0, 0));
            uniform.Send("la", new Vector3(0.8f));
            uniform.Send("ld", new Vector3(0.8f));
            uniform.Send("ls", new Vector3(0.2f));

            var skeleton = target.GetComponent<Skeleton>();
            uniform.SendTexture1D("_boneTrans", skeleton.TranslationData, TextureUnitNumber.Unit0);
            uniform.SendTexture2D("tex_sampler", target.GetComponent<MultiTexture>().CurrentTextureObject, TextureUnitNumber.Unit1);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
in ivec4 bone;
in vec4 weight;
out vec3 Pos;
out vec3 Normal;
out vec2 UV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform sampler1D _boneTrans;

mat4 GetMat(int boneIndex)
{
    return mat4(texelFetch(_boneTrans, boneIndex * 4,     0),
                texelFetch(_boneTrans, boneIndex * 4 + 1, 0),
                texelFetch(_boneTrans, boneIndex * 4 + 2, 0),
                texelFetch(_boneTrans, boneIndex * 4 + 3, 0));
}

void main()
{
    vec4 skinned = weight.x * (GetMat(bone.x) * vec4(vPos, 1.0)) +
                   weight.y * (GetMat(bone.y) * vec4(vPos, 1.0)) +
                   weight.z * (GetMat(bone.z) * vec4(vPos, 1.0)) +
                   weight.w * (GetMat(bone.w) * vec4(vPos, 1.0));
    vec4 tmp = projection * view * model * skinned;
    Pos = tmp.xyz;
    gl_Position = tmp;
    Normal = vNormal;       // TODO: skinning of normal
    UV = vUV;
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
