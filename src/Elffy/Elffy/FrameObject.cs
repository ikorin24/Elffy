#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
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
        private ILayer? _layer;
        private object? _tag;
        private FrameObjectLifeState _state = FrameObjectLifeState.New;
        private bool _isFrozen;

        /// <summary><see cref="Activate(Elffy.Layer)"/> が呼ばれたときのイベント</summary>
        public event ActionEventHandler<FrameObject>? Activated;
        /// <summary><see cref="Terminate"/> が呼ばれたときのイベント</summary>
        public event ActionEventHandler<FrameObject>? Terminated;
        /// <summary><see cref="IsAlive"/> が true になった最初のフレームに発火するイベント</summary>
        public event ActionEventHandler<FrameObject>? Alive;
        /// <summary><see cref="IsAlive"/> が false になった最初のフレームに発火するベント</summary>
        public event ActionEventHandler<FrameObject>? Dead;
        /// <summary>事前更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? EarlyUpdated;
        /// <summary>更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? Updated;
        /// <summary>事後更新時イベント</summary>
        public event ActionEventHandler<FrameObject>? LateUpdated;

        public FrameObjectLifeState LifeState => _state;

        public bool IsNew => _state == FrameObjectLifeState.New;

        public bool IsActivated => _state == FrameObjectLifeState.Activated;

        public bool IsAlive => _state == FrameObjectLifeState.Alive;

        public bool IsTerminated => _state == FrameObjectLifeState.Terminated;

        public bool IsDead => _state == FrameObjectLifeState.Dead;

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen { get => _isFrozen; set => _isFrozen = value; }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public object? Tag { get => _tag; set => _tag = value; }

        /// <summary>
        /// このオブジェクトが所属するレイヤー。
        /// <see cref="Activate(Elffy.Layer)"/> が呼ばれてから <see cref="Terminate"/> が呼ばれるまでの間は常にインスタンスを持ち、それ以外の場合常に null です。
        /// </summary>
        private protected ILayer? InternalLayer => _layer;


        // Layer クラス以外の internal なレイヤーに乗るオブジェクトはこのプロパティを呼んではいけない。代わりに ILayer の方を使う。
        /// <summary>このオブジェクトのレイヤーを取得します</summary>
        /// <exception cref="InvalidOperationException"> <see cref="FrameObject"/> is not activated yet or already dead.</exception>
        public Layer Layer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AssemblyState.IsDebug ? (Layer?)_layer ?? throw new InvalidOperationException()
                                         : Unsafe.As<Layer?>(_layer) ?? throw new InvalidOperationException();

            // ↑Unsafe は怖いのでデバッグ時は通常キャストする。JITで分岐は消える。
        }


        /// <summary>Get HostScreen of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"> <see cref="FrameObject"/> is not activated yet or already dead.</exception>
        public IHostScreen HostScreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _hostScreen ?? TryGetSceen(_layer);

                static IHostScreen TryGetSceen(ILayer? layer) => GetHostScreen(layer) ?? throw new InvalidOperationException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EarlyUpdate()
        {
            OnEarlyUpdate();
        }

        /// <summary>フレームの更新ごとに実行される更新処理</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update()
        {
            OnUpdate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LateUpdate()
        {
            OnLateUpdte();
        }

        /// <summary>このオブジェクトを指定のレイヤーでアクティブにします</summary>
        public void Activate(Layer layer)
        {
            if(layer is null) { ThrowNullArg(); }
            if(_state != FrameObjectLifeState.New) { return; }

            var screen = GetHostScreen(layer);
            if(screen is null) {
                ThrowInvalidLayer();
            }
            else {
                screen.ThrowIfNotMainThread();
                _hostScreen = screen;
            }

            _state = FrameObjectLifeState.Activated;
            _layer = layer;
            layer!.AddFrameObject(this);
            OnActivated();

            static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
            static void ThrowInvalidLayer() => throw new ArgumentException($"{nameof(layer)} is not associated with {nameof(IHostScreen)}");
        }

        internal void Activate<TLayer>(TLayer layer) where TLayer : class, ILayer
        {
            if(layer is null) { ThrowNullArg(); }
            Debug.Assert(layer is Layer == false, "Layer は具象型のオーバーロードを通っていないとおかしい。");
            Debug.Assert(layer!.OwnerCollection is null == false);
            if(_state != FrameObjectLifeState.New) { return; }

            Debug.Assert(GetHostScreen(layer)!.IsThreadMain());

            _state = FrameObjectLifeState.Activated;
            _layer = layer;
            layer.AddFrameObject(this);
            OnActivated();

            static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
        }

        /// <summary>このオブジェクトをエンジン管理下から外して破棄します</summary>
        public void Terminate()
        {
            if(_state != FrameObjectLifeState.Alive) { return; }
            Debug.Assert(_layer is null == false);

            _state = FrameObjectLifeState.Terminated;
            _layer!.RemoveFrameObject(this);
            OnTerminated();
        }

        protected virtual void OnEarlyUpdate() => EarlyUpdated?.Invoke(this);

        protected virtual void OnUpdate() => Updated?.Invoke(this);

        protected virtual void OnLateUpdte() => LateUpdated?.Invoke(this);

        protected virtual void OnActivated() => Activated?.Invoke(this);

        protected virtual void OnTerminated() => Terminated?.Invoke(this);

        protected virtual void OnAlive() => Alive?.Invoke(this);

        protected virtual void OnDead() => Dead?.Invoke(this);

        internal void AddToObjectStoreCallback()
        {
            Debug.Assert(_state == FrameObjectLifeState.Activated);
            _state = FrameObjectLifeState.Alive;
            OnAlive();
        }

        internal void RemovedFromObjectStoreCallback()
        {
            Debug.Assert(_state == FrameObjectLifeState.Terminated);
            _state = FrameObjectLifeState.Dead;
            _layer = null;
            _hostScreen = null;
            OnDead();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IHostScreen? GetHostScreen<TLayer>(TLayer? layer) where TLayer : class, ILayer
        {
            return layer?.OwnerCollection?.OwnerRenderingArea.OwnerScreen;
        }
    }
}
