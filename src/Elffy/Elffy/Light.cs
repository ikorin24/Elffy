#nullable enable
using Elffy.Core;

namespace Elffy
{
    public abstract class Light : ComponentOwner
    {
        /// <summary>get or set ambient value of this light</summary>
        public Color3 Ambient { get; set; }
        /// <summary>get or set diffuse value of this light</summary>
        public Color3 Diffuse { get; set; }
        /// <summary>get or set specular value of this light</summary>
        public Color3 Specular { get; set; }

        public abstract Vector4 Position4 { get; }
    }

    public sealed class DirectLight : Light
    {
        /// <summary>get or set direction of this light</summary>
        public Vector3 Direction { get; set; }

        /// <summary>Get position as <see cref="Vector4"/>, whose element of w is 0.</summary>
        public override Vector4 Position4 => new Vector4(-Direction, 0f);

        public DirectLight() : this(new Vector3(0, -1, 0)) { }

        public DirectLight(Vector3 direction) : this(direction, new Color3(0.85f), new Color3(0.9f), new Color3(1)) { }
        
        public DirectLight(Vector3 direction, Color3 ambient, Color3 diffuse, Color3 specular)
        {
            Direction = direction;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }
    }

    public sealed class PointLight : Light
    {
        /// <summary>Get or set position of this light</summary>
        public Vector3 Position { get; set; }

        /// <summary>Get position as <see cref="Vector4"/>, whose element of w is 1.</summary>
        public override Vector4 Position4 => new Vector4(Position, 1f);
    }
}
