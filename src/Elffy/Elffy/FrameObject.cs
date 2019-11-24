#nullable enable
using System;
using Elffy.Core;
using Elffy.Exceptions;

namespace Elffy
{
    /// <summary>
    /// ゲームによって管理され、ゲームのフレームに関わるオブジェクトの基底クラス<para/>
    /// ゲームのフレームに関する操作・ゲームによって管理されるための操作を提供します。<para/>
    /// </summary>
    public abstract class FrameObject : ITerminatable
    {
        /// <summary>このオブジェクトがゲームによって管理されているかどうかを返します</summary>
        public bool IsActivated { get; private set; }

        /// <summary>このオブジェクトが開始しているかどうかを返します</summary>
        internal bool IsStarted { get; set; }

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen { get; set; }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>このオブジェクトが、ゲームによって管理されるオブジェクトリストから破棄されているかどうかを返します</summary>
        public bool IsTerminated { get; private set; }

        /// <summary>
        /// このオブジェクトが所属するレイヤー<para/>
        /// <see cref="IsActivated"/> == true かつ <see cref="IsTerminated"/> == false の場合常にインスタンスを持ち、そうでない場合常に null です<para/>
        /// </summary>
        public ILayer? Layer { get; private set; }

        /// <summary>このオブジェクトがアクティブになった時のイベント</summary>
        public event ActionEventHandler<FrameObject>? Activated;
        /// <summary>開始時イベント</summary>
        public event ActionEventHandler<FrameObject>? Started;
        /// <summary>更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? Updated;

        /// <summary>このオブジェクトが更新される最初のフレームに1度のみ実行される処理</summary>
        internal void Start()
        {
            Started?.Invoke(this);
        }

        /// <summary>フレームの更新ごとに実行される更新処理</summary>
        internal void Update()
        {
            Updated?.Invoke(this);
        }

        /// <summary>このオブジェクトをワールドレイヤーでアクティブにします</summary>
        public void Activate()
        {
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            var worldLayer = Game.Layers.WorldLayer;
            Layer = worldLayer;
            worldLayer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        /// <summary>このオブジェクトを指定のレイヤーでアクティブにします</summary>
        public void Activate(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        internal void Activate(SystemLayer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        internal void Activate(UILayer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        public void Activate(ILayer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }

        /// <summary>このオブジェクトをゲーム管理下から外して破棄します</summary>
        public virtual void Terminate()
        {
            if(IsTerminated) { throw new ObjectTerminatedException(this); }
            Layer?.RemoveFrameObject(this);
            Layer = null;
            IsTerminated = true;
            (this as IDisposable)?.Dispose();
        }
    }
}
