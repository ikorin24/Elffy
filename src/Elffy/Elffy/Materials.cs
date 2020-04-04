#nullable enable

namespace Elffy
{
    /// <summary>Templates of <see cref="Material"/> object</summary>
    public static class Materials
    {
        /// <summary>Plain (Ambient: (0.2f, 0.2f, 0.2f, 1f), Diffuse: (0.8f, 0.8f, 0.8f, 1f), Specular: (0f, 0f, 0f, 1f), Shininess: 0f)</summary>
        public static readonly Material Plain = new Material(new Color4(0.2f, 0.2f, 0.2f, 1f), new Color4(0.8f, 0.8f, 0.8f, 1f), new Color4(0f, 0f, 0f, 1f), 0f);

        /// <summary>Emerald (Ambient: (0.0215f, 0.1745f, 0.0215f, 1.0f), Diffuse: (0.07568f, 0.61424f, 0.07568f, 1.0f), Specular: (0.633f, 0.727811f, 0.633f, 1.0f), Shininess: 76.8f)</summary>
        public static readonly Material Emerald = new Material(new Color4(0.0215f, 0.1745f, 0.0215f, 1.0f), new Color4(0.07568f, 0.61424f, 0.07568f, 1.0f), new Color4(0.633f, 0.727811f, 0.633f, 1.0f), 76.8f);

        /// <summary>Jade (Ambient: (0.135f, 0.2225f, 0.1575f, 1.0f), Diffuse: (0.54f, 0.89f, 0.63f, 1.0f), Specular: (0.316228f, 0.316228f, 0.316228f, 1.0f), Shininess: 12.8f)</summary>
        public static readonly Material Jade = new Material(new Color4(0.135f, 0.2225f, 0.1575f, 1.0f), new Color4(0.54f, 0.89f, 0.63f, 1.0f), new Color4(0.316228f, 0.316228f, 0.316228f, 1.0f), 12.8f);

        /// <summary>Obsidian (Ambient: (0.05375f, 0.05f, 0.06625f, 1.0f), Diffuse: (0.18275f, 0.17f, 0.22525f, 1.0f), Specular: (0.332741f, 0.328634f, 0.346435f, 1.0f), Shininess: 38.4f)</summary>
        public static readonly Material Obsidian = new Material(new Color4(0.05375f, 0.05f, 0.06625f, 1.0f), new Color4(0.18275f, 0.17f, 0.22525f, 1.0f), new Color4(0.332741f, 0.328634f, 0.346435f, 1.0f), 38.4f);

        /// <summary>Pearl (Ambient: (0.25f, 0.20725f, 0.20725f, 1.0f), Diffuse: (1f, 0.829f, 0.829f, 1.0f), Specular: (0.296648f, 0.296648f, 0.296648f, 1.0f), Shininess: 11.264f)</summary>
        public static readonly Material Pearl = new Material(new Color4(0.25f, 0.20725f, 0.20725f, 1.0f), new Color4(1f, 0.829f, 0.829f, 1.0f), new Color4(0.296648f, 0.296648f, 0.296648f, 1.0f), 11.264f);

        /// <summary>Ruby (Ambient: (0.1745f, 0.01175f, 0.01175f, 1.0f), Diffuse: (0.61424f, 0.04136f, 0.04136f, 1.0f), Specular: (0.727811f, 0.626959f, 0.626959f, 1.0f), Shininess: 76.8f)</summary>
        public static readonly Material Ruby = new Material(new Color4(0.1745f, 0.01175f, 0.01175f, 1.0f), new Color4(0.61424f, 0.04136f, 0.04136f, 1.0f), new Color4(0.727811f, 0.626959f, 0.626959f, 1.0f), 76.8f);

        /// <summary>Turquoise (Ambient: (0.1f, 0.18725f, 0.1745f, 1.0f), Diffuse: (0.396f, 0.74151f, 0.69102f, 1.0f), Specular: (0.297254f, 0.30829f, 0.306678f, 1.0f), Shininess: 12.8f)</summary>
        public static readonly Material Turquoise = new Material(new Color4(0.1f, 0.18725f, 0.1745f, 1.0f), new Color4(0.396f, 0.74151f, 0.69102f, 1.0f), new Color4(0.297254f, 0.30829f, 0.306678f, 1.0f), 12.8f);

        /// <summary>Brass (Ambient: (0.329412f, 0.223529f, 0.027451f, 1.0f), Diffuse: (0.780392f, 0.568627f, 0.113725f, 1.0f), Specular: (0.992157f, 0.941176f, 0.807843f, 1.0f), Shininess: 27.89743616f)</summary>
        public static readonly Material Brass = new Material(new Color4(0.329412f, 0.223529f, 0.027451f, 1.0f), new Color4(0.780392f, 0.568627f, 0.113725f, 1.0f), new Color4(0.992157f, 0.941176f, 0.807843f, 1.0f), 27.89743616f);

        /// <summary>Bronze (Ambient: (0.2125f, 0.1275f, 0.054f, 1.0f), Diffuse: (0.714f, 0.4284f, 0.18144f, 1.0f), Specular: (0.393548f, 0.271906f, 0.166721f, 1.0f), Shininess: 25.6f)</summary>
        public static readonly Material Bronze = new Material(new Color4(0.2125f, 0.1275f, 0.054f, 1.0f), new Color4(0.714f, 0.4284f, 0.18144f, 1.0f), new Color4(0.393548f, 0.271906f, 0.166721f, 1.0f), 25.6f);

        /// <summary>Chrome (Ambient: (0.25f, 0.25f, 0.25f, 1.0f), Diffuse: (0.4f, 0.4f, 0.4f, 1.0f), Specular: (0.774597f, 0.774597f, 0.774597f, 1.0f), Shininess: 76.8f)</summary>
        public static readonly Material Chrome = new Material(new Color4(0.25f, 0.25f, 0.25f, 1.0f), new Color4(0.4f, 0.4f, 0.4f, 1.0f), new Color4(0.774597f, 0.774597f, 0.774597f, 1.0f), 76.8f);

        /// <summary>Copper (Ambient: (0.19125f, 0.0735f, 0.0225f, 1.0f), Diffuse: (0.7038f, 0.27048f, 0.0828f, 1.0f), Specular: (0.256777f, 0.137622f, 0.086014f, 1.0f), Shininess: 12.8f)</summary>
        public static readonly Material Copper = new Material(new Color4(0.19125f, 0.0735f, 0.0225f, 1.0f), new Color4(0.7038f, 0.27048f, 0.0828f, 1.0f), new Color4(0.256777f, 0.137622f, 0.086014f, 1.0f), 12.8f);

        /// <summary>Gold (Ambient: (0.24725f, 0.1995f, 0.0745f, 1.0f), Diffuse: (0.75164f, 0.60648f, 0.22648f, 1.0f), Specular: (0.628281f, 0.555802f, 0.366065f, 1.0f), Shininess: 51.2f)</summary>
        public static readonly Material Gold = new Material(new Color4(0.24725f, 0.1995f, 0.0745f, 1.0f), new Color4(0.75164f, 0.60648f, 0.22648f, 1.0f), new Color4(0.628281f, 0.555802f, 0.366065f, 1.0f), 51.2f);

        /// <summary>Silver (Ambient: (0.19225f, 0.19225f, 0.19225f, 1.0f), Diffuse: (0.50754f, 0.50754f, 0.50754f, 1.0f), Specular: (0.508273f, 0.508273f, 0.508273f, 1.0f), Shininess: 51.2f)</summary>
        public static readonly Material Silver = new Material(new Color4(0.19225f, 0.19225f, 0.19225f, 1.0f), new Color4(0.50754f, 0.50754f, 0.50754f, 1.0f), new Color4(0.508273f, 0.508273f, 0.508273f, 1.0f), 51.2f);

        /// <summary>BlackPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.01f, 0.01f, 0.01f, 1.0f), Specular: (0.50f, 0.50f, 0.50f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly Material BlackPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.01f, 0.01f, 0.01f, 1.0f), new Color4(0.50f, 0.50f, 0.50f, 1.0f), 32.0f);

        /// <summary>CyanPlastic (Ambient: (0f, 0.1f, 0.06f, 1.0f), Diffuse: (0f, 0.50980392f, 0.50980392f, 1.0f), Specular: (0.50196078f, 0.50196078f, 0.50196078f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly Material CyanPlastic = new Material(new Color4(0f, 0.1f, 0.06f, 1.0f), new Color4(0f, 0.50980392f, 0.50980392f, 1.0f), new Color4(0.50196078f, 0.50196078f, 0.50196078f, 1.0f), 32.0f);

        /// <summary>GreenPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.1f, 0.35f, 0.1f, 1.0f), Specular: (0.45f, 0.55f, 0.45f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly Material GreenPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.1f, 0.35f, 0.1f, 1.0f), new Color4(0.45f, 0.55f, 0.45f, 1.0f), 32.0f);

        /// <summary>RedPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0f, 0f, 1.0f), Specular: (0.7f, 0.6f, 0.6f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly Material RedPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.5f, 0f, 0f, 1.0f), new Color4(0.7f, 0.6f, 0.6f, 1.0f), 32.0f);

        /// <summary>WhitePlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.55f, 0.55f, 0.55f, 1.0f), Specular: (0.70f, 0.70f, 0.70f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly Material WhitePlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.55f, 0.55f, 0.55f, 1.0f), new Color4(0.70f, 0.70f, 0.70f, 1.0f), 32.0f);

        /// <summary>YellowPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0.5f, 0f, 1.0f), Specular: (0.60f, 0.60f, 0.50f, 1.0f), Shininess: 32.0f)</summary>
        public static readonly Material YellowPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.5f, 0.5f, 0f, 1.0f), new Color4(0.60f, 0.60f, 0.50f, 1.0f), 32.0f);

        /// <summary>BlackRubber (Ambient: (0.02f, 0.02f, 0.02f, 1.0f), Diffuse: (0.01f, 0.01f, 0.01f, 1.0f), Specular: (0.4f, 0.4f, 0.4f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly Material BlackRubber = new Material(new Color4(0.02f, 0.02f, 0.02f, 1.0f), new Color4(0.01f, 0.01f, 0.01f, 1.0f), new Color4(0.4f, 0.4f, 0.4f, 1.0f), 10.0f);

        /// <summary>CyanRubber (Ambient: (0f, 0.05f, 0.05f, 1.0f), Diffuse: (0.4f, 0.5f, 0.5f, 1.0f), Specular: (0.04f, 0.7f, 0.7f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly Material CyanRubber = new Material(new Color4(0f, 0.05f, 0.05f, 1.0f), new Color4(0.4f, 0.5f, 0.5f, 1.0f), new Color4(0.04f, 0.7f, 0.7f, 1.0f), 10.0f);

        /// <summary>GreenRubber (Ambient: (0f, 0.05f, 0f, 1.0f), Diffuse: (0.4f, 0.5f, 0.4f, 1.0f), Specular: (0.04f, 0.7f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly Material GreenRubber = new Material(new Color4(0f, 0.05f, 0f, 1.0f), new Color4(0.4f, 0.5f, 0.4f, 1.0f), new Color4(0.04f, 0.7f, 0.04f, 1.0f), 10.0f);

        /// <summary>RedRubber (Ambient: (0.05f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0.4f, 0.4f, 1.0f), Specular: (0.7f, 0.04f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly Material RedRubber = new Material(new Color4(0.05f, 0f, 0f, 1.0f), new Color4(0.5f, 0.4f, 0.4f, 1.0f), new Color4(0.7f, 0.04f, 0.04f, 1.0f), 10.0f);

        /// <summary>WhiteRubber (Ambient: (0.05f, 0.05f, 0.05f, 1.0f), Diffuse: (0.5f, 0.5f, 0.5f, 1.0f), Specular: (0.7f, 0.7f, 0.7f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly Material WhiteRubber = new Material(new Color4(0.05f, 0.05f, 0.05f, 1.0f), new Color4(0.5f, 0.5f, 0.5f, 1.0f), new Color4(0.7f, 0.7f, 0.7f, 1.0f), 10.0f);

        /// <summary>YellowRubber (Ambient: (0.05f, 0.05f, 0f, 1.0f), Diffuse: (0.5f, 0.5f, 0.4f, 1.0f), Specular: (0.7f, 0.7f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static readonly Material YellowRubber = new Material(new Color4(0.05f, 0.05f, 0f, 1.0f), new Color4(0.5f, 0.5f, 0.4f, 1.0f), new Color4(0.7f, 0.7f, 0.04f, 1.0f), 10.0f);
    }
}
