#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elffy.Core;

namespace Elffy.Components
{
    [DebuggerDisplay("Ambient=({Ambient.R}, {Ambient.G}, {Ambient.B}, {Ambient.A}), Diffuse=({Diffuse.R}, {Diffuse.G}, {Diffuse.B}, {Diffuse.A}), Specular=({Specular.R}, {Specular.G}, {Specular.B}, {Specular.A}), Shininess={Shininess}")]
    public sealed class Material : IComponent
    {
        private MaterialValue _material;

        /// <summary>Ambient color of material</summary>
        public ref Color4 Ambient => ref _material.Ambient;
        /// <summary>Diffuse color of material</summary>
        public ref Color4 Diffuse => ref _material.Diffuse;
        /// <summary>Specular color of material</summary>
        public ref Color4 Specular => ref _material.Specular;
        /// <summary>Shininess intensity of material</summary>
        public ref float Shininess => ref _material.Shininess;

        public ref MaterialValue Value => ref _material;

        public Material(Color4 color)
        {
            _material = new MaterialValue(color);
        }

        public Material(Color4 ambient, Color4 diffuse, Color4 specular, float shininess)
        {
            _material = new MaterialValue(ambient, diffuse, specular, shininess);
        }

        public Material(MaterialValue materialValue)
        {
            _material = materialValue;
        }

        public void OnAttached(ComponentOwner owner) { }

        public void OnDetached(ComponentOwner owner) { }
    }

    [DebuggerDisplay("Ambient=({Ambient.R}, {Ambient.G}, {Ambient.B}, {Ambient.A}), Diffuse=({Diffuse.R}, {Diffuse.G}, {Diffuse.B}, {Diffuse.A}), Specular=({Specular.R}, {Specular.G}, {Specular.B}, {Specular.A}), Shininess={Shininess}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct MaterialValue : IEquatable<MaterialValue>
    {
        /// <summary>Ambient color of material</summary>
        [FieldOffset(0)]
        public Color4 Ambient;
        /// <summary>Diffuse color of material</summary>
        [FieldOffset(16)]
        public Color4 Diffuse;
        /// <summary>Specular color of material</summary>
        [FieldOffset(32)]
        public Color4 Specular;
        /// <summary>Shininess intensity of material</summary>
        [FieldOffset(48)]
        public float Shininess;

        ///// <summary>Default material.</summary>
        //public static Material Default => Materials.Plain;

        /// <summary>constructor of specified color material</summary>
        /// <param name="color">color of material</param>
        public MaterialValue(Color4 color)
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
        public MaterialValue(Color4 ambient, Color4 diffuse, Color4 specular, float shininess)
        {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
        }

        public readonly override bool Equals(object? obj) => obj is MaterialValue material && Equals(material);

        public readonly bool Equals(MaterialValue other) => Ambient.Equals(other.Ambient) &&
                                                             Diffuse.Equals(other.Diffuse) &&
                                                             Specular.Equals(other.Specular) &&
                                                             Shininess == other.Shininess;

        public readonly override int GetHashCode() => HashCode.Combine(Ambient, Diffuse, Specular, Shininess);

        public static bool operator ==(MaterialValue left, MaterialValue right) => left.Equals(right);

        public static bool operator !=(MaterialValue left, MaterialValue right) => !(left == right);
    }
}
