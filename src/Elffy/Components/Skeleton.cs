#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;

namespace Elffy.Components
{
    public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);
        private FloatDataTextureImpl _impl = new FloatDataTextureImpl();
        private FloatDataTextureImpl _moveOffset = new FloatDataTextureImpl();
        private bool _disposed;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        internal Skeleton()
        {
        }

        ~Skeleton() => Dispose(false);

        internal void Load(ReadOnlySpan<Vector4> bonePositions) => _impl.Load(bonePositions.MarshalCast<Vector4, Color4>());

        public void Apply(TextureUnitNumber textureUnit) => _impl.Apply(textureUnit);

        public void OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        public void OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }
            if(disposing) {
                _impl.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(Skeleton));
            }
            _disposed = true;
        }
    }

    //public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    //{
    //    private const BufferUsage Usage = BufferUsage.StaticDraw;

    //    private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);
    //    private ShaderStorageImpl _ssImpl = new ShaderStorageImpl();
    //    private bool _disposed;

    //    public ComponentOwner? Owner => _core.Owner;

    //    public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

    //    internal Skeleton()
    //    {
    //    }

    //    ~Skeleton() => Dispose(false);

    //    internal void Create(ReadOnlySpan<Vector4> bonePositions) => _ssImpl.Create(bonePositions, Usage);

    //    internal void Update(int offset, ReadOnlySpan<Vector4> update) => _ssImpl.Update(offset, update);

    //    public void BindIndex(int index) => _ssImpl.BindIndex(index);

    //    public void OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

    //    public void OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);

    //    public void Dispose()
    //    {
    //        GC.SuppressFinalize(this);
    //        Dispose(true);
    //    }

    //    private void Dispose(bool disposing)
    //    {
    //        if(_disposed) { return; }
    //        if(disposing) {
    //            _ssImpl.Dispose();
    //        }
    //        else {
    //            throw new MemoryLeakException(typeof(Skeleton));
    //        }
    //        _disposed = true;
    //    }
    //}
}
