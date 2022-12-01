#nullable enable
using Elffy.Components;

namespace Elffy.Shading.Forward;

public sealed class PmxModelShader : RenderingShader
{
    public PmxModelShader()
    {
    }

    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map(context.VertexType, "vPos", VertexFieldSemantics.Position);
        definition.Map(context.VertexType, "vNormal", VertexFieldSemantics.Normal);
        definition.Map(context.VertexType, "vUV", VertexFieldSemantics.UV);
        definition.Map(context.VertexType, "bone", VertexFieldSemantics.Bone);
        definition.Map(context.VertexType, "weight", VertexFieldSemantics.Weight);
        definition.Map(context.VertexType, "vtexIndex", VertexFieldSemantics.TextureIndex);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("modelView", context.View * context.Model);
        dispatcher.SendUniform("view", context.View);
        dispatcher.SendUniform("projection", context.Projection);

        var target = context.Target;
        var skeleton = target.GetComponent<HumanoidSkeleton>();
        dispatcher.SendUniformTexture1D("_boneTrans", skeleton.TranslationData, 0);
        dispatcher.SendUniformTexture2DArray("_texArrSampler", target.GetComponent<ArrayTexture>().TextureObject, 1);

        var screen = context.Screen;
        var lights = screen.RenderPipeline.Lights;
        var light = lights.Length switch
        {
            > 0 => (
                Position: lights[0].Position,
                Color: lights[0].Color,
                LightMvp: lights[0].ShadowMap.LightMatrices[0] * context.Model,
                ShadowMap: lights[0].ShadowMap.LightDepthTexture,
                Exists: true
            ),
            _ => default,
        };
        dispatcher.SendUniform("_lPos", light.Position);
        dispatcher.SendUniform("_lColor", light.Color);
        dispatcher.SendUniform("_lExists", light.Exists);
        dispatcher.SendUniform("_lmvp", light.LightMvp);
        dispatcher.SendUniformTexture2DArray("_shadowMap", light.ShadowMap, 2);
    }

    protected override ShaderSource GetShaderSource(in ShaderGetterContext context) => new()
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
        out vec3 ShadowMapNDC;
        flat out float texIndex;

        uniform mat4 modelView;
        uniform mat4 view;
        uniform mat4 projection;
        uniform sampler1D _boneTrans;
        uniform mat4 _lmvp;

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
            vec4 posLightSpace = _lmvp * vec4(vPos, 1.0);
            ShadowMapNDC = posLightSpace.xyz / posLightSpace.w;
        }
        """u8,
        FragmentShader =
        """
        #version 410
        #extension GL_NV_texture_array : enable

        in vec2 UV;
        in vec4 Pos;
        in vec3 Normal;
        in vec3 ShadowMapNDC;
        flat in float texIndex;
        out vec4 fragColor;

        uniform mat4 modelView;
        uniform mat4 view;
        uniform mat4 projection;

        uniform vec4 _lPos;
        uniform vec4 _lColor;
        uniform bool _lExists;
        uniform sampler2DArray _shadowMap;

        uniform sampler2DArray _texArrSampler;

        float CalcShadow(vec3 shadowMapNDC, sampler2DArray shadowMap, float bias)
        {
            vec3 range = 1.0 - step(vec3(1.0), abs(shadowMapNDC));
            float filter = range.x * range.y * range.z;
            vec3 shadowMapUV = shadowMapNDC * 0.5 + 0.5;
            float d = textureLod(shadowMap, vec3(shadowMapUV.xy, 0), 0).x;
            float shadow = 1.0 - step(shadowMapUV.z - bias, d);
            return shadow * filter;
        }

        void main()
        {
            vec3 posView = (modelView * Pos).xyz;                    // vertex pos in eye space
            vec3 normalView = transpose(inverse(mat3(modelView))) * Normal;    // normal in eye space
            vec3 lightColor = vec3(0, 0, 0);
            const vec3 ma = vec3(0.8, 0.8, 0.8);
            const vec3 md = vec3(0.2, 0.2, 0.2);
            const vec3 ms = vec3(0.5, 0.5, 0.5);
            const float shininess = 5;
            vec4 lPosView = view * _lPos;               // light pos in eye space
            vec3 L = (lPosView.w == 0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
            vec3 N = normalize(normalView);
            vec3 R = reflect(-L, N);
            vec3 V = normalize(-posView);
            vec3 l = _lColor.rgb;
            vec3 la = l * 0.8;
            vec3 ld = l * 0.8;
            vec3 ls = l * 0.2;
            float dot_nl = clamp(dot(N, L), 0, 1);
            float bias = clamp(0.001 * tan(acos(dot_nl)), 0.0, 0.01);
            float shadow = CalcShadow(ShadowMapNDC, _shadowMap, bias);
            vec3 ambient = (la * ma);
            vec3 diffuse = (ld * md * dot_nl);
            vec3 specular = (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));
            vec3 color = ambient + (diffuse + specular) * (1.0 - shadow);
            lightColor += color;
            fragColor = vec4(lightColor, 1.0) * texture2DArray(_texArrSampler, vec3(UV, texIndex));
        }
        """u8,
    };
}

