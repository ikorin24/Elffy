#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.Threading;

namespace Elffy
{
    /// <summary>
    /// フレーム更新に関わるオブジェクトの基底クラス<para/>
    /// フレームに関する操作・エンジンによって管理されるための操作を提供します。<para/>
    /// </summary>
    public abstract class FrameObject
    {
        private IHostScreen? _hostScreen;
        private Dispatcher? _dispatcher;
        private ILayer? _layer;
        private object? _tag;
        private FrameObjectLifeState _lifeState = FrameObjectLifeState.New;

        internal FrameObjectLifeState LifeState => _lifeState;

        public bool IsClean => _lifeState.IsNew();

        public bool IsAlive => _lifeState.HasAliveBit();

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen
        {
            get => _lifeState.HasFrozenBit();
            set => _lifeState = value ? (_lifeState | FrameObjectLifeState.Bit_Frozen)
                                      : (_lifeState & ~FrameObjectLifeState.Bit_Frozen);
        }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public ref object? Tag => ref _tag;

        /// <summary>
        /// このオブジェクトが所属するレイヤー。
        /// <see cref="Activate(Elffy.Layer)"/> が呼ばれてから <see cref="Terminate"/> が呼ばれるまでの間は常にインスタンスを持ち、それ以外の場合常に null です。
        /// </summary>
        private protected ILayer? Layer => _layer;

        /// <summary>Get HostScreen of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IHostScreen HostScreen => (_hostScreen ??= _layer?.Owner?.Owner?.Owner) ?? throw new InvalidOperationException();

        /// <summary>Get Dispatcher of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public Dispatcher Dispatcher => _dispatcher ??= HostScreen.Dispatcher;

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
            _lifeState |= FrameObjectLifeState.Bit_Started;
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

        /// <summary>このオブジェクトを指定のレイヤーでアクティブにします</summary>
        public virtual void Activate(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            ArgumentChecker.ThrowArgumentIf(layer.Owner == null, $"{nameof(layer)} is not associated with {nameof(IHostScreen)}.");
            if(_lifeState != FrameObjectLifeState.New) { return; }

            _lifeState |= FrameObjectLifeState.Bit_Activating;
            _layer = layer;
            layer.AddFrameObject(this);
        }

        internal void Activate<TLayer>(TLayer layer) where TLayer : class, ILayer
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            Debug.Assert(layer is Layer == false, "Layer は具象型のオーバーロードを通っていないとおかしい。");
            Debug.Assert(layer.Owner != null);
            if(_lifeState != FrameObjectLifeState.New) { return; }

            _lifeState |= FrameObjectLifeState.Bit_Activating;
            _layer = layer;
            layer.AddFrameObject(this);
        }

        /// <summary>このオブジェクトをエンジン管理下から外して破棄します</summary>
        public void Terminate()
        {
            if(_lifeState.IsNew() || _lifeState.HasTerminatingBit() || _lifeState.HasDeadBit()) { return; }
            _lifeState |= FrameObjectLifeState.Bit_Terminating;
            _layer?.RemoveFrameObject(this);
        }

        internal void AddToObjectStoreCallback()
        {
            // activating ビットをおろす、alive ビットをたてる
            _lifeState &= ~FrameObjectLifeState.Bit_Activating;
            _lifeState |= FrameObjectLifeState.Bit_Alive;

            Activated?.Invoke(this);
        }

        internal void RemovedFromObjectStoreCallback()
        {
            // alive ビットをおろす、terminating ビットをおろす、dead ビットをたてる
            _lifeState &= ~(FrameObjectLifeState.Bit_Alive | FrameObjectLifeState.Bit_Terminating);
            _lifeState |= FrameObjectLifeState.Bit_Dead;

            _layer = null;
            _dispatcher = null;
            _hostScreen = null;
            (this as IDisposable)?.Dispose();
            Terminated?.Invoke(this);
        }
    }
}
