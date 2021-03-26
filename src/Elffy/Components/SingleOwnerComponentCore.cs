#nullable enable
using Elffy.Core;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Components
{
    // A structure that implements the actual logic part of ISingleOwnerComponent.
    // Something like ManualResetValueTaskSourceCore for IValueTaskSource.

    /// <summary>Helper struct to implement <see cref="ISingleOwnerComponent"/> easily.</summary>
    public struct SingleOwnerComponentCore
    {
        private ComponentOwner? _owner;
        private readonly bool _autoDisposeOnDetached;

        /// <summary>Get the owner of the component</summary>
        public readonly ComponentOwner? Owner => _owner;

        /// <summary>Get whether the component is automatically disposed on detached if the target component is <see cref="IDisposable"/>.</summary>
        public readonly bool AutoDisposeOnDetached => _autoDisposeOnDetached;

        /// <summary>Create a new <see cref="SingleOwnerComponentCore"/> instance</summary>
        /// <param name="autoDisposeOnDetached">whether the component is automatically disposed on detached.</param>
        public SingleOwnerComponentCore(bool autoDisposeOnDetached)
        {
            _autoDisposeOnDetached = autoDisposeOnDetached;
            _owner = null;
        }

        /// <summary>Do <see cref="IComponent.OnAttached(ComponentOwner)"/></summary>
        /// <param name="owner">the owner of the component</param>
        /// <param name="component">component instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnAttached<TComponent>(ComponentOwner owner, TComponent component) where TComponent : class, ISingleOwnerComponent
        {
            if(_owner is null == false) {
                ThrowAlreadyLoaded();
                [DoesNotReturn] static void ThrowAlreadyLoaded() => throw new InvalidOperationException($"This component is already attached. Can not have multi {nameof(ComponentOwner)}s.");
            }
            if(owner is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(owner));
            }
            if(typeof(TComponent) != component.GetType()) {
                ThrowTypeMismatch(component.GetType());

                [DoesNotReturn] static void ThrowTypeMismatch(Type instanceType)
                    => throw new ArgumentException($"Component type is mismatch. Attached as {typeof(TComponent).FullName}, but instance is {instanceType.FullName}.");
            }

            _owner = owner;
            if(_autoDisposeOnDetached) {
                owner.Dead += sender => SafeCast.As<ComponentOwner>(sender).RemoveComponent<TComponent>();
            }
        }

        /// <summary>Do <see cref="IComponent.OnDetached(ComponentOwner)"/></summary>
        /// <typeparam name="TComponent">type of the component</typeparam>
        /// <param name="owner">the owner of the component</param>
        /// <param name="component">the component</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDetached<TComponent>(ComponentOwner owner, TComponent component) where TComponent : class, ISingleOwnerComponent
        {
            if(Owner == owner) {
                _owner = null;
                if(typeof(TComponent).IsAssignableTo(typeof(IDisposable))) {
                    if(_autoDisposeOnDetached) {
                        Unsafe.As<IDisposable>(component).Dispose();
                    }
                }
            }
        }
    }
}
