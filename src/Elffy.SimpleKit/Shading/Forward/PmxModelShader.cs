#nullable enable
using Elffy.Components;
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Shading.Forward;

public sealed class PmxModelShader : RenderingShader
{
    public PmxModelShader()
    {
    }

    protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, "vPos", VertexSpecialField.Position);
        definition.Map(vertexType, "vNormal", VertexSpecialField.Normal);
        definition.Map(vertexType, "vUV", VertexSpecialField.UV);
        definition.Map(vertexType, "bone", VertexSpecialField.Bone);
        definition.Map(vertexType, "weight", VertexSpecialField.Weight);
        definition.Map(vertexType, "vtexIndex", VertexSpecialField.TextureIndex);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
    {
        dispatcher.SendUniform("modelView", view * model);
        dispatcher.SendUniform("view", view);
        dispatcher.SendUniform("projection", projection);

        var skeleton = target.GetComponent<HumanoidSkeleton>();
        dispatcher.SendUniformTexture1D("_boneTrans", skeleton.TranslationData, TextureUnitNumber.Unit0);
        dispatcher.SendUniformTexture2DArray("_texArrSampler", target.GetComponent<ArrayTexture>().TextureObject, TextureUnitNumber.Unit1);

        var screen = target.GetValidScreen();
        var lights = screen.Lights;
        dispatcher.SendUniform("lightCount", lights.LightCount);
        dispatcher.SendUniformTexture1D("lColorSampler", lights.ColorTexture, TextureUnitNumber.Unit2);
        dispatcher.SendUniformTexture1D("lPosSampler", lights.PositionTexture, TextureUnitNumber.Unit3);
    }

    protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer) => new()
    {
        OnlyContainsConstLiteralUtf8 = true,
        VertexShader =
        """
        #version 410

        in vec3 vPos;
        in vec3 vNormal;
        in vec2 vUV;
        in ivec4 bone;
        in vec4 weight;
        in int vtexIndex;
        out vec4 Pos;
        out vec3 Normal;
        out vec2 UV;
        flat out float texIndex;

        uniform mat4 modelView;
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
            Pos = projection * modelView * skinning * vec4(vPos, 1.0);
            gl_Position = Pos;
            Normal = transpose(inverse(mat3(skinning))) * vNormal;
            UV = vUV;
            texIndex = vtexIndex;
        }
        """u8,
        FragmentShader =
        """
        #version 410
        #extension GL_NV_texture_array : enable

        in vec2 UV;
        in vec4 Pos;
        in vec3 Normal;
        flat in float texIndex;
        out vec4 fragColor;

        uniform mat4 modelView;
        uniform mat4 view;
        uniform mat4 projection;
        uniform int lightCount;
        uniform sampler1D lPosSampler;
        uniform sampler1D lColorSampler;

        uniform sampler2DArray _texArrSampler;

        void main()
        {
            vec3 posView = (modelView * Pos).xyz;                    // vertex pos in eye space
            vec3 normalView = transpose(inverse(mat3(modelView))) * Normal;    // normal in eye space
            vec3 lightColor = vec3(0, 0, 0);
            const vec3 ma = vec3(0.8, 0.8, 0.8);
            const vec3 md = vec3(0.2, 0.2, 0.2);
            const vec3 ms = vec3(0.5, 0.5, 0.5);
            const float shininess = 5;
            for(int i = 0; i < lightCount; i++) {
                vec4 lPosView = view * texelFetch(lPosSampler, i, 0);               // light pos in eye space
                vec3 L = (lPosView.w == 0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
                vec3 N = normalize(normalView);
                vec3 R = reflect(-L, N);
                vec3 V = normalize(-posView);
                vec3 l = texelFetch(lColorSampler, i, 0).rgb;
                vec3 la = l * 0.8;
                vec3 ld = l * 0.8;
                vec3 ls = l * 0.2;
                vec3 color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));
                lightColor += color;
            }
            fragColor = vec4(lightColor, 1.0) * texture2DArray(_texArrSampler, vec3(UV, texIndex));
        }
        """u8,
    };
}

