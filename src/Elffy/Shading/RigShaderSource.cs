#nullable enable
using Elffy.Components;
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.Effective;
using Elffy.OpenGL;
using OpenToolkit.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
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

            var skeleton = target.GetComponent<Skeleton>();
            uniform.Send("_boneCountInverse", 1f / skeleton.BoneCount);
            skeleton.Apply(TextureUnitNumber.Unit1);
            uniform.Send("_boneMove", TextureUnitNumber.Unit1);

            const TextureUnitNumber texUnit = TextureUnitNumber.Unit0;
            target.GetComponent<MultiTexture>().Apply(texUnit);
            uniform.Send("tex_sampler", texUnit);
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
uniform highp sampler1D _boneMove;
uniform highp float _boneCountInverse;

vec4 GetBoneMove(int index)
{
    highp float u = (float(index) + 0.5) * _boneCountInverse;
    return texture(_boneMove, u);
}

mat4 GetBoneMoveMat(ivec4 b)
{
    return mat4(GetBoneMove(b[0]), GetBoneMove(b[1]), GetBoneMove(b[2]), GetBoneMove(b[3]));
}

void main()
{
    vec3 move = (GetBoneMoveMat(bone) * weight).xyz;
    vec3 pos = vPos + move;
    UV = vUV;
    Pos = pos;
    Normal = vNormal;
    mat4 modelView = view * model;
    gl_Position = projection * modelView * vec4(pos, 1.0);
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
