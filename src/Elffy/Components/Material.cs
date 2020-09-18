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

        public Material(in Color4 color)
        {
            _material = new MaterialValue(color);
        }

        public Material(in Color4 ambient, in Color4 diffuse, in Color4 specular, float shininess)
        {
            _material = new MaterialValue(ambient, diffuse, specular, shininess);
        }

        public Material(in MaterialValue materialValue)
        {
            _material = materialValue;
        }

        void IComponent.OnAttached(ComponentOwner owner) { }
        void IComponent.OnDetached(ComponentOwner owner) { }
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

        /// <summary>Default material.</summary>
        public static MaterialValue Default => Plain;

        /// <summary>constructor of specified color material</summary>
        /// <param name="color">color of material</param>
        public MaterialValue(in Color4 color)
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
        public MaterialValue(in Color4 ambient, in Color4 diffuse, in Color4 specular, float shininess)
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

        public static bool operator ==(in MaterialValue left, in MaterialValue right) => left.Equals(right);

        public static bool operator !=(in MaterialValue left, in MaterialValue right) => !(left == right);



        /// <summary>Plain (Ambient: (0.2f, 0.2f, 0.2f, 1f), Diffuse: (0.8f, 0.8f, 0.8f, 1f), Specular: (0f, 0f, 0f, 1f), Shininess: 0f)</summary>
        public static readonly MaterialValue Plain = new MaterialValue(new Color4(0.2f, 0.2f, 0.2f, 1f), new Color4(0.8f, 0.8f, 0.8f, 1f), new Color4(0f, 0f, 0f, 1f), 0f);

        /// <summary>Emerald (Ambient: (0.0215f, 0.1745f, 0.0215f, 1.0f), Diffuse: (0.07568f, 0.61424f, 0.07568f, 1.0f), Specular: (0.633f, 0.727811f, 0.633f, 1.0f), Shininess: 76.8f)</summary>
        public static readonly MaterialValue Emerald = new MaterialValue(new Color4(0.0215f, 0.1745f, 0.0215f, 1.0f), new Color4(0.07568f, 0.61424f, 0.07568f, 1.0f), new Color4(0.633f, 0.727811f, 0.633f, 1.0f), 76.8f);

        /// <summary>Jade (Ambient: (0.135f, 0.2225f, 0.1575f, 1.0f), Diffuse: (0.54f, 0.89f, 0.63f, 1.0f), Specular: (0.316228f, 0.316228f, 0.316228f, 1.0f), Shininess: 12.8f)</summary>
        public static readonly MaterialValue Jade = new MaterialValue(new Color4(0.135f, 0.2225f, 0.1575f, 1.0f), new Color4(0.54f, 0.89f, 0.63f, 1.0f), new Color4(0.316228f, 0.316228f, 0.316228f, 1.0f), 12.8f);

        /// <summary>Obsidian (Ambient: (0.05375f, 0.05f, 0.06625f, 1.0f), Diffuse: (0.18275f, 0.17f, 0.22525f, 1.0f), Specular: (0.332741f, 0.328634f, 0.346435f, 1.0f), Shininess: 38.4f)</summary>
        public static readonly MaterialValue Obsidian = new MaterialValue(new Color4(0.05375f, 0.05f, 0.06625f, 1.0f), new Color4(0.18275f, 0.17f, 0.22525f, 1.0f), new Color4(0.332741f, 0.328634f, 0.346435f, 1.0f), 38.4f);

        /// <summary>Pearl (Ambient: (0.25f, 0.20725f, 0.20725f, 1.0f), Diffuse: (1f, 0.829f, 0.829f, 1.0f), Specular: (0.296648f, 0.296648f, 0.296648f, 1.0f), Shininess: 11.264f)</summary>
        public static readonly MaterialValue Pearl = new MaterialValue(new Color4(0.25f, 0.20725f, 0.20725f, 1.0f), new Color4(1f, 0.829f, 0.829f, 1.0f), new Color4(0.296648f, 0.296648f, 0.296648f, 1.0f), 11.264f);

        /// <summary>Ruby (Ambient: (0.1745f, 0.01175f, 0.01175f, 1.0f), Diffuse: (0.61424f, 0.04136f, 0.04136f, 1.0f), Specular: (0.727811f, 0.626959f, 0.626959f, 1.0f), Shininess: 76.8f)</summary>
        public static readonly MaterialValue Ruby = new MaterialValue(new Color4(0.1745f, 0.01175f, 0.01175f, 1.0f), new Color4(0.61424f, 0.04136f, 0.04136f, 1.0f), new Color4(0.727811f, 0.626959f, 0.626959f, 1.0f), 76.8f);

        /// <summary>Turquoise (Ambient: (0.1f, 0.18725f, 0.1745f, 1.0f), Diffuse: (0.396f, 0.74151f, 0.69102f, 1.0f), Specular: (0.297254f, 0.30829f, 0.306678f, 1.0f), Shininess: 12.8f)</summary>
        public static readonly MaterialValue Turquoise = new MaterialValue(new Color4(0.1f, 0.18725f, 0.1745f, 1.0f), new Color4(0.396f, 0.74151f, 0.69102f, 1.0f), new Color4(0.297254f, 0.30829f, 0.306678f, 1.0f), 12.8f);

        /// <summary>Brass (Ambient: (0.329412f, 0.223529f, 0.027451f, 1.0f), Diffuse: (0.780392f, 0.568627f, 0.113725f, 1.0f), Specular: (0.992157f, 0.941176f, 0.807843f, 1.0f), Shininess: 27.89743616f)</summary>
        public static readonly MaterialValue Brass = new MaterialValue(new Color4(0.329412f, 0.223529f, 0.027451f, 1.0f), new Color4(0.780392f, 0.568627f, 0.113725f, 1.0f), new Color4(0.992157f, 0.941176f, 0.807843f, 1.0f), 27.89743616f);

        /// <summary>Bronze (Ambient: (0.2125f, 0.1275f, 0.054f, 1.0f), Diffuse: (0.714f, 0.4284f, 0.18144f, 1.0f), Specular: (0.393548f, 0.271906f, 0.166721f, 1.0f), Shininess: 25.6f)</summary>
        public static readonly MaterialValue Bronze = new MaterialValue(new Color4(0.2125f, 0.1275f, 0.054f, 1.0f), new Color4(0.714f, 0.4284f, 0.18144f, 1.0f), new Color4(0.393548f, 0.271906f, 0.166721f, 1.0f), 25.6f);

        /// <summary>Chrome (Ambient: (0.25f, 0.25f, 0.25f, 1.0f), Diffuse: (0.4f, 0.4f, 0.4f, 1.0f), Specular: (0.774597f, 0.774597f, 0.774597f, 1.0f), Shininess: 76.8f)</summary>
        public static readonly MaterialValue Chrome = new MaterialValue(new Color4(0.25f, 0.25f, 0.25f, 1.0f), new Color4(0.4f, 0.4f, 0.4f, 1.0f), new Color4(0.774597f, 0.774597f, 0.774597f, 1.0f), 76.8f);

        /// <summary>Copper (Ambient: (0.19125f, 0.0735f, 0.0225f, 1.0f), Diffuse: (0.7038f, 0.27048f, 0.0828f, 1.0f), Specular: (0.256777f, 0.137622f, 0.086014f, 1.0f), Shininess: 12.8f)</summary>
        public static readonly MaterialValue Copper = new MaterialValue(new Color4(0.19125f, 0.0735f, 0.0225f, 1.0f), new Color4(0.7038f, 0.27048f, 0.0828f, 1.0f), new Color4(0.256777f, 0.137622f, 0.086014f, 1.0f), 12.8f);

        /// <summary>Gold (Ambient: (0.24725f, 0.1995f, 0.0745f, 1.0f), Diffuse: (0.75164f, 0.60648f, 0.22648f, 1.0f), Specular: (0.628281f, 0.555802f, 0.366065f, 1.0f), Shininess: 51.2f)</summary>
        public static readonly MaterialValue Gold = new MaterialValue(new Color4(0.24725f, 0.1995f, 0.0745f, 1.0f), new Color4(0.75164f, 0.60648f, 0.22648f, 1.0f), new Color4(0.628281f, 0.555802f, 0.366065f, 1.0f), 51.2f);

        /// <summary>Silver (Ambient: (0.19225f, 0.19225f, 0.19225f, 1.0f), Diffuse: (0.50754f, 0.50754f, 0.50754f, 1.0f), Specular: (0.508273f, 0.508273f, 0.508273f, 1.0f), Shininess: 51.2f)</summary>
        public static readonly MaterialValue Silver = new MaterialValue(new Color4(0.19225f, 0.19225f, 0.19225f, 1.0f), new Color4(0.50754f, 0.50754f, 0.50754f, 1.0f), new Color4(0.508273f, 0.508273f, 0.508273f, 1.0f), 51.2f);

        /// <summary>BlackPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.01f, 0.01f, 0.01f, 1.0f), Specular: (0.50f, 0.50f, 0.50f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly MaterialValue BlackPlastic = new MaterialValue(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.01f, 0.01f, 0.01f, 1.0f), new Color4(0.50f, 0.50f, 0.50f, 1.0f), 32.0f);

        /// <summary>CyanPlastic (Ambient: (0f, 0.1f, 0.06f, 1.0f), Diffuse: (0f, 0.50980392f, 0.50980392f, 1.0f), Specular: (0.50196078f, 0.50196078f, 0.50196078f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly MaterialValue CyanPlastic = new MaterialValue(new Color4(0f, 0.1f, 0.06f, 1.0f), new Color4(0f, 0.50980392f, 0.50980392f, 1.0f), new Color4(0.50196078f, 0.50196078f, 0.50196078f, 1.0f), 32.0f);

        /// <summary>GreenPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.1f, 0.35f, 0.1f, 1.0f), Specular: (0.45f, 0.55f, 0.45f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly MaterialValue GreenPlastic = new MaterialValue(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.1f, 0.35f, 0.1f, 1.0f), new Color4(0.45f, 0.55f, 0.45f, 1.0f), 32.0f);

        /// <summary>RedPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0f, 0f, 1.0f), Specular: (0.7f, 0.6f, 0.6f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly MaterialValue RedPlastic = new MaterialValue(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.5f, 0f, 0f, 1.0f), new Color4(0.7f, 0.6f, 0.6f, 1.0f), 32.0f);

        /// <summary>WhitePlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.55f, 0.55f, 0.55f, 1.0f), Specular: (0.70f, 0.70f, 0.70f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly MaterialValue WhitePlastic = new MaterialValue(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.55f, 0.55f, 0.55f, 1.0f), new Color4(0.70f, 0.70f, 0.70f, 1.0f), 32.0f);

        /// <summary>YellowPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0.5f, 0f, 1.0f), Specular: (0.60f, 0.60f, 0.50f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly MaterialValue YellowPlastic = new MaterialValue(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.5f, 0.5f, 0f, 1.0f), new Color4(0.60f, 0.60f, 0.50f, 1.0f), 32.0f);

        /// <summary>BlackRubber (Ambient: (0.02f, 0.02f, 0.02f, 1.0f), Diffuse: (0.01f, 0.01f, 0.01f, 1.0f), Specular: (0.4f, 0.4f, 0.4f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly MaterialValue BlackRubber = new MaterialValue(new Color4(0.02f, 0.02f, 0.02f, 1.0f), new Color4(0.01f, 0.01f, 0.01f, 1.0f), new Color4(0.4f, 0.4f, 0.4f, 1.0f), 10.0f);

        /// <summary>CyanRubber (Ambient: (0f, 0.05f, 0.05f, 1.0f), Diffuse: (0.4f, 0.5f, 0.5f, 1.0f), Specular: (0.04f, 0.7f, 0.7f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly MaterialValue CyanRubber = new MaterialValue(new Color4(0f, 0.05f, 0.05f, 1.0f), new Color4(0.4f, 0.5f, 0.5f, 1.0f), new Color4(0.04f, 0.7f, 0.7f, 1.0f), 10.0f);

        /// <summary>GreenRubber (Ambient: (0f, 0.05f, 0f, 1.0f), Diffuse: (0.4f, 0.5f, 0.4f, 1.0f), Specular: (0.04f, 0.7f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly MaterialValue GreenRubber = new MaterialValue(new Color4(0f, 0.05f, 0f, 1.0f), new Color4(0.4f, 0.5f, 0.4f, 1.0f), new Color4(0.04f, 0.7f, 0.04f, 1.0f), 10.0f);

        /// <summary>RedRubber (Ambient: (0.05f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0.4f, 0.4f, 1.0f), Specular: (0.7f, 0.04f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly MaterialValue RedRubber = new MaterialValue(new Color4(0.05f, 0f, 0f, 1.0f), new Color4(0.5f, 0.4f, 0.4f, 1.0f), new Color4(0.7f, 0.04f, 0.04f, 1.0f), 10.0f);

        /// <summary>WhiteRubber (Ambient: (0.05f, 0.05f, 0.05f, 1.0f), Diffuse: (0.5f, 0.5f, 0.5f, 1.0f), Specular: (0.7f, 0.7f, 0.7f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly MaterialValue WhiteRubber = new MaterialValue(new Color4(0.05f, 0.05f, 0.05f, 1.0f), new Color4(0.5f, 0.5f, 0.5f, 1.0f), new Color4(0.7f, 0.7f, 0.7f, 1.0f), 10.0f);

        /// <summary>YellowRubber (Ambient: (0.05f, 0.05f, 0f, 1.0f), Diffuse: (0.5f, 0.5f, 0.4f, 1.0f), Specular: (0.7f, 0.7f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly MaterialValue YellowRubber = new MaterialValue(new Color4(0.05f, 0.05f, 0f, 1.0f), new Color4(0.5f, 0.5f, 0.4f, 1.0f), new Color4(0.7f, 0.7f, 0.04f, 1.0f), 10.0f);
    }
}
