#nullable enable
using Elffy.Core;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Components
{
    public readonly struct SingleOwnerComponentCore<TComponent> where TComponent : class, ISingleOwnerComponent
    {
        private readonly ComponentOwner? _owner;
        private readonly bool _autoDisposeOnDetached;

        public readonly ComponentOwner? Owner => _owner;
        public readonly bool AutoDisposeOnDetached => _autoDisposeOnDetached;

        public SingleOwnerComponentCore(bool autoDisposeOnDetached)
        {
            _autoDisposeOnDetached = autoDisposeOnDetached;
            _owner = null;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDetached(ComponentOwner owner)
        {
            if(Owner == owner) {
                Unsafe.AsRef(_owner) = null;
            }
        }

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
