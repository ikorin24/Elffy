#nullable enable
using Elffy.Core;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Elffy
{
    /// <summary>Light interface</summary>
    public interface ILight : IDestroyable
    {
        /// <summary>[internal] get or set light name of OpenGL</summary>
        internal LightName LightName { get; set; }

        /// <summary>get whether this light is activated</summary>
        bool IsActivated { get; }

        /// <summary>get or set ambient value of this light</summary>
        Color4 Ambient { get; set; }
        /// <summary>get or set diffuse value of this light</summary>
        Color4 Diffuse { get; set; }
        /// <summary>get or set specular value of this light</summary>
        Color4 Specular { get; set; }
        /// <summary>get ID of this light</summary>
        public int ID { get; }

        /// <summary>Activate this light</summary>
        void Activate();

        /// <summary>Light up this light</summary>
        void LightUp();

        /// <summary>Turn off this light</summary>
        void TurnOff();
    }
}
