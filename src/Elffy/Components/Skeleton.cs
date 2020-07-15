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
        private TextureUnitNumber _textureUnit;
        private bool _disposed;

        public TextureUnitNumber TextureUnit => _textureUnit;
        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        internal Skeleton(TextureUnitNumber textureUnit)
        {
            _textureUnit = textureUnit;
        }

        ~Skeleton() => Dispose(false);

        internal void Load(ReadOnlySpan<Vector4> bonePositions) => _impl.Load(bonePositions.MarshalCast<Vector4, Color4>());

        public void Apply() => _impl.Apply(TextureUnit);

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
}
