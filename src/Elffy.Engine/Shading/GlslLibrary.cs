#nullable enable

namespace Elffy.Shading
{
    public static class GlslLibrary
    {
        /// <summary>Get random value from value</summary>
        public const string Rand =
@"
#ifndef SOURCE_RAND
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

        public const string NegativeColor =
@"
#ifndef SOURCE_NEGATIVECOLOR
#define SOURCE_NEGATIVECOLOR 1
vec4 NegativeColor(vec4 c)
{
    return vec4(1-c.r, 1-c.g, 1-c.b, c.a);
}
vec3 NegativeColor(vec3 c)
{
    return vec3(1-c.r, 1-c.g, 1-c.b);
}
#endif
";

        /// <summary>Get xorshift next value from seed</summary>
        public const string Xorshift =
@"
#ifndef SOURCE_XORSHIFT
#define SOURCE_XORSHIFT 1
uint Xorshift(uint seed){seed ^= (seed << 13);seed ^= (seed >> 17);seed ^= (seed << 5);return seed;}
#endif
";

        /// <summary>Make color grayscale</summary>
        public const string MakeGray =
@"
#ifndef SOURCE_MAKEGRAY
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
@"
#ifndef SOURCE_MAKEGRAYVALUE
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
    }
}
