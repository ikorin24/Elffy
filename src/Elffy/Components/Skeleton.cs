#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elffy.Components
{
    public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private bool _disposed;

        private FloatDataTexture _bonePositions;

        public ComponentOwner? Owner { get; private set; }

        public TextureUnitNumber TextureUnit => _bonePositions.TextureUnit;

        public bool AutoDisposeOnDetached => true;

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


        public void OnAttached(ComponentOwner owner)
        {
            if(Owner is null == false) { throw new InvalidOperationException($"{nameof(Skeleton)} is already attatched."); }
            if(_disposed) { throw new ObjectDisposedException(nameof(Skeleton)); }
            Owner = owner;
            if(AutoDisposeOnDetached) {
                Owner.Dead += sender =>
                {
                    Debug.Assert(sender is ComponentOwner);
                    Unsafe.As<ComponentOwner>(sender).RemoveComponent<Skeleton>();
                };
            }
        }

        public void OnDetached(ComponentOwner owner)
        {
            if(Owner == owner) {
                Owner = null;
            }
            if(AutoDisposeOnDetached) {
                Dispose();
            }
        }

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
