#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Core;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})")]
    public class Layer : ILayer
    {
        private readonly FrameObjectStore _store = FrameObjectStore.New();
        private LayerCollection? _owner;

        /// <summary>
        /// このレイヤーを持つ親 (<see cref="LayerCollection"/>)<para/>
        /// ※ このレイヤーを <see cref="LayerCollection"/> に追加する時、必ず <see cref="LayerCollection"/> をこのプロパティに入れるように実装されなければならない。
        /// 削除時は null を必ず入れる。<para/>
        /// </summary>
        internal LayerCollection? Owner
        {
            get => _owner;
            set => _owner = value;
        }
        LayerCollection? ILayer.OwnerCollection => Owner;

        /// <summary>Get name of the layer</summary>
        public string Name { get; }

        /// <inheritdoc/>
        public bool IsVisible { get; set; } = true;

        /// <summary>Create new <see cref="Layer"/> with specified name.</summary>
        /// <param name="name">name of the layer</param>
        public Layer(string name)
        {
            if(name is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }
            Name = name;
        }

        /// <summary>Get count of alive objects in the current frame.</summary>
        public int ObjectCount => _store.ObjectCount;

        internal void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        void ILayer.AddFrameObject(FrameObject frameObject) => AddFrameObject(frameObject);

        internal void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        void ILayer.RemoveFrameObject(FrameObject frameObject) => RemoveFrameObject(frameObject);

        internal void ApplyRemove() => _store.ApplyRemove();

        internal void ApplyAdd() => _store.ApplyAdd();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        internal void ClearFrameObject() => _store.ClearFrameObject();
        void ILayer.ClearFrameObject() => ClearFrameObject();

        internal void Render(in Matrix4 projection, in Matrix4 view) => _store.Render(projection, view);
    }
}
