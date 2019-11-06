using Elffy.Exceptions;
using Elffy.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy
{
    /// <summary>Direct light class</summary>
    public sealed class DirectLight : IDestroyable
    {
        /// <summary>
        /// The position of this light.<para/>
        /// [NOTE] <para/>
        /// The position of a light is four dimentional like (x, y, z, w). <para/>
        /// Actual position is three dimentional, that is (x/w, y/w, z/w). <para/>
        /// w == 0 means the infinite point directed by (x, y, z). That is Direct light. <para/>
        /// </summary>
        private Vector4 _position;

        /// <summary>whether this light is lit up</summary>
        private bool _isLitUp;

        /// <summary>get whether this light is activated</summary>
        public bool IsActivated { get; private set; }

        /// <summary>get whether this light is destroyed</summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>light name</summary>
        internal LightName LightName { get; set; }

        /// <summary>get or set direction of this light</summary>
        public Vector3 Direction
        {
            get => -_position.Xyz;
            set
            {
                _position = new Vector4(-value);
            }
        }
        /// <summary>get or set ambient value of this light</summary>
        public Color4 Ambient { get; set; }
        /// <summary>get or set diffuse value of this light</summary>
        public Color4 Diffuse { get; set; }
        /// <summary>get or set specular value of this light</summary>
        public Color4 Specular { get; set; }
        /// <summary>get ID of this light</summary>
        public int ID => (int)(LightName - LightName.Light0);

        /// <summary>
        /// Create <see cref="DirectLight"/> instance.<para/>
        /// Direction = (-1,-1, 0) --- (X, Y, Z)<para/>
        /// [Ambient, Diffuse, Specular] = [(0, 0, 0, 1), (1, 1, 1, 1), (1, 1, 1, 1)] --- (R, G, B, A)<para/>
        /// </summary>
        public DirectLight() : this(new Vector3(-1f, -1f, 0f), Color4.Black, Color4.White, Color4.White) { }

        /// <summary>
        /// Create <see cref="DirectLight"/> instance of specified direction.<para/>
        /// [Ambient, Diffuse, Specular] = [(0, 0, 0, 1), (1, 1, 1, 1), (1, 1, 1, 1)] --- (R, G, B, A)<para/>
        /// </summary>
        /// <param name="direction">direction of the light</param>
        public DirectLight(Vector3 direction) : this(direction, Color4.Black, Color4.White, Color4.White) { }

        /// <summary>
        /// Create <see cref="DirectLight"/> instance of specified direction and specified diffuse &amp; specular.<para/>
        /// Ambient = (0, 0, 0, 1) --- (R, G, B, A)<para/>
        /// </summary>
        /// <param name="direction">direction of the light</param>
        /// <param name="color">diffuse and specular value</param>
        public DirectLight(Vector3 direction, Color4 color) : this(direction, Color4.Black, color, color) { }

        /// <summary>Create <see cref="DirectLight"/> instance of specified direction and specified ambient, diffuse, specular.</summary>
        /// <param name="direction">direction of the light</param>
        /// <param name="ambient">ambient value</param>
        /// <param name="diffuse">diffuse value</param>
        /// <param name="specular">specular value</param>
        public DirectLight(Vector3 direction, Color4 ambient, Color4 diffuse, Color4 specular)
        {
            Direction = direction;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }

        /// <summary>Activate this light</summary>
        public void Activate()
        {
            ThrowIfDestroyed();
            if(IsActivated) { return; }
            Dispatcher.Invoke(() =>
            {
                IsActivated = true;
                Light.AddLight(this);
                LightUp();
            });
        }

        /// <summary>Destroy this light</summary>
        public void Destroy()
        {
            ThrowIfDestroyed();
            Dispatcher.Invoke(() =>
            {
                IsDestroyed = true;
                Light.RemoveLight(this);
                TurnOff();
            });
        }

        /// <summary>Light up this light</summary>
        public void LightUp()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            if(_isLitUp == false) {
                GL.Enable((EnableCap)LightName);
                GL.Light(LightName, LightParameter.Position, _position);
                GL.Light(LightName, LightParameter.Ambient, Ambient);
                GL.Light(LightName, LightParameter.Diffuse, Diffuse);
                GL.Light(LightName, LightParameter.Specular, Specular);
                _isLitUp = true;
            }
        }

        /// <summary>Turn off this light</summary>
        public void TurnOff()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            if(_isLitUp) {
                GL.Disable((EnableCap)LightName);
                _isLitUp = false;
            }
        }

        /// <summary>Throw exception if the instance is destroyed.</summary>
        private void ThrowIfDestroyed()
        {
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
        }
    }
}
