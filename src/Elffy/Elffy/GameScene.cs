using System;
using System.Collections.Generic;
using Elffy.Serialization;
using Elffy.UI;

namespace Elffy
{
    public abstract class GameScene
    {
        private const string SCENE_FILE_EXT = "xml";

        /// <summary>現在ロードされている <see cref="GameScene"/></summary>
        public static GameScene Current { get; private set; }

        internal ICollection<FrameObject> FrameObjects { get; set; }

        internal Page UI { get; set; }

        public delegate void SceneEventHandler();

        public event SceneEventHandler Loaded;

        /// <summary>指定したシーンを読み込みます</summary>
        /// <typeparam name="T">読み込みを行う <see cref="GameScene"/> 継承クラス</typeparam>
        public static void Load<T>() where T : GameScene, new()
        {
            var scene = LoadWithoutInitializing<T>();
            scene.InitializeComponent();
            Current = scene;
        }

        internal static T LoadWithoutInitializing<T>() where T : GameScene, new()
        {
            var parser = new SceneParser();
            var scene = parser.Parse<T>($"{typeof(T).Name}.{SCENE_FILE_EXT}");
            return scene;
        }

        protected virtual void Initialize() { }

        private void InitializeComponent()
        {
            foreach(var obj in FrameObjects) {
                obj.Activate();
            }
            FrameObjects = null;
            Loaded?.Invoke();
        }
    }
}
