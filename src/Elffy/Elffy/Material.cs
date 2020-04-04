#nullable enable
using System.Diagnostics;
using Elffy.Core;
using System;
using System.Runtime.InteropServices;

namespace Elffy
{
    /// <summary>Material of <see cref="Renderable"/> object</summary>
    [DebuggerDisplay("Ambient=({Ambient.R}, {Ambient.G}, {Ambient.B}, {Ambient.A}), Diffuse=({Diffuse.R}, {Diffuse.G}, {Diffuse.B}, {Diffuse.A}), Specular=({Specular.R}, {Specular.G}, {Specular.B}, {Specular.A}), Shininess={Shininess}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct Material : IEquatable<Material>
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

        /// <summary>Default material.</summary>
        public static Material Default => Materials.Plain;

        /// <summary>constructor of specified color material</summary>
        /// <param name="color">color of material</param>
        public Material(Color4 color)
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
        public Material(Color4 ambient, Color4 diffuse, Color4 specular, float shininess)
        {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
        }

        public readonly override bool Equals(object? obj) => obj is Material material && Equals(material);

        public readonly bool Equals(Material other) => Ambient.Equals(other.Ambient) &&
                                                       Diffuse.Equals(other.Diffuse) &&
                                                       Specular.Equals(other.Specular) &&
                                                       Shininess == other.Shininess;

        public readonly override int GetHashCode() => HashCode.Combine(Ambient, Diffuse, Specular, Shininess);

        public static bool operator ==(Material left, Material right) => left.Equals(right);

        public static bool operator !=(Material left, Material right) => !(left == right);
    }
}
