﻿#nullable enable
using Elffy;
using Elffy.Shading;
using System;

namespace Sandbox
{
    public sealed class SkyShader : RenderingShader
    {
        public static readonly SkyShader Instance = new SkyShader();

        private SkyShader()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
        {
            definition.Map(context.VertexType, "vPos", VertexFieldSemantics.Position);
            definition.Map(context.VertexType, "vUV", VertexFieldSemantics.UV);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
        {
            dispatcher.SendUniform("mvp", context.Projection * context.View * context.Model);
        }

        protected override ShaderSource GetShaderSource(in ShaderGetterContext context) => new()
        {
            OnlyContainsConstLiteralUtf8 = true,
            VertexShader =
            """
            #version 410
            precision highp float;

            in vec3 vPos;
            in vec2 vUV;
            out vec2 UV;

            uniform mat4 mvp;

            void main()
            {
                UV = vUV;
                gl_Position = mvp * vec4(vPos, 1.0);
            }
            """u8,
            FragmentShader =
            """
            #version 410
            precision highp float;

            in vec2 UV;
            out vec4 fragColor;

            float RangeFilter(float range_min, float range_max, float value)
            {
                return step(range_min, value) * (1.0 - step(range_max, value));
            }


            vec3 Linear(float x0, float x1, vec3 y0, vec3 y1, float x)
            {
                return (y1-y0)/(x1-x0)*(x-x0)+y0;
            }

            void main()
            {
                const int num = 5;
                vec3[num] colors = vec3[](
                    vec3(0.3686, 0.349, 0.3294),
                    vec3(0.3686, 0.349, 0.3294),
                    vec3(0.6314, 0.9882, 1.0),
                    vec3(0.4902, 0.8902, 1.0),
                    vec3(0.0902, 0.4863, 1.0)
                );
                float[num] offsets = float[]( 0.0, 0.498, 0.5, 0.54, 1.0 );
                vec3 color = vec3(0.0);
                for(int i = 0;i < num-1; i++) {
                    color += RangeFilter(offsets[i], offsets[i+1], UV.y) * Linear(offsets[i], offsets[i+1], colors[i], colors[i+1], UV.y);
                }
                fragColor = vec4(color, 1.0);
            }
            """u8,
        };
    }
}
