#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.Threading;
using Elffy.AssemblyServices;

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

        /// <summary><see cref="Activate(Elffy.Layer)"/> が呼ばれたときのイベント</summary>
        public event ActionEventHandler<FrameObject>? Activated;
        /// <summary><see cref="Terminate"/> が呼ばれたときのイベント</summary>
        public event ActionEventHandler<FrameObject>? Terminated;
        /// <summary><see cref="IsAlive"/> が true になった時のイベント</summary>
        public event ActionEventHandler<FrameObject>? Alive;
        /// <summary><see cref="IsAlive"/> が false になった時のベント</summary>
        public event ActionEventHandler<FrameObject>? Dead;
        /// <summary>開始時イベント</summary>
        public event ActionEventHandler<FrameObject>? Started;
        /// <summary>事前更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? EarlyUpdated;
        /// <summary>更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? Updated;
        /// <summary>事後更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? LateUpdated;

        internal FrameObjectLifeState LifeState => _lifeState;

        public bool IsClean => _lifeState.IsNew();

        public bool IsAlive => _lifeState.HasAliveBit();

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen
        {
            get => _lifeState.HasFrozenBit();

            // 変更するビット以外は触らないように
            set => _lifeState = value ? (_lifeState | FrameObjectLifeState.Bit_Frozen)
                                      : (_lifeState & ~FrameObjectLifeState.Bit_Frozen);
        }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public ref object? Tag => ref _tag;

        /// <summary>
        /// このオブジェクトが所属するレイヤー。
        /// <see cref="Activate(Elffy.Layer)"/> が呼ばれてから <see cref="Terminate"/> が呼ばれるまでの間は常にインスタンスを持ち、それ以外の場合常に null です。
        /// </summary>
        private protected ILayer? InternalLayer => _layer;


        // Layer クラス以外の internal なレイヤーに乗るオブジェクトはこのプロパティを呼んではいけない。代わりに ILayer の方を使う。
        /// <summary>このオブジェクトのレイヤーを取得します</summary>
        /// <exception cref="InvalidOperationException"><see cref="IsAlive"/> が false です。</exception>
        public Layer Layer => AssemblyState.IsDebug ? (Layer?)_layer ?? throw new InvalidOperationException()
                                                    : Unsafe.As<Layer?>(_layer) ?? throw new InvalidOperationException();
        // ↑Unsafe は怖いのでデバッグ時は通常キャストする。JITで分岐は消える。


        /// <summary>Get HostScreen of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"><see cref="IsAlive"/> が false です。</exception>
        public IHostScreen HostScreen => (_hostScreen ??= _layer?.OwnerCollection?.OwnerRenderingArea?.OwnerScreen) ?? throw new InvalidOperationException();

        /// <summary>Get Dispatcher of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public Dispatcher Dispatcher => _dispatcher ??= HostScreen.Dispatcher;

        /// <summary>このオブジェクトが更新される最初のフレームに1度のみ実行される処理</summary>
        internal void Start()
        {
            // 変更するビット以外は触らないように
            // Started ビットを立てる
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
        public void Activate(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            ArgumentChecker.ThrowArgumentIf(layer.Owner == null, $"{nameof(layer)} is not associated with {nameof(IHostScreen)}.");
            if(_lifeState != FrameObjectLifeState.New) { return; }

            // 変更するビット以外は触らないように
            // Activating ビットを立てる
            _lifeState |= FrameObjectLifeState.Bit_Activating;
            _layer = layer;
            layer.AddFrameObject(this);
            OnActivated();
        }

        internal void Activate<TLayer>(TLayer layer) where TLayer : class, ILayer
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            Debug.Assert(layer is Layer == false, "Layer は具象型のオーバーロードを通っていないとおかしい。");
            Debug.Assert(layer.OwnerCollection != null);
            if(_lifeState != FrameObjectLifeState.New) { return; }

            // 変更するビット以外は触らないように
            // Activating ビットを立てる
            _lifeState |= FrameObjectLifeState.Bit_Activating;
            _layer = layer;
            layer.AddFrameObject(this);
            OnActivated();
        }

        /// <summary>このオブジェクトをエンジン管理下から外して破棄します</summary>
        public void Terminate()
        {
            if(_lifeState.IsNew() || _lifeState.HasTerminatingBit() || _lifeState.HasDeadBit()) { return; }

            // 変更するビット以外は触らないように
            // Terminating ビットを立てる
            _lifeState |= FrameObjectLifeState.Bit_Terminating;
            Debug.Assert(_layer is null == false);

            _layer!.RemoveFrameObject(this);
            OnTerminated();
        }

        protected virtual void OnActivated()
        {
            Activated?.Invoke(this);
        }

        protected virtual void OnTerminated()
        {
            Terminated?.Invoke(this);
        }

        protected virtual void OnAlive()
        {
            Alive?.Invoke(this);
        }

        protected virtual void OnDead()
        {
            Dead?.Invoke(this);
            (this as IDisposable)?.Dispose();       // TODO: 継承先にIDisposeがないの確認したら消す。継承先に責任を持って破棄させる
        }

        internal void AddToObjectStoreCallback()
        {
            // 変更するビット以外は触らないように
            // activating ビットをおろす、alive ビットをたてる
            _lifeState = _lifeState & ~FrameObjectLifeState.Bit_Activating
                                    | FrameObjectLifeState.Bit_Alive;
            OnAlive();
        }

        internal void RemovedFromObjectStoreCallback()
        {
            // 変更するビット以外は触らないように
            // alive ビットをおろす、terminating ビットをおろす、dead ビットをたてる
            _lifeState = _lifeState & ~FrameObjectLifeState.Bit_Alive
                                    & ~FrameObjectLifeState.Bit_Terminating
                                    | FrameObjectLifeState.Bit_Dead;
            _layer = null;
            _dispatcher = null;
            _hostScreen = null;
            OnDead();
        }
    }
}
