#nullable enable
using Elffy.Core;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Components
{
    // ISingleOwnerComponent の実質的なロジック部を実装した構造体。
    // IValueTaskSource に対する ManualResetValueTaskSourceCore のようなもの。

    /// <summary>
    /// <see cref="ISingleOwnerComponent"/> を継承したクラスの実装を簡略化するためのヘルパー構造体
    /// </summary>
    /// <typeparam name="TComponent">コンポーネントの型</typeparam>
    public readonly struct SingleOwnerComponentCore<TComponent> where TComponent : class, ISingleOwnerComponent
    {
        private readonly ComponentOwner? _owner;
        private readonly bool _autoDisposeOnDetached;

        /// <summary>対象のコンポーネントの所有者を取得します</summary>
        public readonly ComponentOwner? Owner => _owner;

        /// <summary>
        /// コンポーネントを自動的に破棄するかどうかを取得します。<para/>
        /// true の場合、所有者の <see cref="FrameObject.Dead"/> イベント時または <see cref="IComponent.OnDetached(ComponentOwner)"/> 時に破棄が行われます。<para/>
        /// </summary>
        public readonly bool AutoDisposeOnDetached => _autoDisposeOnDetached;

        /// <summary><see cref="ISingleOwnerComponent"/> のロジックを提供する構造体を生成します</summary>
        /// <param name="autoDisposeOnDetached">対象のコンポーネントを自動で破棄するかどうか</param>
        public SingleOwnerComponentCore(bool autoDisposeOnDetached)
        {
            _autoDisposeOnDetached = autoDisposeOnDetached;
            _owner = null;
        }

        /// <summary><see cref="IComponent.OnAttached(ComponentOwner)"/> の処理を行います。</summary>
        /// <param name="owner">コンポーネントの所有者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnAttached(ComponentOwner owner)
        {
            if(_owner is null == false) {
                throw new InvalidOperationException($"This component is already attached. Can not have multi {nameof(ComponentOwner)}s.");
            }

            Unsafe.AsRef(_owner) = owner ?? throw new ArgumentNullException(nameof(owner));
            if(_autoDisposeOnDetached) {
                owner.Dead += sender =>
                {
                    Debug.Assert(sender is ComponentOwner);
                    Unsafe.As<ComponentOwner>(sender).RemoveComponent<TComponent>();
                };
            }
        }

        /// <summary>
        /// <see cref="IComponent.OnDetached(ComponentOwner)"/> の処理を行います。<para/>
        /// [NOTE] コンポーネントが <see cref="IDisposable"/> の場合は <see cref="OnDetachedForDisposable{T}(ComponentOwner, T)"/> を使用してください。<para/>
        /// </summary>
        /// <param name="owner">コンポーネントの所有者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDetached(ComponentOwner owner)
        {
            if(Owner == owner) {
                Unsafe.AsRef(_owner) = null;
            }
        }

        /// <summary>
        /// <see cref="IComponent.OnDetached(ComponentOwner)"/> の処理を行います。<para/>
        /// [NOTE] コンポーネントが <see cref="IDisposable"/> でない場合は <see cref="OnDetached(ComponentOwner)"/> を使用してください。<para/>
        /// </summary>
        /// <typeparam name="T">コンポーネントの型 (<see cref="IDisposable"/>)</typeparam>
        /// <param name="owner">コンポーネントの所有者</param>
        /// <param name="self"><see cref="IDisposable"/> を継承したコンポーネント</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDetachedForDisposable<T>(ComponentOwner owner, T self) where T : IDisposable
        {
            if(Owner == owner) {
                Unsafe.AsRef(_owner) = null;
                if(AutoDisposeOnDetached) {
                    self.Dispose();
                }
            }
        }
    }
}
