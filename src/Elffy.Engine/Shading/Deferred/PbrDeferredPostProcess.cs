#nullable enable
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Shading.Deferred
{
    internal sealed class PbrDeferredPostProcess : PostProcess
    {
        private readonly IGBufferProvider _gBufferProvider;

        public override ReadOnlySpan<byte> FragShaderSource => FragSource;

        internal PbrDeferredPostProcess(IGBufferProvider gBufferProvider)
        {
            _gBufferProvider = gBufferProvider;
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, in Vector2i screenSize)
        {
            var gBuffer = _gBufferProvider.GetGBufferData();
            var screen = _gBufferProvider.GetValidScreen();
            var lights = screen.Lights;

            var lightCount = lights.LightCount;
            var camera = screen.Camera;
            dispatcher.SendUniform("_view", camera.View);
            dispatcher.SendUniform("_projection", camera.Projection);
            dispatcher.SendUniform("_lightCount", lightCount);
            dispatcher.SendUniformTexture2D("_mrt0", gBuffer.Mrt[0], TextureUnitNumber.Unit0);
            dispatcher.SendUniformTexture2D("_mrt1", gBuffer.Mrt[1], TextureUnitNumber.Unit1);
            dispatcher.SendUniformTexture2D("_mrt2", gBuffer.Mrt[2], TextureUnitNumber.Unit2);
            dispatcher.SendUniformTexture1D("_lightPosSampler", lights.PositionTexture, TextureUnitNumber.Unit3);
            dispatcher.SendUniformTexture1D("_lightColorSampler", lights.ColorTexture, TextureUnitNumber.Unit4);

            var light = lights.GetLights().FirstOrDefault();
            if(light != null) {
                dispatcher.SendUniformTexture1D("_lmat", lights.MatrixTexture, TextureUnitNumber.Unit5);
                dispatcher.SendUniformTexture2D("_shadowMap", light.ShadowMap.DepthTexture, TextureUnitNumber.Unit6);
            }
        }

        // index  | R           | G            | B           | A         |
        // ----
        // mrt[0] | pos.x       | pos.y        | pos.z       | 1         |
        // mrt[1] | normal.x    | normal.y     | normal.z    | roughness |
        // mrt[2] | baseColor.r | baseColor.g  | baseColor.b | metallic  |
        // mrt[3] | 0           | 0            | 0           | 0         |
        // mrt[4] | 0           | 0            | 0           | 0         |

        private static ReadOnlySpan<byte> FragSource =>
"""
#version 410
#define m_float mediump float
#define m_vec2  mediump vec2
#define m_vec3  mediump vec3
#define m_vec4  mediump vec4
#define h_float highp float
#define h_vec2 highp vec2
#define h_vec3 highp vec3
#define h_vec4 highp vec4
const float INV_PI = 1.0 / 3.1415926;
const float DielectricF0 = 0.04;

// TODO: calculate from light probe
const vec3 IndirectDiffuse = vec3(0.5, 0.5, 0.5);
const vec3 IndirectSpecular = vec3(0, 0, 0);

in V2f
{
    vec2 uv;
} _v2f;
uniform mat4 _view;
uniform mat4 _projection;
uniform sampler2D _mrt0;
uniform sampler2D _mrt1;
uniform sampler2D _mrt2;
uniform sampler1D _lightPosSampler;
uniform sampler1D _lightColorSampler;
uniform int _lightCount;
uniform sampler1D _lmat;
uniform sampler2D _shadowMap;
out vec4 _fragColor;

m_float GGX(m_vec3 n, m_vec3 h, m_float dot_nh, m_float roughness)     // Trowbridge-Reitz
{
    m_float p = roughness * dot_nh;
    m_vec3 cross_nh = cross(n, h);
    m_float q = roughness / (dot(cross_nh, cross_nh) + p * p);
    return min(q * q * INV_PI, 16300.0);        // 16300.0 is about 2^14, safe max of mediump float
}

float SmithGGXCorrelated(float dot_nl, float dot_nv, float alpha)    // Height-Correlated Smith
{
    // For optimization, we will approximate the following expression.
    // (This approximation is not mathematically correct, but it works fine.)

    // float a2 = alpha * alpha;
    // float lambdaV = dot_nl * sqrt((-dot_nv * a2 + dot_nv) * dot_nv + a2);
    // float lambdaL = dot_nv * sqrt((-dot_nl * a2 + dot_nl) * dot_nl + a2);

    float beta = 1.0 - alpha;
    float lambdaV = dot_nl * (dot_nv * beta + alpha);
    float lambdaL = dot_nv * (dot_nl * beta + alpha);

    return 0.5 / (lambdaV + lambdaL + 0.0001);
}

m_vec3 FresnelSchlick(m_vec3 f0, m_vec3 f90, m_float u)
{
    m_float x = 1.0 - u;
    m_float x2 = x * x;
    m_float x5 = x2 * x2 * x;
    return f0 + (f90 - f0) * x5;
}

float Burley(float dot_nv, float dot_nl, float dot_lh, float roughness)
{
    float fd90 = 0.5 + 2.0 * dot_lh * dot_lh * roughness;
    float p = 1.0 - dot_nl;
    float q = 1.0 - dot_nv;
    float p2 = p * p;
    float q2 = q * q;
    float p5 = p2 * p2 * p;
    float q5 = q2 * q2 * q;
    float lightScatter = 1.0 + (fd90 - 1.0) * p5;
    float viewScatter = 1.0 + (fd90 - 1.0) * q5;
    return lightScatter * viewScatter * INV_PI;
}

float Lambert()
{
    return INV_PI;
}

vec3 ToVec3(vec4 v)
{
    return v.xyz / v.w;
}

float CalcShadow(vec3 shadowMapNDC, sampler2D shadowMap)
{
    const float bias = 0.0004;
    vec3 range = 1.0 - step(vec3(1.0), abs(shadowMapNDC));
    float filter = range.x * range.y * range.z;
    vec3 shadowMapUV = shadowMapNDC * 0.5 + 0.5;
    float d = textureLod(shadowMap, shadowMapUV.xy, 0).x;
    float shadow = 1.0 - step(shadowMapUV.z - bias, d);
    return shadow * filter;
}

void main()
{
    vec4 mrt0Value = textureLod(_mrt0, _v2f.uv, 0);
    vec4 posWorld = vec4(mrt0Value.rgb, 1.0);
    if(mrt0Value.w == 0) {
        gl_FragDepth = 1;
        discard;
        return;
    }
    mat3 viewInvT = transpose(inverse(mat3(_view)));
    vec4 mrt1Value = textureLod(_mrt1, _v2f.uv, 0);
    vec4 mrt2Value = textureLod(_mrt2, _v2f.uv, 0);
    vec3 nWorld = mrt1Value.rgb;
    vec3 baseColor = mrt2Value.rgb;
    vec3 pos = ToVec3(_view * posWorld);    // pos in eye space
    vec4 dncPos = _projection * vec4(pos, 1);    // device-normalized-coordinate position
    gl_FragDepth = (dncPos.z / dncPos.w) * 0.5 + 0.5;
    float metallic = mrt2Value.a;
    float roughness = mrt1Value.a;
    float alpha = roughness * roughness;
    vec3 v = -normalize(pos);                               // eye direction in eye space, normalized
    vec3 n = viewInvT * nWorld;                             // normal direction in eye space, normalized
    float dot_nv = abs(dot(n, v));
    float reflectivity = mix(DielectricF0, 1.0, metallic);
    vec3 f0 = mix(vec3(DielectricF0, DielectricF0, DielectricF0), baseColor, metallic);

    vec3 fragColor = vec3(0.0, 0.0, 0.0);
    for(int i = 0; i < _lightCount; i++) {
        vec4 lPosWorld = texelFetch(_lightPosSampler, i, 0);
        vec3 lColor = texelFetch(_lightColorSampler, i, 0).rgb; // light color
        vec4 lPos = _view * lPosWorld;              // light pos in eye space

        vec3 l;
        if(lPos.w < 0.001) {
            l = normalize(lPos.xyz);                // light vec in eye space, normalized
        }
        else {
            l = normalize(lPos.xyz / lPos.w - pos);          // light vec in eye space, normalized
        }
        vec3 h = normalize(v + l);                  // half vector in eye space, normalized
        float dot_nl = max(0.0, dot(n, l));
        float dot_lh = max(0.0, dot(l, h));
        vec3 irradiance = dot_nl * lColor;

        // Diffuse
        // You can use Burley instead of Lambert.
        vec3 diffuse = (1.0 - reflectivity) * Lambert() * irradiance * baseColor;

        // Specular
        float dot_nh = dot(n, h);
        float V = SmithGGXCorrelated(dot_nl, dot_nv, alpha);
        float D = GGX(n, h, dot_nh, roughness) * step(0.0, dot_nh);
        vec3 F = FresnelSchlick(f0, vec3(1.0, 1.0, 1.0), dot_lh);
        vec3 specular = V * D * F * irradiance;
        specular = max(vec3(0.0, 0.0, 0.0), specular);

        if(i == 0) {
            mat4 lmat = mat4(
                texelFetch(_lmat, 0, 0),
                texelFetch(_lmat, 1, 0),
                texelFetch(_lmat, 2, 0),
                texelFetch(_lmat, 3, 0)
            );
            vec3 shadowMapNdc = ToVec3(lmat * posWorld);
            float shadow = CalcShadow(shadowMapNdc, _shadowMap);
            fragColor += (diffuse + specular) * (1.0 - shadow);
        }
        else {
            fragColor += diffuse + specular;
        }
    }

    // indirect diffuse
    fragColor += (1.0 - reflectivity) * IndirectDiffuse * baseColor;

    // indirect specular
    float f90 = max(0, min(1, 1 - roughness + reflectivity));
    fragColor += 1.0 / (alpha * alpha + 1.0) * FresnelSchlick(f0, vec3(f90, f90, f90), dot_nv) * IndirectSpecular;
    
    _fragColor = vec4(fragColor, 1.0);
}
"""u8;
    }
}
