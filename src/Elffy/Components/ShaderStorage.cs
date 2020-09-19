#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Components
{
    public sealed class ShaderStorage : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<ShaderStorage> _core = new SingleOwnerComponentCore<ShaderStorage>(true);  // Mutable object, Don't change into reaadonly
        private ShaderStorageImpl _impl = new ShaderStorageImpl();

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        ~ShaderStorage() => Dispose(false);

        public void Create<T>(Span<T> data, BufferUsage usage) where T : unmanaged => _impl.Create(data.AsReadOnly(), usage);

        public void Create<T>(ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged => _impl.Create(data, usage);

        public void Update<T>(int offset, Span<T> data) where T : unmanaged => _impl.Update(offset, data.AsReadOnly());

        public void Update<T>(int offset, ReadOnlySpan<T> data) where T : unmanaged => _impl.Update(offset, data);

        public void BindIndex(int index) => _impl.BindIndex(index);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _impl.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(ShaderStorage));
            }
        }

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);
    }

    internal readonly struct ShaderStorageImpl : IDisposable
    {
        private readonly SSBO _ssbo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Create<T>(ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
        {
            ref var ssbo = ref Unsafe.AsRef(_ssbo);
            ssbo = SSBO.Create();
            SSBO.LoadNewData(ref ssbo, data, usage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update<T>(int offset, ReadOnlySpan<T> data) where T : unmanaged
        {
            if(_ssbo.IsEmpty) { throw new InvalidOperationException($"SSBO is not created yet. Call {nameof(Create)} before"); }
            SSBO.UpdateSubData(ref Unsafe.AsRef(_ssbo), offset, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindIndex(int index)
        {
            if(_ssbo.IsEmpty) { throw new InvalidOperationException($"SSBO is not created yet. Call {nameof(Create)} before"); }
            SSBO.BindBase(_ssbo, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            SSBO.Delete(ref Unsafe.AsRef(_ssbo));
        }
    }
}
