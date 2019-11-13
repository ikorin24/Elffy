#nullable enable
using OpenTK.Graphics;

namespace Elffy
{
    /// <summary>Templates of <see cref="Material"/> object</summary>
    public static class Materials
    {
        /// <summary>Plain (Ambient: (0.2f, 0.2f, 0.2f, 1f), Diffuse: (0.8f, 0.8f, 0.8f, 1f), Specular: (0f, 0f, 0f, 1f), Shininess: 0f)</summary>
        public static Material Plain => _plain ?? (_plain = new Material(new Color4(0.2f, 0.2f, 0.2f, 1f), new Color4(0.8f, 0.8f, 0.8f, 1f), new Color4(0f, 0f, 0f, 1f), 0f));
        private static Material? _plain;

        /// <summary>Emerald (Ambient: (0.0215f, 0.1745f, 0.0215f, 1.0f), Diffuse: (0.07568f, 0.61424f, 0.07568f, 1.0f), Specular: (0.633f, 0.727811f, 0.633f, 1.0f), Shininess: 76.8f)</summary>
        public static Material Emerald => _emerald ?? (_emerald = new Material(new Color4(0.0215f, 0.1745f, 0.0215f, 1.0f), new Color4(0.07568f, 0.61424f, 0.07568f, 1.0f), new Color4(0.633f, 0.727811f, 0.633f, 1.0f), 76.8f));
        private static Material? _emerald;

        /// <summary>Jade (Ambient: (0.135f, 0.2225f, 0.1575f, 1.0f), Diffuse: (0.54f, 0.89f, 0.63f, 1.0f), Specular: (0.316228f, 0.316228f, 0.316228f, 1.0f), Shininess: 12.8f)</summary>
        public static Material Jade => _jade ?? (_jade = new Material(new Color4(0.135f, 0.2225f, 0.1575f, 1.0f), new Color4(0.54f, 0.89f, 0.63f, 1.0f), new Color4(0.316228f, 0.316228f, 0.316228f, 1.0f), 12.8f));
        private static Material? _jade;

        /// <summary>Obsidian (Ambient: (0.05375f, 0.05f, 0.06625f, 1.0f), Diffuse: (0.18275f, 0.17f, 0.22525f, 1.0f), Specular: (0.332741f, 0.328634f, 0.346435f, 1.0f), Shininess: 38.4f)</summary>
        public static Material Obsidian => _obsidian ?? (_obsidian = new Material(new Color4(0.05375f, 0.05f, 0.06625f, 1.0f), new Color4(0.18275f, 0.17f, 0.22525f, 1.0f), new Color4(0.332741f, 0.328634f, 0.346435f, 1.0f), 38.4f));
        private static Material? _obsidian;

        /// <summary>Pearl (Ambient: (0.25f, 0.20725f, 0.20725f, 1.0f), Diffuse: (1f, 0.829f, 0.829f, 1.0f), Specular: (0.296648f, 0.296648f, 0.296648f, 1.0f), Shininess: 11.264f)</summary>
        public static Material Pearl => _pearl ?? (_pearl = new Material(new Color4(0.25f, 0.20725f, 0.20725f, 1.0f), new Color4(1f, 0.829f, 0.829f, 1.0f), new Color4(0.296648f, 0.296648f, 0.296648f, 1.0f), 11.264f));
        private static Material? _pearl;

        /// <summary>Ruby (Ambient: (0.1745f, 0.01175f, 0.01175f, 1.0f), Diffuse: (0.61424f, 0.04136f, 0.04136f, 1.0f), Specular: (0.727811f, 0.626959f, 0.626959f, 1.0f), Shininess: 76.8f)</summary>
        public static Material Ruby => _ruby ?? (_ruby = new Material(new Color4(0.1745f, 0.01175f, 0.01175f, 1.0f), new Color4(0.61424f, 0.04136f, 0.04136f, 1.0f), new Color4(0.727811f, 0.626959f, 0.626959f, 1.0f), 76.8f));
        private static Material? _ruby;

        /// <summary>Turquoise (Ambient: (0.1f, 0.18725f, 0.1745f, 1.0f), Diffuse: (0.396f, 0.74151f, 0.69102f, 1.0f), Specular: (0.297254f, 0.30829f, 0.306678f, 1.0f), Shininess: 12.8f)</summary>
        public static Material Turquoise => _turquoise ?? (_turquoise = new Material(new Color4(0.1f, 0.18725f, 0.1745f, 1.0f), new Color4(0.396f, 0.74151f, 0.69102f, 1.0f), new Color4(0.297254f, 0.30829f, 0.306678f, 1.0f), 12.8f));
        private static Material? _turquoise;

        /// <summary>Brass (Ambient: (0.329412f, 0.223529f, 0.027451f, 1.0f), Diffuse: (0.780392f, 0.568627f, 0.113725f, 1.0f), Specular: (0.992157f, 0.941176f, 0.807843f, 1.0f), Shininess: 27.89743616f)</summary>
        public static Material Brass => _brass ?? (_brass = new Material(new Color4(0.329412f, 0.223529f, 0.027451f, 1.0f), new Color4(0.780392f, 0.568627f, 0.113725f, 1.0f), new Color4(0.992157f, 0.941176f, 0.807843f, 1.0f), 27.89743616f));
        private static Material? _brass;

        /// <summary>Bronze (Ambient: (0.2125f, 0.1275f, 0.054f, 1.0f), Diffuse: (0.714f, 0.4284f, 0.18144f, 1.0f), Specular: (0.393548f, 0.271906f, 0.166721f, 1.0f), Shininess: 25.6f)</summary>
        public static Material Bronze => _bronze ?? (_bronze = new Material(new Color4(0.2125f, 0.1275f, 0.054f, 1.0f), new Color4(0.714f, 0.4284f, 0.18144f, 1.0f), new Color4(0.393548f, 0.271906f, 0.166721f, 1.0f), 25.6f));
        private static Material? _bronze;

        /// <summary>Chrome (Ambient: (0.25f, 0.25f, 0.25f, 1.0f), Diffuse: (0.4f, 0.4f, 0.4f, 1.0f), Specular: (0.774597f, 0.774597f, 0.774597f, 1.0f), Shininess: 76.8f)</summary>
        public static Material Chrome => _chrome ?? (_chrome = new Material(new Color4(0.25f, 0.25f, 0.25f, 1.0f), new Color4(0.4f, 0.4f, 0.4f, 1.0f), new Color4(0.774597f, 0.774597f, 0.774597f, 1.0f), 76.8f));
        private static Material? _chrome;

        /// <summary>Copper (Ambient: (0.19125f, 0.0735f, 0.0225f, 1.0f), Diffuse: (0.7038f, 0.27048f, 0.0828f, 1.0f), Specular: (0.256777f, 0.137622f, 0.086014f, 1.0f), Shininess: 12.8f)</summary>
        public static Material Copper => _copper ?? (_copper = new Material(new Color4(0.19125f, 0.0735f, 0.0225f, 1.0f), new Color4(0.7038f, 0.27048f, 0.0828f, 1.0f), new Color4(0.256777f, 0.137622f, 0.086014f, 1.0f), 12.8f));
        private static Material? _copper;

        /// <summary>Gold (Ambient: (0.24725f, 0.1995f, 0.0745f, 1.0f), Diffuse: (0.75164f, 0.60648f, 0.22648f, 1.0f), Specular: (0.628281f, 0.555802f, 0.366065f, 1.0f), Shininess: 51.2f)</summary>
        public static Material Gold => _gold ?? (_gold = new Material(new Color4(0.24725f, 0.1995f, 0.0745f, 1.0f), new Color4(0.75164f, 0.60648f, 0.22648f, 1.0f), new Color4(0.628281f, 0.555802f, 0.366065f, 1.0f), 51.2f));
        private static Material? _gold;

        /// <summary>Silver (Ambient: (0.19225f, 0.19225f, 0.19225f, 1.0f), Diffuse: (0.50754f, 0.50754f, 0.50754f, 1.0f), Specular: (0.508273f, 0.508273f, 0.508273f, 1.0f), Shininess: 51.2f)</summary>
        public static Material Silver => _silver ?? (_silver = new Material(new Color4(0.19225f, 0.19225f, 0.19225f, 1.0f), new Color4(0.50754f, 0.50754f, 0.50754f, 1.0f), new Color4(0.508273f, 0.508273f, 0.508273f, 1.0f), 51.2f));
        private static Material? _silver;

        /// <summary>BlackPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.01f, 0.01f, 0.01f, 1.0f), Specular: (0.50f, 0.50f, 0.50f, 1.0f), Shininess: 32.0f)</summary>
        public static Material BlackPlastic => _blackPlastic ?? (_blackPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.01f, 0.01f, 0.01f, 1.0f), new Color4(0.50f, 0.50f, 0.50f, 1.0f), 32.0f));
        private static Material? _blackPlastic;

        /// <summary>CyanPlastic (Ambient: (0f, 0.1f, 0.06f, 1.0f), Diffuse: (0f, 0.50980392f, 0.50980392f, 1.0f), Specular: (0.50196078f, 0.50196078f, 0.50196078f, 1.0f), Shininess: 32.0f)</summary>
        public static Material CyanPlastic => _cyanPlastic ?? (_cyanPlastic = new Material(new Color4(0f, 0.1f, 0.06f, 1.0f), new Color4(0f, 0.50980392f, 0.50980392f, 1.0f), new Color4(0.50196078f, 0.50196078f, 0.50196078f, 1.0f), 32.0f));
        private static Material? _cyanPlastic;

        /// <summary>GreenPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.1f, 0.35f, 0.1f, 1.0f), Specular: (0.45f, 0.55f, 0.45f, 1.0f), Shininess: 32.0f)</summary>
        public static Material GreenPlastic => _greenPlastic ?? (_greenPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.1f, 0.35f, 0.1f, 1.0f), new Color4(0.45f, 0.55f, 0.45f, 1.0f), 32.0f));
        private static Material? _greenPlastic;

        /// <summary>RedPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0f, 0f, 1.0f), Specular: (0.7f, 0.6f, 0.6f, 1.0f), Shininess: 32.0f)</summary>
        public static Material RedPlastic => _redPlastic ?? (_redPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.5f, 0f, 0f, 1.0f), new Color4(0.7f, 0.6f, 0.6f, 1.0f), 32.0f));
        private static Material? _redPlastic;

        /// <summary>WhitePlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.55f, 0.55f, 0.55f, 1.0f), Specular: (0.70f, 0.70f, 0.70f, 1.0f), Shininess: 32.0f)</summary>
        public static Material WhitePlastic => _whitePlastic ?? (_whitePlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.55f, 0.55f, 0.55f, 1.0f), new Color4(0.70f, 0.70f, 0.70f, 1.0f), 32.0f));
        private static Material? _whitePlastic;

        /// <summary>YellowPlastic (Ambient: (0f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0.5f, 0f, 1.0f), Specular: (0.60f, 0.60f, 0.50f, 1.0f), Shininess: 32.0f)</summary>
        public static Material YellowPlastic => _yellowPlastic ?? (_yellowPlastic = new Material(new Color4(0f, 0f, 0f, 1.0f), new Color4(0.5f, 0.5f, 0f, 1.0f), new Color4(0.60f, 0.60f, 0.50f, 1.0f), 32.0f));
        private static Material? _yellowPlastic;

        /// <summary>BlackRubber (Ambient: (0.02f, 0.02f, 0.02f, 1.0f), Diffuse: (0.01f, 0.01f, 0.01f, 1.0f), Specular: (0.4f, 0.4f, 0.4f, 1.0f), Shininess: 10.0f)</summary>
        public static Material BlackRubber => _blackRubber ?? (_blackRubber = new Material(new Color4(0.02f, 0.02f, 0.02f, 1.0f), new Color4(0.01f, 0.01f, 0.01f, 1.0f), new Color4(0.4f, 0.4f, 0.4f, 1.0f), 10.0f));
        private static Material? _blackRubber;

        /// <summary>CyanRubber (Ambient: (0f, 0.05f, 0.05f, 1.0f), Diffuse: (0.4f, 0.5f, 0.5f, 1.0f), Specular: (0.04f, 0.7f, 0.7f, 1.0f), Shininess: 10.0f)</summary>
        public static Material CyanRubber => _cyanRubber ?? (_cyanRubber = new Material(new Color4(0f, 0.05f, 0.05f, 1.0f), new Color4(0.4f, 0.5f, 0.5f, 1.0f), new Color4(0.04f, 0.7f, 0.7f, 1.0f), 10.0f));
        private static Material? _cyanRubber;

        /// <summary>GreenRubber (Ambient: (0f, 0.05f, 0f, 1.0f), Diffuse: (0.4f, 0.5f, 0.4f, 1.0f), Specular: (0.04f, 0.7f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static Material GreenRubber => _greenRubber ?? (_greenRubber = new Material(new Color4(0f, 0.05f, 0f, 1.0f), new Color4(0.4f, 0.5f, 0.4f, 1.0f), new Color4(0.04f, 0.7f, 0.04f, 1.0f), 10.0f));
        private static Material? _greenRubber;

        /// <summary>RedRubber (Ambient: (0.05f, 0f, 0f, 1.0f), Diffuse: (0.5f, 0.4f, 0.4f, 1.0f), Specular: (0.7f, 0.04f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static Material RedRubber => _redRubber ?? (_redRubber = new Material(new Color4(0.05f, 0f, 0f, 1.0f), new Color4(0.5f, 0.4f, 0.4f, 1.0f), new Color4(0.7f, 0.04f, 0.04f, 1.0f), 10.0f));
        private static Material? _redRubber;

        /// <summary>WhiteRubber (Ambient: (0.05f, 0.05f, 0.05f, 1.0f), Diffuse: (0.5f, 0.5f, 0.5f, 1.0f), Specular: (0.7f, 0.7f, 0.7f, 1.0f), Shininess: 10.0f)</summary>
        public static Material WhiteRubber => _whiteRubber ?? (_whiteRubber = new Material(new Color4(0.05f, 0.05f, 0.05f, 1.0f), new Color4(0.5f, 0.5f, 0.5f, 1.0f), new Color4(0.7f, 0.7f, 0.7f, 1.0f), 10.0f));
        private static Material? _whiteRubber;

        /// <summary>YellowRubber (Ambient: (0.05f, 0.05f, 0f, 1.0f), Diffuse: (0.5f, 0.5f, 0.4f, 1.0f), Specular: (0.7f, 0.7f, 0.04f, 1.0f), Shininess: 10.0f)</summary>
        public static Material YellowRubber => _yellowRubber ?? (_yellowRubber = new Material(new Color4(0.05f, 0.05f, 0f, 1.0f), new Color4(0.5f, 0.5f, 0.4f, 1.0f), new Color4(0.7f, 0.7f, 0.04f, 1.0f), 10.0f));
        private static Material? _yellowRubber;


    }
}
