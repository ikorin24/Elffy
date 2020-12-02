#nullable enable
using System;
using Elffy.Components;
using Elffy.Diagnostics;
using Elffy.OpenGL;
using Elffy.Shading;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Core
{
    [ShaderTargetVertexType(typeof(Vertex))]
    public sealed class DeferedRenderingShaderSource : ShaderSource
    {
        private static DeferedRenderingShaderSource? _instance;
        public static DeferedRenderingShaderSource Instance => _instance ??= new DeferedRenderingShaderSource();

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        private DeferedRenderingShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<Vertex>(nameof(Vertex.Position), "_vPos");
            definition.Map<Vertex>(nameof(Vertex.Normal), "_vNormal");
            definition.Map<Vertex>(nameof(Vertex.TexCoord), "_vUV");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var material = target.GetComponent<Material>();
            uniform.Send("_diffuse", material.Diffuse);
            uniform.Send("_specular", material.Specular.R);
            uniform.Send("_model", model);
        }

        private const string VertSource =
@"#version 440
in vec3 _vPos;
in vec3 _vNormal;
in vec2 _vUV;
in vec3 _pos;
in vec3 _normal;
in vec2 _uv;
void main()
{
    _pos = _vPos;
    _normal = _vNormal;
    _uv = _vUV;
    gl_Position = vec4(_vPos, 1.0);
}";

        private const string FragSource =
@"#version 440
in vec3 _pos;
in vec3 _normal;
in vec2 _uv;
uniform mat4 _model;
uniform vec4 _diffuse;
uniform float _specular;
layout (location = 0) out vec3 _gPosition;
layout (location = 1) out vec3 _gNormal;
layout (location = 2) out vec4 _gColor;

void main()
{    
    _gPosition = _model * _pos;     // position in world coordinate
    _gNormal = transpose(inverse(mat3(_model))) * _normal;  // normal in world coordinate
    _gNormal = _normal;
    _gColor.rgb = _diffuse.rgb;
    _gColor.a = _specular;
}  
";
    }

    //internal struct GBuffer
    //{
    //    private FBO _fbo;
    //    private TextureObject Position;
    //    private TextureObject Normal;
    //    private TextureObject Color;

    //    public static void BindGBuffers(in GBuffer gBuffer)
    //    {
    //        TextureObject.Bind2D(gBuffer.Position, TextureUnitNumber.Unit0);
    //        TextureObject.Bind2D(gBuffer.Normal, TextureUnitNumber.Unit1);
    //        TextureObject.Bind2D(gBuffer.Color, TextureUnitNumber.Unit2);
    //    }

    //    public static unsafe void Create(in Vector2i screenSize, out GBuffer gBuffer)
    //    {
    //        gBuffer = default;

    //        gBuffer._fbo = FBO.Create();
    //        FBO.Bind(gBuffer._fbo, FBO.Target.FrameBuffer);
    //        const int bufCount = 3;
    //        var bufs = stackalloc DrawBuffersEnum[bufCount];

    //        gBuffer.Position = TextureObject.Create();
    //        TextureObject.Bind2D(gBuffer.Position, TextureUnitNumber.Unit0);
    //        TextureObject.Image2D(screenSize, (Color4*)null, TextureObject.InternalFormat.Rgba16f);
    //        TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
    //        TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor);
    //        FBO.SetTexture2DBuffer(gBuffer.Position, FBO.Attachment.ColorAttachment0);
    //        bufs[0] = DrawBuffersEnum.ColorAttachment0;

    //        gBuffer.Normal = TextureObject.Create();
    //        TextureObject.Bind2D(gBuffer.Normal, TextureUnitNumber.Unit0);
    //        TextureObject.Image2D(screenSize, (Color4*)null, TextureObject.InternalFormat.Rgba16f);
    //        TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
    //        TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor);
    //        FBO.SetTexture2DBuffer(gBuffer.Normal, FBO.Attachment.ColorAttachment1);
    //        bufs[1] = DrawBuffersEnum.ColorAttachment1;

    //        gBuffer.Color = TextureObject.Create();
    //        TextureObject.Bind2D(gBuffer.Color, TextureUnitNumber.Unit0);
    //        TextureObject.Image2D(screenSize, (ColorByte*)null, TextureObject.InternalFormat.Rgba);
    //        TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
    //        TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor);
    //        FBO.SetTexture2DBuffer(gBuffer.Color, FBO.Attachment.ColorAttachment2);
    //        bufs[2] = DrawBuffersEnum.ColorAttachment2;

    //        if(!FBO.CheckStatus(out var status)) {
    //            throw new Exception(status.ToString());
    //        }

    //        GL.DrawBuffers(bufCount, bufs);
    //    }
    //}
}
