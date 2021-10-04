#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Components.Implementation
{
    // A structure that implements the actual logic part of ISingleOwnerComponent.
    // Something like ManualResetValueTaskSourceCore for IValueTaskSource.

    /// <summary>Helper struct to implement <see cref="ISingleOwnerComponent"/> easily.</summary>
    public struct SingleOwnerComponentCore
    {
        private ComponentOwner? _owner;
        private readonly bool _dontDisposeOnDetached;

        /// <summary>Get the owner of the component</summary>
        public readonly ComponentOwner? Owner => _owner;

        /// <summary>Get whether the component is automatically disposed on detached if the target component is <see cref="IDisposable"/>.</summary>
        public readonly bool AutoDisposeOnDetached => !_dontDisposeOnDetached;

        /// <summary>Create a new <see cref="SingleOwnerComponentCore"/> instance</summary>
        /// <param name="autoDisposeOnDetached">whether the component is automatically disposed on detached.</param>
        public SingleOwnerComponentCore(bool autoDisposeOnDetached)
        {
            _dontDisposeOnDetached = !autoDisposeOnDetached;
            _owner = null;
        }

        /// <summary>Do <see cref="IComponent.OnAttached(ComponentOwner)"/></summary>
        /// <param name="owner">the owner of the component</param>
        /// <param name="component">component instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnAttached<TComponent>(ComponentOwner owner, TComponent component) where TComponent : class, ISingleOwnerComponent
        {
            if(owner is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(owner));
            }
            if(Interlocked.CompareExchange(ref _owner, owner, null) is not null) {
                ThrowAlreadyLoaded();
                [DoesNotReturn] static void ThrowAlreadyLoaded() => throw new InvalidOperationException($"This component is already attached. Can not have multi {nameof(ComponentOwner)}s.");
            }
            _owner = owner;
        }

        /// <summary>Do <see cref="IComponent.OnDetached(ComponentOwner)"/></summary>
        /// <typeparam name="TComponent">type of the component</typeparam>
        /// <param name="owner">the owner of the component</param>
        /// <param name="component">the component</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDetached<TComponent>(ComponentOwner owner, TComponent component) where TComponent : class, ISingleOwnerComponent
        {
            if(Interlocked.CompareExchange(ref _owner, null, owner) == owner) {
                if(_dontDisposeOnDetached == false) {
                    component.Dispose();
                }
            }
        }
    }
}
