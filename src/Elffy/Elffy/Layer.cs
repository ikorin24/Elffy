#nullable enable
using System;
using System.Diagnostics;
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Shading;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})", Type = nameof(Layer), TargetTypeName = nameof(Layer))]
    public class Layer : ILayer, IDisposable
    {
        private readonly FrameObjectStore _store = FrameObjectStore.New();
        private LayerCollection? _owner;
        private PostProcessImpl _postProcessImpl;   // mutable object, don't make it readonly.

        /// <summary>
        /// このレイヤーを持つ親 (<see cref="LayerCollection"/>)<para/>
        /// ※ このレイヤーを <see cref="LayerCollection"/> に追加する時、必ず <see cref="LayerCollection"/> をこのプロパティに入れるように実装されなければならない。
        /// 削除時は null を必ず入れる。<para/>
        /// </summary>
        internal LayerCollection? Owner
        {
            get => _owner;
            set
            {
                if(value is null) {
                    Dispose();      // dispose when removed from owner.
                }
                _owner = value;
            }
        }
        LayerCollection? ILayer.OwnerCollection => Owner;

        public string Name { get; }

        public ReadOnlySpan<Light> Lights => _store.Lights;

        public PostProcess? PostProcess
        {
            get => _postProcessImpl.PostProcess;
            set => _postProcessImpl.PostProcess = value;
        }

        /// <inheritdoc/>
        public bool IsVisible { get; set; } = true;

        /// <summary>レイヤー名を指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="name">レイヤー名</param>
        public Layer(string name)
        {
            if(name is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }
            Name = name!;
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

        /// <summary>Render all <see cref="FrameObject"/>s in this layer</summary>
        /// <param name="projection">projection matrix</param>
        /// <param name="view">view matrix</param>
        /// <param name="currentScope">current scope of <see cref="FBO"/></param>
        internal void Render(in Matrix4 projection, in Matrix4 view, in FrameBufferScope currentScope)
        {
            _postProcessImpl.ApplyChange();

            using(var scope = _postProcessImpl.GetScope(currentScope)) {
                foreach(var renderable in _store.Renderables) {
                    if(!renderable.IsRoot || !renderable.IsVisible) { continue; }
                    renderable.Render(projection, view, Matrix4.Identity);
                }
            }
        }

        public void Dispose()
        {
            // [NOTE]
            // Dispose() is called when removed from an owner layer collection.
            // Dispose resources, but this instance does not die
            // because default layer in layer collection (e.g. world layer) must be survived
            // when called LayerCollection.Clear().
            // Don't check already disposed.

            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing) {
                // Release managed resources
                _postProcessImpl.Dispose();
            }
        }
    }
}
