#nullable enable
using Elffy.Exceptions;
using Elffy.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Elffy
{
    /// <summary>Direct light class</summary>
    public sealed class DirectLight : ILight
    {
        private readonly Light.LightImpl _lightImpl = new Light.LightImpl();

        /// <summary>get whether this light is activated</summary>
        public bool IsActivated
        { 
            get => _lightImpl.IsActivated;
            private set => _lightImpl.IsActivated = value;
        }

        /// <summary>get whether this light is destroyed</summary>
        public bool IsDestroyed => _lightImpl.IsDestroyed;

        /// <summary>light name</summary>
        LightName ILight.LightName
        {
            get => _lightImpl.LightName;
            set => _lightImpl.LightName = value;
        }

        /// <summary>get or set direction of this light</summary>
        public Vector3 Direction
        {
            get => -_lightImpl.Position.Xyz;
            set => _lightImpl.Position = new Vector4(-value);
        }
        /// <summary>get or set ambient value of this light</summary>
        public Color4 Ambient
        {
            get => _lightImpl.Ambient;
            set => _lightImpl.Ambient = value;
        }
        /// <summary>get or set diffuse value of this light</summary>
        public Color4 Diffuse
        {
            get => _lightImpl.Diffuse;
            set => _lightImpl.Diffuse = value;
        }
        /// <summary>get or set specular value of this light</summary>
        public Color4 Specular
        {
            get => _lightImpl.Specular;
            set => _lightImpl.Specular = value;
        }
        /// <summary>get ID of this light</summary>
        public int ID => (int)(_lightImpl.LightName - LightName.Light0);

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
                _lightImpl.Activate(this);
            });
        }

        /// <summary>Destroy this light</summary>
        public void Destroy()
        {
            ThrowIfDestroyed();
            Dispatcher.Invoke(() =>
            {
                _lightImpl.Destroy(this);
            });
        }

        /// <summary>Light up this light</summary>
        public void LightUp()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            _lightImpl.LightUp();
        }

        /// <summary>Turn off this light</summary>
        public void TurnOff()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            _lightImpl.TurnOff();
        }

        /// <summary>Throw exception if the instance is destroyed.</summary>
        private void ThrowIfDestroyed()
        {
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
        }
    }
}
