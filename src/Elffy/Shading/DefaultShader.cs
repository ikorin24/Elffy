#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.Shading
{
    internal sealed class DefaultShader : Shader
    {
        internal DefaultShader()
        {
        }

        ~DefaultShader() => Dispose(false);

        protected override void Dispose(bool disposing) => base.Dispose(disposing);

        protected override void SendUniforms(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            ThrowIfEmptyProgram();
            // TODO: 実行テスト用 本来はライティングとマテリアルの情報も引数で渡されるようにする
            SendUniformLight(new Vector4(-1f, -1f, 0f, 0f), new Color4(0.2f, 0.2f, 0.2f), Color4.White, Color4.White);
            SendUniformMaterial(Material.Default);
            SendUniformModelViewProjection(model, view, projection);
        }

        private void SendUniformLight(in Vector4 position, in Color4 ambient, in Color4 diffuse, in Color4 specular)
        {
            ThrowIfEmptyProgram();
            SendUniformNoCheck("LightPosition", position);
            SendUniformNoCheck("LightAmbient", Unsafe.As<Color4, Vector3>(ref Unsafe.AsRef(ambient)));
            SendUniformNoCheck("LightDiffuse", Unsafe.As<Color4, Vector3>(ref Unsafe.AsRef(diffuse)));
            SendUniformNoCheck("LightSpecular", Unsafe.As<Color4, Vector3>(ref Unsafe.AsRef(specular)));
        }

        private void SendUniformMaterial(in Material material)
        {
            ThrowIfEmptyProgram();
            SendUniformNoCheck("MaterialAmbient", Unsafe.As<Color4, Vector3>(ref Unsafe.AsRef(material.Ambient)));
            SendUniformNoCheck("MaterialDiffuse", Unsafe.As<Color4, Vector3>(ref Unsafe.AsRef(material.Diffuse)));
            SendUniformNoCheck("MaterialSpecular", Unsafe.As<Color4, Vector3>(ref Unsafe.AsRef(material.Specular)));
            SendUniformNoCheck("MaterialShininess", material.Shininess);
        }

        private void SendUniformModelViewProjection(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            ThrowIfEmptyProgram();
            SendUniformNoCheck("ModelViewMatrix", view * model);
            SendUniformNoCheck("ProjectionMatrix", projection);
        }

        protected override string VertexShaderSource() => VertSource;

        protected override string FragmentShaderSource() => FragSource;

        private static readonly string VertSource =
@"#version 440

layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec3 VertexNormal;
layout (location = 2) in vec3 VertexColor;
layout (location = 3) in vec2 Texcoord;

uniform vec4 LightPosition;
uniform vec3 LightAmbient;
uniform vec3 LightDiffuse;
uniform vec3 LightSpecular;

uniform vec3 MaterialAmbient;
uniform vec3 MaterialDiffuse;
uniform vec3 MaterialSpecular;
uniform float MaterialShininess;

uniform mat4 ModelViewMatrix;
uniform mat4 ProjectionMatrix;

out vec3 LightIntensity;

mat3 NormalMatrix;
mat4 MVP;


void getEyeSpace( out vec3 norm, out vec4 position )
{
  norm = normalize( NormalMatrix * VertexNormal);
  position = ModelViewMatrix * vec4(VertexPosition,1.0);
}

vec3 phongModel( vec4 position, vec3 norm )
{
    vec3 s = normalize(vec3(LightPosition - position));
    vec3 v = normalize(-position.xyz);
    vec3 r = reflect( -s, norm );
    vec3 ambient = LightAmbient * MaterialAmbient;
    float sDotN = max( dot(s,norm), 0.0 );
    vec3 diffuse = LightDiffuse * MaterialDiffuse * sDotN;
    vec3 spec = vec3(0.0);
    if( sDotN > 0.0 )
        spec = LightSpecular * MaterialSpecular * pow( max( dot(r,v), 0.0 ), MaterialShininess );
    return ambient + diffuse + spec;
}

void main()
{
    MVP = ProjectionMatrix * ModelViewMatrix;

    /*
    モデルビュー行列の上3x3の逆行列の転置行列
    これも多分CPU側でやったほうがいい
    */
    NormalMatrix = transpose(inverse(mat3(ModelViewMatrix)));


    vec3 eyeNorm;
    vec4 eyePosition;
    // Get the position and normal in eye space
    getEyeSpace(eyeNorm, eyePosition);

    // Evaluate the lighting equation.
    LightIntensity = phongModel( eyePosition, eyeNorm );

    gl_Position = MVP * vec4(VertexPosition,1.0);
}	

";

        private const string FragSource =
@"#version 440

out vec4 FragColor;

in vec3 LightIntensity;

void main()
{
	FragColor = vec4(LightIntensity, 1.0);
}
";

    }
}
