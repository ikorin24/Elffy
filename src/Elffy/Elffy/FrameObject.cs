#nullable enable
using System;
using System.Diagnostics;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.Threading;

namespace Elffy
{
    /// <summary>
    /// フレーム更新に関わるオブジェクトの基底クラス<para/>
    /// フレームに関する操作・エンジンによって管理されるための操作を提供します。<para/>
    /// </summary>
    public abstract class FrameObject : ITerminatable
    {
        private Dispatcher? _dispatcher;

        /// <summary>このオブジェクトがエンジンによって管理されているかどうかを返します</summary>
        public bool IsActivated { get; private set; }

        /// <summary>このオブジェクトが開始しているかどうかを返します</summary>
        internal bool IsStarted { get; set; }

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen { get; set; }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>このオブジェクトが、エンジンによって管理されるオブジェクトリストから破棄されているかどうかを返します</summary>
        public bool IsTerminated { get; private set; }

        /// <summary>
        /// このオブジェクトが所属するレイヤー<para/>
        /// <see cref="IsActivated"/> == true かつ <see cref="IsTerminated"/> == false の場合常にインスタンスを持ち、そうでない場合常に null です<para/>
        /// </summary>
        private protected ILayer? Layer { get; private set; }

        /// <summary>Get <see cref="Elffy.Threading.Dispatcher"/> of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public Dispatcher Dispatcher => _dispatcher ?? (_dispatcher = Layer?.Owner?.Owner?.Owner?.Dispatcher) ??
                                        throw new InvalidOperationException($"{nameof(FrameObject)} is not activated yet or already terminated.");

        /// <summary>このオブジェクトがアクティブになった時のイベント</summary>
        public event ActionEventHandler<FrameObject>? Activated;
        /// <summary>このオブジェクトが終了した時のイベント</summary>
        public event ActionEventHandler<FrameObject>? Terminated;
        /// <summary>開始時イベント</summary>
        public event ActionEventHandler<FrameObject>? Started;
        /// <summary>事前更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? EarlyUpdated;
        /// <summary>更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? Updated;
        /// <summary>事後更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? LateUpdated;

        /// <summary>このオブジェクトが更新される最初のフレームに1度のみ実行される処理</summary>
        internal void Start()
        {
            Started?.Invoke(this);
        }

        internal void EarlyUpdate()
        {
            EarlyUpdated?.Invoke(this);
        }

        /// <summary>フレームの更新ごとに実行される更新処理</summary>
        internal void Update()
        {
            Updated?.Invoke(this);
        }

        internal void LateUpdate()
        {
            LateUpdated?.Invoke(this);
        }

        /// <summary>このオブジェクトをデフォルトのレイヤーでアクティブにします</summary>
        //public void Activate()
        //{
        //    if(IsTerminated) { throw new ObjectTerminatedException(this); }
        //    if(IsActivated) { return; }
        //    var worldLayer = CurrentScreen.Layers.WorldLayer;
        //    Debug.Assert(worldLayer.Owner != null);
        //    Layer = worldLayer;
        //    worldLayer.AddFrameObject(this);
        //    IsActivated = true;
        //    Activated?.Invoke(this);
        //}

        /// <summary>このオブジェクトを指定のレイヤーでアクティブにします</summary>
        public void Activate(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            ArgumentChecker.ThrowArgumentIf(layer.Owner == null, $"{nameof(layer)} is not associated with {nameof(IHostScreen)}.");
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        internal void Activate<TLayer>(TLayer layer) where TLayer : class, ILayer
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            Debug.Assert(layer.Owner != null);
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        /// <summary>このオブジェクトをゲーム管理下から外して破棄します</summary>
        public void Terminate()
        {
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            Layer?.RemoveFrameObject(this);
            Layer = null;
            IsTerminated = true;
            _dispatcher = null;
            (this as IDisposable)?.Dispose();
            Terminated?.Invoke(this);
        }
    }
}
