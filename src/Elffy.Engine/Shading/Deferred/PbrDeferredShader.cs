#nullable enable
namespace Elffy.Shading.Deferred;

public sealed class PbrDeferredShader : RenderingShader
{
    private Texture? _texture;
    private Color3 _baseColor;
    private float _metallic;
    private float _roughness;

    public ref Color3 BaseColor => ref _baseColor;
    public float Metallic { get => _metallic; set => _metallic = value; }
    public float Roughness { get => _roughness; set => _roughness = value; }
    public Texture? Texture { get => _texture; set => _texture = value; }

    public PbrDeferredShader()
    {
    }

    public PbrDeferredShader(Color3 albedo, float metallic, float roughness)
    {
        _baseColor = albedo;
        _metallic = metallic;
        _roughness = roughness;
    }

    protected override void OnProgramDisposed()
    {
        _texture?.Dispose();
        _texture = null;
        base.OnProgramDisposed();
    }

    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map(context.VertexType, "_vPos", VertexFieldSemantics.Position);
        definition.Map(context.VertexType, "_vNormal", VertexFieldSemantics.Normal);
        definition.Map(context.VertexType, "_vUV", VertexFieldSemantics.UV);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("_modelView", context.View * context.Model);
        dispatcher.SendUniform("_proj", context.Projection);
        dispatcher.SendUniform("_baseColorMetallic", new Color4(_baseColor, _metallic));
        dispatcher.SendUniform("_roughness", _roughness);
        var texture = _texture;
        if(texture != null) {
            dispatcher.SendUniformTexture2D("_tex", texture.TextureObject, 0);
        }
        dispatcher.SendUniform("_hasTexture", texture != null);
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
            uniform mat4 _modelView;
            uniform mat4 _proj;
            out vec3 _pos;
            out vec3 _normal;
            out vec2 _uv;
            void main()
            {
                vec4 pos4 = _modelView * vec4(_vPos, 1.0);
                _pos = pos4.xyz / pos4.w;
                _normal = normalize(transpose(inverse(mat3(_modelView))) * _vNormal);
                _uv = _vUV;
                gl_Position = _proj * pos4;
            }
            """u8,

            // index  | R           | G            | B           | A         |
            // ----
            // mrt[0] | pos.x       | pos.y        | pos.z       | 1         |
            // mrt[1] | normal.x    | normal.y     | normal.z    | roughness |
            // mrt[2] | baseColor.r | baseColor.g  | baseColor.b | metallic  |
            // mrt[3] | emmisive.r  | emmisive.g   | emmisive.b  | 0         |
            // mrt[4] | 0           | 0            | 0           | 0         |

            FragmentShader =
            """
            #version 410
            in vec3 _pos;
            in vec3 _normal;
            in vec2 _uv;
            uniform vec4 _baseColorMetallic;
            uniform float _roughness;
            uniform sampler2D _tex;
            uniform bool _hasTexture;
            layout (location = 0) out vec4 _mrt0;
            layout (location = 1) out vec4 _mrt1;
            layout (location = 2) out vec4 _mrt2;
            layout (location = 3) out vec4 _mrt3;
            layout (location = 4) out vec4 _mrt4;

            void main()
            {
                _mrt0 = vec4(_pos, 1.0);
                _mrt1 = vec4(_normal, _roughness);
                if(_hasTexture) {
                    _mrt2 = vec4(texture(_tex, _uv).rgb, _baseColorMetallic.a);
                    //_mrt2 = vec4(0, 1, 0, 0);
                }
                else {
                    _mrt2 = _baseColorMetallic;
                }
                _mrt3 = vec4(0, 0, 0, 0);
                _mrt4 = vec4(0, 0, 0, 0);
            }
            """u8,
        };
    }
}
