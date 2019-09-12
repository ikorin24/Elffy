using System;
using System.Collections.Generic;
using Elffy.Serialization;
using Elffy.UI;

namespace Elffy
{
    public abstract class GameScene
    {
        /// <summary>現在ロードされている <see cref="GameScene"/></summary>
        public static GameScene Current { get; private set; }

        internal ICollection<FrameObject> FrameObjects { get; set; }

        internal Page UI { get; set; }

        public delegate void SceneEventHandler();

        /// <summary>指定したシーンを読み込みます</summary>
        /// <typeparam name="T">読み込みを行う <see cref="GameScene"/> 継承クラス</typeparam>
        public static void Load<T>() where T : GameScene, new()
        {
            var scene = LoadWithoutActivating<T>();
            scene.Activate();
            Current = scene;
        }

        internal static T LoadWithoutActivating<T>() where T : GameScene, new()
        {
            var scene = new T();
            return scene;
        }

        protected virtual void Initialize() { }

        protected virtual void Activate() { }
    }
}
