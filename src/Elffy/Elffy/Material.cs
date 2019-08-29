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
    public sealed class Material
    {
        private const float DEFAULT_SHININESS = 30f;
        private static bool _isClear;

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
            _isClear = false;
        }

        internal static void ClearMaterial()
        {
            if(!_isClear) {
                GL.Material(MaterialFace.Front, MaterialParameter.Ambient, new Color4(0.2f, 0.2f, 0.2f, 1f));
                GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, new Color4(0.8f, 0.8f, 0.8f, 1f));
                GL.Material(MaterialFace.Front, MaterialParameter.Specular, new Color4(0f, 0f, 0f, 1f));
                GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 0);
                _isClear = true;
            }
        }
    }
}
