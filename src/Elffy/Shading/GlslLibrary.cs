#nullable enable

namespace Elffy.Shading
{
    public static class GlslLibrary
    {
        /// <summary>Get random value from value</summary>
        public const string Rand =
@"#ifndef SOURCE_RAND
#define SOURCE_RAND 1
highp float Rand(vec2 pos)
{
    highp float a = 12.9898;
    highp float b = 78.233;
    highp float c = 43758.5453;
    highp float dt= dot(pos.xy ,vec2(a,b));
    highp float sn= mod(dt,3.14);
    return fract(sin(sn) * c);
}

highp float Rand(float x){return Rand(vec2(x, 0.0));}
#endif
";
        /// <summary>Get xorshift next value from seed</summary>
        public const string Xorshift =
@"#ifndef SOURCE_XORSHIFT
#define SOURCE_XORSHIFT 1
uint Xorshift(uint seed){seed ^= (seed << 13);seed ^= (seed >> 17);seed ^= (seed << 5);return seed;}
#endif
";

        /// <summary>Make color grayscale</summary>
        public const string MakeGray =
@"#ifndef SOURCE_MAKEGRAY
#define SOURCE_MAKEGRAY 1
vec3 MakeGray(vec3 c)
{
    vec3 grayFactor = vec3(0.299, 0.587, 0.114);
    float gray = dot(c, grayFactor);
    return vec3(gray, gray, gray);
}
vec4 MakeGray(vec4 c)
{
    vec3 grayFactor = vec3(0.299, 0.587, 0.114);
    float gray = dot(c.rgb, grayFactor);
    return vec4(gray, gray, gray, c.a);
}
#endif
";
        public const string MakeGrayValue =
@"#ifndef SOURCE_MAKEGRAYVALUE
#define SOURCE_MAKEGRAYVALUE 1
float MakeGrayValue(vec3 c)
{
    return dot(c, vec3(0.299, 0.587, 0.114));
}
float MakeGrayValue(vec4 c)
{
    return dot(c.rgb, vec3(0.299, 0.587, 0.114));
}
#endif
";

        /// <summary>
        /// FXAA; Fast Approximate Anti-Aliasing<para/>
        /// vec4 FXAA(sampler2D sampler, vec2 uv, vec2 inversedScreenSize)<para/>
        /// </summary>
        public const string FXAA =
MakeGray + 
MakeGrayValue +
@"#ifndef SOURCE_FXAA
#define SOURCE_FXAA 1
vec4 FXAA(sampler2D sampler, vec2 uv, vec2 inversedScreenSize)
{
    vec4 original = textureLod(sampler, uv, 0);
    float m = MakeGrayValue(original);
    float n = MakeGrayValue(textureLod(sampler, uv+vec2(0,  1)*inversedScreenSize, 0));
    float s = MakeGrayValue(textureLod(sampler, uv+vec2(0, -1)*inversedScreenSize, 0));
    float w = MakeGrayValue(textureLod(sampler, uv+vec2(-1, 0)*inversedScreenSize, 0));
    float e = MakeGrayValue(textureLod(sampler, uv+vec2(1,  0)*inversedScreenSize, 0));

    float nw = (n.r + w.r) * 0.5;
    float ne = (n.r + e.r) * 0.5;
    float sw = (s.r + w.r) * 0.5;
    float se = (s.r + e.r) * 0.5;

    float maxLuma = max(nw, max(ne, max(sw, max(se, m.r))));
    float minLuma = min(nw, min(ne, min(sw, min(se, m.r))));
    float contrast = maxLuma - minLuma;

    // 'dir' is perpendicular to luma edge
    vec2 dir = vec2(-(nw + ne) + (sw - se), (nw + sw) - (ne + se));

    const float minThreshold = 0.15;
    const float threshold = 0.05;
    if(contrast < max(minThreshold, maxLuma * threshold) || (abs(dir.x) < 0.01 && abs(dir.y) < 0.01)) {
        // Early return pixel color. (no anti-aliasing)
        return original;
    }

    const float sharpness = 1;
    float scale = 1.0 / (min(abs(dir.x), abs(dir.y)) * sharpness + 0.001);
    scale = clamp(scale, 0, 2.0);
    dir = normalize(dir) * scale;

    vec4[4] colorSrc = vec4[]
    (
        textureLod(sampler, uv + dir*(-0.5+0.000)*inversedScreenSize, 0),
        textureLod(sampler, uv + dir*(-0.5+0.333)*inversedScreenSize, 0),
        textureLod(sampler, uv + dir*(-0.5+0.667)*inversedScreenSize, 0),
        textureLod(sampler, uv + dir*(-0.5+1.000)*inversedScreenSize, 0)
    );
    vec4 mixedColor1 = (colorSrc[1] + colorSrc[2]) * 0.5;
    vec4 mixedColor2 = (colorSrc[0] + colorSrc[3]) * 0.25 + mixedColor1 * 0.5;
    
    float lumaMixedColor2 = MakeGrayValue(mixedColor2);
    if(lumaMixedColor2 >= minLuma && lumaMixedColor2 <= maxLuma){
        return mixedColor2;
    }
    else{
        return mixedColor1;
    }
}
#endif
";
    }
}
