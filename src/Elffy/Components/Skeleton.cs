#nullable enable
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;

namespace Elffy.Components
{
    public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);
        private bool _disposed;

        private FloatDataTexture _bonePositions;

        public ComponentOwner? Owner => _core.Owner;

        public TextureUnitNumber TextureUnit => _bonePositions.TextureUnit;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        internal Skeleton(TextureUnitNumber textureUnit)
        {
            _bonePositions = new FloatDataTexture(textureUnit);
        }

        ~Skeleton() => Dispose(false);

        internal void Load(ReadOnlySpan<Vector4> bonePositions)
        {
            _bonePositions.Load(bonePositions);
        }

        public void Apply()
        {
            _bonePositions.Apply();
        }


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
                _bonePositions.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(Skeleton));
            }
            _disposed = true;
        }
    }
}
