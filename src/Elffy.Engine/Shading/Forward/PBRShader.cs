#nullable enable
using Elffy.Components;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Shading.Forward
{
    /// <summary>Shader source for physics based rendering</summary>
    [Obsolete("Don't use.", true)]
    public sealed class PBRShader : RenderingShader
    {
        protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
        {
            definition.Map(context.VertexType, "_vPos", VertexFieldSemantics.Position);
            definition.Map(context.VertexType, "_vNormal", VertexFieldSemantics.Normal);
            definition.Map(context.VertexType, "_vUV", VertexFieldSemantics.UV);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
        {
            dispatcher.SendUniform("_view", context.View);
            dispatcher.SendUniform("_modelView", context.View * context.Model);
            dispatcher.SendUniform("_projection", context.Projection);
            dispatcher.SendUniform("_lPos", new Vector4(0, 100, 0, 1));
            dispatcher.SendUniform("_lColor", new Color3(1, 1, 1));

            ref var material = ref Unsafe.NullRef<PbrMaterialData>();
            if(context.Target.TryGetComponent<PbrMaterial>(out var m)) {
                material = ref m.Data;
            }
            else {
                material = new PbrMaterialData();
            }
            dispatcher.SendUniform("_metallic", (float)material.Metallic);
            dispatcher.SendUniform("_albedo", material.Albedo);
            dispatcher.SendUniform("_roughness", (float)material.Roughness);
        }

        protected override ShaderSource GetShaderSource(in ShaderGetterContext context)
        {
            return new()
            {
                OnlyContainsConstLiteralUtf8 = true,
                VertexShader =
"""
#version 410
in vec3 _vPos;
in vec3 _vNormal;
in vec2 _vUV;
out vec3 _pos;
out vec3 _normal;
out vec2 _uv;
uniform mat4 _modelView;
uniform mat4 _projection;

void main()
{
    _pos = _vPos;
    _normal = _vNormal;
    _uv = _vUV;
    gl_Position = _projection * _modelView * vec4(_vPos, 1.0);
}
"""u8,
                FragmentShader =
"""
#version 410
#define m_float mediump float
#define m_vec2 mediump vec2
#define m_vec3 mediump vec3
#define m_vec4 mediump vec4
#define h_float highp float
#define h_vec2 highp vec2
#define h_vec3 highp vec3
#define h_vec4 highp vec4

const float INV_PI = 1.0 / 3.1415926;
const float DielectricF0 = 0.04;
in vec3 _pos;
in vec3 _normal;
in vec2 _uv;
out m_vec4 _fragColor;
uniform mat4 _view;
uniform mat4 _modelView;
uniform vec4 _lPos;
uniform vec3 _lColor;
uniform vec3 _albedo;
uniform float _metallic;
uniform float _roughness;

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

void main()
{
    vec3 vPosView = (_modelView * vec4(_pos, 1.0)).xyz;                        // vertex pos in eye space
    vec3 v = -normalize(vPosView);
    vec3 n = normalize(transpose(inverse(mat3(_modelView))) * _normal); // normal in eye space, normalized
    vec3 l = normalize((_view * _lPos).xyz - vPosView); // light vec in eye space, normalized    
    vec3 h = normalize(v + l);

    float dot_nv = abs(dot(n, v));
    float dot_nl = max(0.0, dot(n, l));
    float dot_nh = max(0.0, dot(n, h));
    float dot_lh = max(0.0, dot(l, h));
    float reflectivity = mix(DielectricF0, 1.0, _metallic);
    vec3 f0 = mix(vec3(DielectricF0, DielectricF0, DielectricF0), _albedo, _metallic);

    // Diffuse
    float diffuseTerm = Fd_Burley(dot_nv, dot_nl, dot_lh, _roughness) * dot_nl;
    vec3 diffuse = (1.0 - reflectivity) * diffuseTerm * _lColor * _albedo;

    // Specular
    float alpha = _roughness * _roughness;
    float V = V_SmithGGXCorrelated(dot_nl, dot_nv, alpha);
    float D = D_GGX(n, h, dot_nh, _roughness);
    vec3 F = F_Schlick(f0, dot_lh);
    vec3 specular = V * D * F * dot_nl * _lColor;
    specular = max(vec3(0.0, 0.0, 0.0), specular);
    
    _fragColor = vec4(specular + diffuse, 1.0);
}
"""u8,
            };
        }
    }
}
