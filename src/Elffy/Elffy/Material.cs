#nullable enable
using System.Diagnostics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy
{
    /// <summary>Material of <see cref="Renderable"/> object</summary>
    [DebuggerDisplay("Ambient=({Ambient.R}, {Ambient.G}, {Ambient.B}, {Ambient.A}), Diffuse=({Diffuse.R}, {Diffuse.G}, {Diffuse.B}, {Diffuse.A}), Specular=({Specular.R}, {Specular.G}, {Specular.B}, {Specular.A}), Shininess={Shininess}")]
    public sealed class Material
    {
        private static Material? _current;

        /// <summary>Ambient color of material</summary>
        public Color4 Ambient { get; set; }
        /// <summary>Diffuse color of material</summary>
        public Color4 Diffuse { get; set; }
        /// <summary>Specular color of material</summary>
        public Color4 Specular { get; set; }
        /// <summary>Shininess intensity of material</summary>
        public float Shininess { get; set; }

        /// <summary>Default material. (If <see cref="Renderable.Material"/> is null, it is rendered by this material.)</summary>
        public static Material Default => Materials.Plain;

        /// <summary>constructor of specified color material</summary>
        /// <param name="color">color of material</param>
        public Material(Color4 color)
        {
            const float AMBIENT_RATIO = 0.4f;
            const float DIFFUSE_RATIO = 1f - AMBIENT_RATIO;
            Ambient = new Color4(color.R * AMBIENT_RATIO, color.G * AMBIENT_RATIO, color.B * AMBIENT_RATIO, color.A);
            Diffuse = new Color4(color.R * DIFFUSE_RATIO, color.G * DIFFUSE_RATIO, color.B * DIFFUSE_RATIO, color.A);
            Specular = Color4.Black;
            Shininess = 0;
        }

        /// <summary>constructor of specified colors and specified shininess material</summary>
        /// <param name="ambient"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="shininess"></param>
        public Material(Color4 ambient, Color4 diffuse, Color4 specular, float shininess)
        {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
        }

        internal void Apply()
        {
            if(this != _current) {
                _current = this;
                GL.Material(MaterialFace.Front, MaterialParameter.Ambient, Ambient);
                GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, Diffuse);
                GL.Material(MaterialFace.Front, MaterialParameter.Specular, Specular);
                GL.Material(MaterialFace.Front, MaterialParameter.Shininess, Shininess);
            }
        }
    }
}
