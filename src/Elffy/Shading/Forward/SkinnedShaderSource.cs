#nullable enable
using Elffy.Components;
using Elffy.Core;
using Elffy.OpenGL;
using System;

namespace Elffy.Shading.Forward
{
    public sealed class SkinnedShaderSource : ShaderSource
    {
        private static SkinnedShaderSource? _instance;
        public static SkinnedShaderSource Instance => _instance ??= new();

        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        private SkinnedShaderSource() { }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "vNormal", VertexSpecialField.Normal);
            definition.Map(vertexType, "vUV", VertexSpecialField.UV);
            definition.Map(vertexType, "bone", VertexSpecialField.Bone);
            definition.Map(vertexType, "weight", VertexSpecialField.Weight);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("ma", new Color3(0.88f));
            uniform.Send("md", new Color3(0.18f));
            uniform.Send("ms", new Color3(0.1f));
            uniform.Send("shininess", 5f);

            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);

            uniform.Send("lPos", new Vector4(0, 1, 0, 0));
            uniform.Send("la", new Vector3(0.8f));
            uniform.Send("ld", new Vector3(0.8f));
            uniform.Send("ls", new Vector3(0.2f));

            var skeleton = target.GetComponent<HumanoidSkeleton>();
            uniform.SendTexture1D("_boneTrans", skeleton.TranslationData, TextureUnitNumber.Unit0);
            //var parts = target.GetComponent<PmxModelParts>();
            //ref readonly var texture = ref parts.Textures[parts.TextureIndexArray[parts.Current]];
            //uniform.SendTexture2D("tex_sampler", texture, TextureUnitNumber.Unit1);
        }

        private const string VertSource =
@"#version 410

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
in ivec4 bone;
in vec4 weight;
out vec4 Pos;
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
    mat4 skinning = weight.x * GetMat(bone.x) +
                    weight.y * GetMat(bone.y) +
                    weight.z * GetMat(bone.z) +
                    weight.w * GetMat(bone.w);
    Pos = projection * view * model * skinning * vec4(vPos, 1.0);
    Pos = projection * view * model * vec4(vPos, 1.0);
    gl_Position = Pos;
    Normal = transpose(inverse(mat3(skinning))) * vNormal;
    //Normal = vNormal;
    //UV = vUV;
}
";

        private const string FragSource =
@"#version 410

//in vec2 UV;
in vec4 Pos;
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

//uniform sampler2D tex_sampler;

void main()
{
    mat4 modelView = view * model;
    vec3 posView = (modelView * Pos).xyz;                    // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * Normal;    // normal in eye space
    vec4 lPosView = view * lPos;                                        // light pos in eye space
    vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
    vec3 N = normalize(normalView);
    vec3 R = reflect(-L, N);
    vec3 V = normalize(-posView);
    vec3 color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));

    //fragColor = vec4(color, 1.0) * texture(tex_sampler, UV);
    fragColor = vec4(color, 1.0);
}
";
    }
}
