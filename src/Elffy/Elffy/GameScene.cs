using System;
using System.Collections.Generic;
using Elffy.Serialization;

namespace Elffy
{
    public abstract class GameScene
    {
        internal ICollection<FrameObject> FrameObjects { get; set; }

        public delegate void SceneEventHandler();

        public event SceneEventHandler Loaded;

        /// <summary>指定したシーンを読み込みます</summary>
        /// <typeparam name="T">読み込みを行う <see cref="GameScene"/> 継承クラス</typeparam>
        public static void Load<T>() where T : GameScene, new()
        {
            var scene = LoadWithoutInitializing<T>();
            scene.InitializeComponent();
        }

        public static T LoadWithoutInitializing<T>() where T : GameScene, new()
        {
            var parser = new SceneParser();
            var scene = parser.Parse<T>($"{typeof(T).Name}.xml");
            return scene;
        }

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
