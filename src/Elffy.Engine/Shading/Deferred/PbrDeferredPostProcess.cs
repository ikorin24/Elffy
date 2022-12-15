#nullable enable
using Elffy.Components;
using Elffy.Mathematics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Shading.Deferred;

internal sealed class PbrDeferredPostProcess : PostProcess
{
    private readonly IGBufferProvider _gBufferProvider;
    private FloatDataTexture? _ssaoKernel;
    private float _shadowMapBias = 0.001f;
    public float ShadowMapBias
    {
        get => _shadowMapBias;
        set => _shadowMapBias = float.Max(value, 0);
    }

    internal PbrDeferredPostProcess(IGBufferProvider gBufferProvider)
    {
        _gBufferProvider = gBufferProvider;
        Attached.Subscribe(sender =>
        {
            SafeCast.As<PbrDeferredPostProcess>(sender).OnAttached();
        });
        Detached.Subscribe(sender =>
        {
            SafeCast.As<PbrDeferredPostProcess>(sender).OnDetached();
        });
    }

    private void OnAttached()
    {
        var ssaoKernel = new FloatDataTexture();
        const int KernelSize = 64;
        Span<Vector4> kernel = stackalloc Vector4[KernelSize];
        var random = new Xorshift32();

        // create kernel
        for(int i = 0; i < kernel.Length; i++) {
            var vec = new Vector3(
                random.Single() * 2f - 1f,
                random.Single() * 2f - 1f,
                random.Single());
            var s = i / (float)KernelSize;
            var scale = Lerp(0.1f, 1.0f, s * s);
            vec.Normalize();
            vec *= scale;
            kernel[i] = new Vector4(vec, 1f);
        }

        ssaoKernel.LoadAsPowerOfTwo(kernel);
        _ssaoKernel = ssaoKernel;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Lerp(float a, float b, float f)
        {
            return a + f * (b - a);
        }
    }

    private void OnDetached()
    {
        _ssaoKernel?.Dispose();
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in PostProcessRenderContext context)
    {
        var gBuffer = _gBufferProvider.GetGBufferData();
        var screen = context.Screen;
        var lights = context.RenderPipeline.Lights;

        var camera = screen.Camera;
        var view = camera.View;
        dispatcher.SendUniform("_viewInv", view.Inverted());
        dispatcher.SendUniform("_projection", camera.Projection);
        dispatcher.SendUniformTexture2D("_mrt0", gBuffer.Mrt[0], 0);
        dispatcher.SendUniformTexture2D("_mrt1", gBuffer.Mrt[1], 1);
        dispatcher.SendUniformTexture2D("_mrt2", gBuffer.Mrt[2], 2);

        var light = lights.IsEmpty switch
        {
            false => (
                Exists: true,
                Position: lights[0].Position,
                Color: lights[0].Color,
                LightMatrixData: lights[0].ShadowMap.LightMatricesDataTexture,
                ShadowMap: lights[0].ShadowMap.LightDepthTexture,
                CascadeCount: lights[0].ShadowMap.CascadeCount
            ),
            true => default,
        };
        dispatcher.SendUniform("_cascadeCount", light.CascadeCount);
        dispatcher.SendUniform("_hasLight", light.Exists);
        dispatcher.SendUniform("_lightPos", view * light.Position); // light pos in eye space
        dispatcher.SendUniform("_lightColor", light.Color);
        dispatcher.SendUniform("_shadowMapBias", _shadowMapBias);
        dispatcher.SendUniformTexture1D("_lightMatData", light.LightMatrixData, 3);
        dispatcher.SendUniformTexture2DArray("_shadowMap", light.ShadowMap, 4);

        Debug.Assert(_ssaoKernel is not null);
        dispatcher.SendUniformTexture1D("_ssaoKernel", _ssaoKernel.TextureObject, 5);
    }

    protected override PostProcessSource GetSource(in PostProcessGetterContext context) => new PostProcessSource
    {
        // index  | R           | G            | B           | A         |
        // ----
        // mrt[0] | pos.x       | pos.y        | pos.z       | 1         |
        // mrt[1] | normal.x    | normal.y     | normal.z    | roughness |
        // mrt[2] | baseColor.r | baseColor.g  | baseColor.b | metallic  |
        // mrt[3] | 0           | 0            | 0           | 0         |
        // mrt[4] | 0           | 0            | 0           | 0         |

        FragmentShader = """
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
        const vec3 IndirectDiffuse = vec3(0.3, 0.3, 0.3);
        const vec3 IndirectSpecular = vec3(0.3, 0.3, 0.3);

        in V2f
        {
            vec2 uv;
        } _v2f;
        uniform mat4 _viewInv;
        uniform mat4 _projection;
        uniform sampler2D _mrt0;
        uniform sampler2D _mrt1;
        uniform sampler2D _mrt2;

        uniform bool _hasLight;
        uniform int _cascadeCount;
        uniform vec4 _lightPos;
        uniform vec4 _lightColor;
        uniform sampler1D _lightMatData;
        uniform float _shadowMapBias;

        uniform sampler2DArray _shadowMap;
        uniform sampler1D _ssaoKernel;
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

        float CalcShadow(sampler1D lightMatData, vec4 posWorld, sampler2DArray shadowMap, float bias)
        {
            for(int cascade = 0; cascade < _cascadeCount; ++cascade) {
                mat4 lightMat = mat4(
                    texelFetch(lightMatData, cascade * 4,     0),
                    texelFetch(lightMatData, cascade * 4 + 1, 0),
                    texelFetch(lightMatData, cascade * 4 + 2, 0),
                    texelFetch(lightMatData, cascade * 4 + 3, 0));
                vec3 shadowMapNDC = ToVec3(lightMat * posWorld);
                if(all(lessThanEqual(abs(shadowMapNDC), vec3(1.0, 1.0, 1.0)))) {
                    vec3 shadowMapUV = shadowMapNDC * 0.5 + 0.5;
                    float d = textureLod(shadowMap, vec3(shadowMapUV.xy, cascade), 0).x;
                    float shadow = 1.0 - step(shadowMapUV.z - bias, d);
                    return shadow;
                }
            }
            return 0;
        }

        //float Random1To1(uint p){
        //    const uint k = 0x456789abu;
        //    uint n = floatBitsToUint(p);
        //    n ^= (n << 9);
        //    n ^= (n >> 1);
        //    n ^= (n << 1);
        //    n *= 0x456789abu;
        //    return float(n) / float(0xffffffffu);
        //}
        vec2 Random2To2(vec2 p){
            const uvec2 k = uvec2(0x456789abu, 0x6789ab45u);
            const vec2 s = 1.0 / vec2(0xffffffffu, 0xffffffffu);
            uvec2 n = floatBitsToUint(p);
            n ^= (n.yx << 9);
            n ^= (n.yx >> 1);
            n *= k;
            n ^= (n.yx << 1);
            n *= k;
            return vec2(n) * s;
        }
        //vec3 Random3To3(vec3 p) {
        //    const uvec3 k = uvec3(0x456789abu, 0x6789ab45u, 0x89ab4567u);
        //    const vec3 s = 1.0 / vec3(0xffffffffu, 0xffffffffu, 0xffffffffu);
        //    uvec3 n = floatBitsToUint(p);
        //    n ^= (n.yzx << 9);
        //    n ^= (n.yzx >> 1);
        //    n *= k;
        //    n ^= (n.yzx << 1);
        //    n *= k;
        //    return vec3(n) * s;
        //}

        float Ssao(vec3 pos, vec3 normal, sampler1D kernel)
        {
            const int KernelSize = 64;
            const float KernelSizeInv = 1.0 / KernelSize;
            const float Radius = 0.5;
            const float SsaoBias = 0.1;
            vec3 randomVec = vec3(Random2To2(gl_FragCoord.xy) * 2 - 1, 0);
            vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
            vec3 bitangent = cross(normal, tangent);
            mat3 TBN = mat3(tangent, bitangent, normal);
            float occlusion = 0.0;
            for(int i = 0; i < KernelSize; ++i) {
                vec3 samplePos = pos + TBN * texelFetch(kernel, i, 0).xyz * Radius;
                vec4 offset = _projection * vec4(samplePos, 1.0);
                offset.xyz /= offset.w;
                offset.xyz = offset.xyz * 0.5 + 0.5;
                float sampleDepth = textureLod(_mrt0, offset.xy, 0).z;
                float rangeCheck = smoothstep(0.0, 1.0, Radius / abs(pos.z - sampleDepth));
                occlusion += step(samplePos.z + SsaoBias, sampleDepth) * rangeCheck;
            }
            float result = 1.0 - occlusion * KernelSizeInv;
            return result;
        }

        void main()
        {
            vec4 mrt0Value = textureLod(_mrt0, _v2f.uv, 0);
            if(mrt0Value.w == 0) {
                gl_FragDepth = 1;
                discard;
                return;
            }
            vec4 mrt1Value = textureLod(_mrt1, _v2f.uv, 0);
            vec4 mrt2Value = textureLod(_mrt2, _v2f.uv, 0);
            vec3 baseColor = mrt2Value.rgb;
            vec3 pos = mrt0Value.xyz;    // pos in eye space
            vec4 dncPos = _projection * vec4(pos, 1);    // device-normalized-coordinate position
            gl_FragDepth = (dncPos.z / dncPos.w) * 0.5 + 0.5;

            float metallic = mrt2Value.a;
            float roughness = mrt1Value.a;
            float alpha = roughness * roughness;
            vec3 v = -normalize(pos);                       // eye direction in eye space, normalized
            vec3 n = mrt1Value.rgb;                         // normal direction in eye space, normalized
            float dot_nv = abs(dot(n, v));
            float reflectivity = mix(DielectricF0, 1.0, metallic);
            vec3 f0 = mix(vec3(DielectricF0, DielectricF0, DielectricF0), baseColor, metallic);

            vec3 fragColor = vec3(0.0, 0.0, 0.0);
            if(_hasLight) {
                vec3 lColor = _lightColor.rgb;
                vec4 lPos = _lightPos;              // light pos in eye space

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
                float bias = clamp(_shadowMapBias * tan(acos(dot_nl)), 0.0, _shadowMapBias * 10);
                float shadow = CalcShadow(_lightMatData, _viewInv * vec4(pos, 1), _shadowMap, bias);
                fragColor += (diffuse + specular) * (1.0 - shadow);
            }

            float ssao = Ssao(pos, n, _ssaoKernel);

            // indirect diffuse
            fragColor += (1.0 - reflectivity) * IndirectDiffuse * baseColor;

            // indirect specular
            float f90 = max(0, min(1, 1 - roughness + reflectivity));
            fragColor += 1.0 / (alpha * alpha + 1.0) * FresnelSchlick(f0, vec3(f90, f90, f90), dot_nv) * IndirectSpecular;
    
            _fragColor = vec4(fragColor * ssao, 1.0);
        }
        """u8
    };
}
