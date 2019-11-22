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
    public abstract class FrameObject : IDestroyable
    {
        #region Proeprty
        /// <summary>このオブジェクトがゲームによって管理されているかどうかを返します</summary>
        public bool IsActivated { get; private set; }

        /// <summary>このオブジェクトが開始しているかどうかを返します</summary>
        internal bool IsStarted { get; set; }

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen { get; set; }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>このオブジェクトが、ゲームによって管理されるオブジェクトリストから破棄されているかどうかを返します</summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>
        /// このオブジェクトが所属するレイヤー<para/>
        /// <see cref="IsActivated"/> == true かつ <see cref="IsDestroyed"/> == false の場合常にインスタンスを持ち、そうでない場合常に null です<para/>
        /// </summary>
        public LayerBase? Layer { get; private set; }
        #endregion

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
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
            if(IsActivated) { return; }
            ActivatePrivate(Game.Layers.WorldLayer);
        }

        /// <summary>このオブジェクトを指定のレイヤーでアクティブにします</summary>
        public void Activate(LayerBase layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
            if(IsActivated) { return; }
            ActivatePrivate(layer);
        }

        /// <summary>このオブジェクトをゲーム管理下から外して破棄します</summary>
        public virtual void Destroy()
        {
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
            Layer?.RemoveFrameObject(this);
            Layer = null;
            IsDestroyed = true;
            (this as IDisposable)?.Dispose();
        }

        private void ActivatePrivate(LayerBase layer)
        {
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            Activated?.Invoke(this);
        }
    }
}
