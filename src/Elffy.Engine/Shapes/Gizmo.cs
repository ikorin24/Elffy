#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Shading;
using System;

namespace Elffy.Shapes;

public class Gizmo : Renderable
{
    public Gizmo()
    {
        Activating.Subscribe(static (sender, ct) =>
        {
            var self = SafeCast.As<Gizmo>(sender);
            PrimitiveMeshProvider<VertexPosNormal>.GetArrow(self, static (self, vertices, indices) => self.LoadMesh(vertices, indices));
            return UniTask.CompletedTask;
        });
        InstancingCount = 3;
        Shader = GizmoShader.Instance;
        HasShadow = false;
    }

    private sealed class GizmoShader : RenderingShader
    {
        private static GizmoShader? _instance;

        /// <summary>Get singleton instance</summary>
        public static GizmoShader Instance => _instance ??= new();

        private GizmoShader()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
        {
            definition.Map(context.VertexType, "_pos", VertexSpecialField.Position);
            definition.Map(context.VertexType, "_normal", VertexSpecialField.Normal);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
        {
            dispatcher.SendUniform("_model", context.Model);
            dispatcher.SendUniform("_viewProjection", context.Projection * context.View);
        }

        protected override ShaderSource GetShaderSource(in ShaderGetterContext context) => new()
        {
            OnlyContainsConstLiteralUtf8 = true,
            VertexShader =
            """
            #version 410
            in vec3 _pos;
            in vec3 _normal;
            out vec3 _color;
            uniform mat4 _model;
            uniform mat4 _viewProjection;
            const vec3[] ColorTable = vec3[](
                vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1)
            );
            const mat4[] RotTable = mat4[](
                mat4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1),
                mat4(0,1,0,0,-1,0,0,0,0,0,1,0,0,0,0,1),
                mat4(0,0,1,0,0,1,0,0,-1,0,0,0,0,0,0,1)
            );

            void main()
            {
                mat4 rotModel = _model * RotTable[gl_InstanceID];
                gl_Position = _viewProjection * rotModel * vec4(_pos, 1.0);
                vec3 nWorld = (rotModel * vec4(_normal, 0)).xyz;
                float colorFactor = dot(vec3(0, 1, 0), (nWorld + vec3(0, 1, 0)) * 0.5) * 0.5 + 0.5;
                _color = ColorTable[gl_InstanceID] * colorFactor;
            }
            """u8,
            FragmentShader =
            """
            #version 410
            in vec3 _color;
            out vec4 _fragColor;
            void main()
            {
                _fragColor = vec4(_color, 1);
            }
            """u8,
        };
    }
}
