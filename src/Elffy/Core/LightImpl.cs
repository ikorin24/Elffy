#nullable enable
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Core
{
    /// <summary>
    /// Implementation of <see cref="ILight"/>
    /// </summary>
    /// <remarks>
    /// This class is minimum implementation of <see cref="ILight"/>. There are no argument checking and object state checking.
    /// </remarks>
    internal class LightImpl
    {
        /// <summary>
        /// The position of this light.<para/>
        /// [NOTE] <para/>
        /// The position of a light is four dimentional like (x, y, z, w). <para/>
        /// Actual position is three dimentional, that is (x/w, y/w, z/w). <para/>
        /// w == 0 means the infinite point directed by (x, y, z). That is Direct light. <para/>
        /// </summary>
        public Vector4 Position { get; set; }

        /// <summary>get or set whether the light is activated</summary>
        public bool IsActivated { get; set; }

        /// <summary>get or set ambient value of this light</summary>
        public Color4 Ambient { get; set; }
        /// <summary>get or set diffuse value of this light</summary>
        public Color4 Diffuse { get; set; }
        /// <summary>get or set specular value of this light</summary>
        public Color4 Specular { get; set; }

        public LightName LightName { get; set; }

        /// <summary>whether this light is lit up</summary>
        public bool IsLitUp { get; set; }

        /// <summary>get whether this light is destroyed</summary>
        public bool IsDestroyed { get; set; }

        public void LightUp()
        {
            if(IsLitUp == false) {
                GL.Enable((EnableCap)LightName);
                GL.Light(LightName, LightParameter.Position, Position);
                GL.Light(LightName, LightParameter.Ambient, Ambient);
                GL.Light(LightName, LightParameter.Diffuse, Diffuse);
                GL.Light(LightName, LightParameter.Specular, Specular);
                IsLitUp = true;
            }
        }

        public void TurnOff()
        {
            if(IsLitUp) {
                GL.Disable((EnableCap)LightName);
                IsLitUp = false;
            }
        }

        public void Activate(ILight light)
        {
            IsActivated = true;
            Light.AddLight(light);
            LightUp();
        }

        public void Destroy(ILight light)
        {
            IsDestroyed = true;
            Light.RemoveLight(light);
            TurnOff();
        }
    }
}
