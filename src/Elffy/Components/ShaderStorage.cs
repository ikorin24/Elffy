#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;

namespace Elffy.Components
{
    public sealed class ShaderStorage : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<ShaderStorage> _core = new SingleOwnerComponentCore<ShaderStorage>(true);
        private SSBO _ssbo;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        ~ShaderStorage() => Dispose(false);

        public void Create<T>(Span<T> data, BufferUsage usage) where T : unmanaged
            => Create(data.AsReadOnly(), usage);


        public void Create<T>(ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
        {
            _ssbo = SSBO.Create();
            SSBO.LoadNewData(ref _ssbo, data, usage);
        }

        public void Update<T>(int offset, Span<T> data) where T : unmanaged
            => Update(offset, data.AsReadOnly());

        public void Update<T>(int offset, ReadOnlySpan<T> data) where T : unmanaged
        {
            if(_ssbo.IsEmpty) { throw new InvalidOperationException($"{nameof(ShaderStorage)} is not created yet. Call {nameof(Create)} before"); }
            SSBO.UpdateSubData(ref _ssbo, offset, data);
        }

        public void BindIndex(int index)
        {
            if(_ssbo.IsEmpty) { throw new InvalidOperationException("SSBO is empty"); }
            SSBO.BindBase(_ssbo, index);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_ssbo.IsEmpty) { return; }
            if(disposing) {
                SSBO.Delete(ref _ssbo);
            }
            else {
                throw new MemoryLeakException(typeof(ShaderStorage));
            }
        }

        public void OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        public void OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);
    }
}
