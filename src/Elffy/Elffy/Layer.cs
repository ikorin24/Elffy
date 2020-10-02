#nullable enable
using Elffy.Core;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using Elffy.Exceptions;
using System;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})", Type = nameof(Layer), TargetTypeName = nameof(Layer))]
    public class Layer : ILayer
    {
        private readonly FrameObjectStore _store = new FrameObjectStore();

        /// <summary>
        /// このレイヤーを持つ親 (<see cref="LayerCollection"/>)<para/>
        /// ※ このレイヤーを <see cref="LayerCollection"/> に追加する時、必ず <see cref="LayerCollection"/> をこのプロパティに入れるように実装されなければならない。
        /// 削除時は null を必ず入れる。<para/>
        /// </summary>
        internal LayerCollection? Owner { get; set; }
        LayerCollection? ILayer.OwnerCollection => Owner;

        public string Name { get; }

        public ReadOnlySpan<Light> Lights => _store.Lights;

        /// <summary>レイヤー名を指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="name">レイヤー名</param>
        public Layer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        internal Layer(string name, LayerCollection owner)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Owner = owner;
        }

        /// <summary>現在生きている全オブジェクトの数を取得します</summary>
        public int ObjectCount => _store.ObjectCount;

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        internal void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        void ILayer.AddFrameObject(FrameObject frameObject) => AddFrameObject(frameObject);

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        internal void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        void ILayer.RemoveFrameObject(FrameObject frameObject) => RemoveFrameObject(frameObject);

        internal void ApplyRemove() => _store.ApplyRemove();

        internal void ApplyAdd() => _store.ApplyAdd();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        /// <summary>フレームの更新を行います</summary>
        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        internal void ClearFrameObject() => _store.ClearFrameObject();
        void ILayer.ClearFrameObject() => ClearFrameObject();

        /// <summary>画面への投影行列とカメラ行列を指定して、描画を実行します</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">カメラ行列</param>
        internal unsafe void Render(in Matrix4 projection, in Matrix4 view)
        {
            foreach(var renderable in _store.Renderables) {
                if(!renderable.IsRoot || !renderable.IsVisible) { continue; }
                renderable.Render(projection, view, Matrix4.Identity);
            }
        }
    }
}
