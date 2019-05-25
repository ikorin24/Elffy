using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public class Material
    {
        private const float DEFAULT_SHININESS = 30f;

        public Color4 Ambient { get; set; }

        public Color4 Diffuse { get; set; }
        public Color4 Specular { get; set; }
        public float Shininess { get; set; }

        public Material() :this(Color4.White, Color4.Black, Color4.Black, DEFAULT_SHININESS) { }

        public Material(Color4 color) : this(color, Color4.Black, Color4.Black, DEFAULT_SHININESS) { }
        public Material(Color4 color, float opacity) : 
            this(new Color4(color.R, color.G, color.B, opacity), 
                 new Color4(color.R, color.G, color.B, opacity), 
                 new Color4(color.R, color.G, color.B, opacity), DEFAULT_SHININESS) { }

        public Material(Color4 ambient, Color4 diffuse, Color4 specular, float shininess)
        {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
        }

        internal void Apply()
        {
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, Ambient);
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, Diffuse);
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, Specular);
            GL.Material(MaterialFace.Front, MaterialParameter.Shininess, Shininess);
        }
    }
}
