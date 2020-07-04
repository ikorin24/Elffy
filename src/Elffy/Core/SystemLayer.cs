#nullable enable

using System;

namespace Elffy.Core
{
    internal sealed class SystemLayer : ILayer
    {
        private readonly FrameObjectStore _store = new FrameObjectStore();

        /// <summary>このレイヤーを持つ親</summary>
        internal LayerCollection Owner { get; }
        LayerCollection? ILayer.OwnerCollection => Owner;

        /// <summary>現在生きている全オブジェクトの数を取得します</summary>
        public int ObjectCount => _store.ObjectCount;

        internal SystemLayer(LayerCollection owner)
        {
            Owner = owner;
        }

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        public void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        public void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        internal void ApplyRemove() => _store.ApplyRemove();

        internal void ApplyAdd() => _store.ApplyAdd();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        /// <summary>フレームの更新を行います</summary>
        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        public void ClearFrameObject() => _store.ClearFrameObject();
    }
}
