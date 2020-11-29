#nullable enable
using Elffy.Core;

namespace Elffy
{
    public abstract class Light : ComponentOwner
    {
        private Color3 _ambient;
        private Color3 _diffuse;
        private Color3 _specular;

        /// <summary>get or set ambient value of this light</summary>
        public ref Color3 Ambient => ref _ambient;
        /// <summary>get or set diffuse value of this light</summary>
        public ref Color3 Diffuse => ref _diffuse;
        /// <summary>get or set specular value of this light</summary>
        public ref Color3 Specular => ref _specular;

        public abstract Vector4 Position4 { get; }
    }

    public sealed class DirectLight : Light
    {
        private Vector3 _direction;

        /// <summary>get or set direction of this light</summary>
        public ref Vector3 Direction => ref _direction;

        /// <summary>Get position as <see cref="Vector4"/>, whose element of w is 0.</summary>
        public override Vector4 Position4 => new Vector4(-Direction, 0f);

        public DirectLight() : this(new Vector3(0, -1, 0)) { }

        public DirectLight(in Vector3 direction) : this(direction, new Color3(0.85f), new Color3(0.9f), new Color3(1)) { }
        
        public DirectLight(in Vector3 direction, in Color3 ambient, in Color3 diffuse, in Color3 specular)
        {
            Direction = direction;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }
    }

    public sealed class PointLight : Light
    {
        private Vector3 _position;

        /// <summary>Get or set position of this light</summary>
        public ref Vector3 Position => ref _position;

        /// <summary>Get position as <see cref="Vector4"/>, whose element of w is 1.</summary>
        public override Vector4 Position4 => new Vector4(Position, 1f);
    }
}
