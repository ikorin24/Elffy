#nullable enable
using Elffy.Exceptions;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Point light class</summary>
    public class PointLight : ILight
    {
        /// <summary>Implementation of the light</summary>
        private readonly LightImpl _lightImpl = new LightImpl();

        /// <summary>get whether this light is activated</summary>
        public bool IsActivated
        {
            get => _lightImpl.IsActivated;
            private set => _lightImpl.IsActivated = value;
        }

        /// <summary>get whether this light is destroyed</summary>
        public bool IsTerminated => _lightImpl.IsTerminated;

        /// <summary>get or set position of this light</summary>
        public Vector3 Position
        {
            get => _lightImpl.Position.Xyz;
            set => _lightImpl.Position = new Vector4(value, 1f);
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

        /// <summary>light name</summary>
        LightName ILight.LightName
        {
            get => _lightImpl.LightName;
            set => _lightImpl.LightName = value;
        }

        /// <summary>
        /// Create <see cref="PointLight"/> instance.<para/>
        /// Position = (0, 0, 0) --- (X, Y, Z)<para/>
        /// [Ambient, Diffuse, Specular] = [(0, 0, 0, 1), (1, 1, 1, 1), (1, 1, 1, 1)] --- (R, G, B, A)<para/>
        /// </summary>
        public PointLight() : this(Vector3.Zero, Color4.Black, Color4.White, Color4.White) { }

        /// <summary>
        /// Create <see cref="PointLight"/> instance of specified position.<para/>
        /// [Ambient, Diffuse, Specular] = [(0, 0, 0, 1), (1, 1, 1, 1), (1, 1, 1, 1)] --- (R, G, B, A)<para/>
        /// </summary>
        /// <param name="position">position of the light</param>
        public PointLight(Vector3 position) : this(position, Color4.Black, Color4.White, Color4.White) { }

        /// <summary>
        /// Create <see cref="PointLight"/> instance of specified position and specified diffuse &amp; specular.<para/>
        /// Ambient = (0, 0, 0, 1) --- (R, G, B, A)<para/>
        /// </summary>
        /// <param name="position">position of the light</param>
        /// <param name="color">diffuse and specular value</param>
        public PointLight(Vector3 position, Color4 color) : this(position, Color4.Black, color, color) { }

        /// <summary>Create <see cref="PointLight"/> instance of specified position and specified ambient, diffuse, specular.</summary>
        /// <param name="position">position of the light</param>
        /// <param name="ambient">ambient value</param>
        /// <param name="diffuse">diffuse value</param>
        /// <param name="specular">specular value</param>
        public PointLight(Vector3 position, Color4 ambient, Color4 diffuse, Color4 specular)
        {
            Position = position;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }

        public void Activate()
        {
            ThrowIfTerminated();
            if(IsActivated) { return; }
            _lightImpl.Activate(this, Light.Current);
            _lightImpl.LightUp();
        }

        public void Terminate()
        {
            ThrowIfTerminated();
            _lightImpl.Terminate(this);
            _lightImpl.TurnOff();
        }

        public void LightUp()
        {
            ThrowIfTerminated();
            Dispatcher.Current.ThrowIfNotMainThread();
            _lightImpl.LightUp();
        }

        public void TurnOff()
        {
            ThrowIfTerminated();
            Dispatcher.Current.ThrowIfNotMainThread();
            _lightImpl.TurnOff();
        }

        /// <summary>Throw exception if the instance is destroyed.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfTerminated()
        {
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
        }
    }
}
