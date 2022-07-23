#nullable enable
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading.Deferred
{
    internal sealed class PbrDeferredPostProcess : PostProcess
    {
        private readonly IGBufferProvider _gBufferProvider;

        public override string FragShaderSource => FragSource;

        internal PbrDeferredPostProcess(IGBufferProvider gBufferProvider)
        {
            _gBufferProvider = gBufferProvider;
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, in Vector2i screenSize)
        {
            var (screen, gBuffer) = _gBufferProvider.GetValidScreenAndGBuffer();
            var lights = screen.Lights;
            var gData = gBuffer.GetBufferData();

            var lightCount = lights.LightCount;
            var camera = screen.Camera;
            dispatcher.SendUniform("_view", camera.View);
            dispatcher.SendUniform("_projection", camera.Projection);
            dispatcher.SendUniform("_lightCount", lightCount);
            dispatcher.SendUniformTexture2D("_posSampler", gData.Position, TextureUnitNumber.Unit0);
            dispatcher.SendUniformTexture2D("_normalSampler", gData.Normal, TextureUnitNumber.Unit1);
            dispatcher.SendUniformTexture2D("_albedoSampler", gData.Albedo, TextureUnitNumber.Unit2);
            dispatcher.SendUniformTexture2D("_emitSampler", gData.Emit, TextureUnitNumber.Unit3);
            dispatcher.SendUniformTexture2D("_metallicRoughnessSampler", gData.MetallicRoughness, TextureUnitNumber.Unit4);
            dispatcher.SendUniformTexture1D("_lightPosSampler", lights.PositionTexture, TextureUnitNumber.Unit5);
            dispatcher.SendUniformTexture1D("_lightColorSampler", lights.ColorTexture, TextureUnitNumber.Unit6);

            if(lightCount > 0) {
                var shadowMaps = lights.GetShadowMaps();
                dispatcher.SendUniformTexture1D("_lmat", lights.MatrixTexture, TextureUnitNumber.Unit7);
                dispatcher.SendUniformTexture2D("_shadowMap", shadowMaps[0].DepthTexture, TextureUnitNumber.Unit8);
            }
        }

        private const string FragSource =
@"#version 410
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

in vec2 _uv;
uniform mat4 _view;
uniform mat4 _projection;
uniform sampler2D _posSampler;
uniform sampler2D _normalSampler;
uniform sampler2D _albedoSampler;
uniform sampler2D _emitSampler;
uniform sampler2D _metallicRoughnessSampler;
uniform sampler1D _lightPosSampler;
uniform sampler1D _lightColorSampler;
uniform int _lightCount;
uniform sampler1D _lmat;
uniform sampler2D _shadowMap;
out vec4 _fragColor;

m_float D_GGX(m_vec3 n, m_vec3 h, m_float dot_nh, m_float roughness)     // Trowbridge-Reitz
{
    m_float p = roughness * dot_nh;
    m_vec3 cross_nh = cross(n, h);
    m_float q = roughness / (dot(cross_nh, cross_nh) + p * p);
    return min(q * q * INV_PI, 16300.0);        // 16300.0 is about 2^14, safe max of mediump float
}

float V_SmithGGXCorrelated(float dot_nl, float dot_nv, float alpha)    // Height-Correlated Smith
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

m_vec3 F_Schlick(m_vec3 f0, m_float u)
{
    vec3 f90 = vec3(1.0, 1.0, 1.0);
    m_float x = 1.0 - u;
    m_float x2 = x * x;
    m_float x5 = x2 * x2 * x;
    return f0 + (f90 - f0) * x5;
}

float Fd_Burley(float dot_nv, float dot_nl, float dot_lh, float roughness)
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
    vec4 posWorld = textureLod(_posSampler, _uv, 0);
    if(posWorld.w < 0.01) {
        gl_FragDepth = 1;
        discard;
        return;
    }
    mat3 viewInvT = transpose(inverse(mat3(_view)));
    vec3 nWorld = textureLod(_normalSampler, _uv, 0).rgb;
    vec3 albedo = textureLod(_albedoSampler, _uv, 0).rgb;
    vec3 emit = textureLod(_emitSampler, _uv, 0).rgb;
    vec3 pos = ToVec3(_view * posWorld);    // pos in eye space
    vec4 dncPos = _projection * vec4(pos, 1);    // device-normalized-coordinate position
    gl_FragDepth = (dncPos.z / dncPos.w) * 0.5 + 0.5;
    vec4 metallicRoughness = textureLod(_metallicRoughnessSampler, _uv, 0);
    float metallic = metallicRoughness.r;
    float roughness = metallicRoughness.g;
    vec3 v = -normalize(pos);                               // eye direction in eye space, normalized
    vec3 n = viewInvT * nWorld;                             // normal direction in eye space, normalized
    float dot_nv = abs(dot(n, v));
    float reflectivity = mix(DielectricF0, 1.0, metallic);
    vec3 f0 = mix(vec3(DielectricF0, DielectricF0, DielectricF0), albedo, metallic);

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
            l = normalize(lPos.xyz - pos);          // light vec in eye space, normalized
        }
        vec3 h = normalize(v + l);                  // half vector in eye space, normalized
        float dot_nl = max(0.0, dot(n, l));
        float dot_nh = max(0.0, dot(n, h));
        float dot_lh = max(0.0, dot(l, h));

        // Diffuse
        float diffuseTerm = Fd_Burley(dot_nv, dot_nl, dot_lh, roughness) * dot_nl;
        vec3 diffuse = (1.0 - reflectivity) * diffuseTerm * lColor * albedo;

        // Specular
        float alpha = roughness * roughness;
        float V = V_SmithGGXCorrelated(dot_nl, dot_nv, alpha);
        float D = D_GGX(n, h, dot_nh, roughness);
        vec3 F = F_Schlick(f0, dot_lh);
        vec3 specular = V * D * F * dot_nl * lColor;
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
    
    _fragColor = vec4(fragColor, 1.0);
}
";
    }
}
