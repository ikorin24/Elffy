﻿#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;
using System.Diagnostics;
using UnmanageUtility;

namespace Elffy.Components
{
    public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);    // Mutable object, Don't change into reaadonly
        private FloatDataTextureImpl _boneMoveData = new FloatDataTextureImpl();    // Mutable object, Don't change into reaadonly
        private UnmanagedArray<Vector4>? _positions;
        private UnmanagedArray<Vector4>? _move;
        private bool _disposed;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public int BoneCount => _boneMoveData.Length;

        internal Skeleton()
        {
        }

        ~Skeleton() => Dispose(false);

        /// <summary></summary>
        /// <param name="bonePositions">ボーンのデータ (メソッドの外側で解放しないでください)</param>
        internal void Load(UnmanagedArray<Vector4> bonePositions)
        {
            if(bonePositions is null) { throw new ArgumentNullException(nameof(bonePositions)); }

            _positions?.Dispose();
            _move?.Dispose();
            _positions = bonePositions;
            var move = new UnmanagedArray<Vector4>(bonePositions.Length);
            _boneMoveData.Load(move.AsSpan().MarshalCast<Vector4, Color4>());
            _move = move;
        }

        internal void Load(ReadOnlySpan<Vector4> bonePositions) => Load(bonePositions.ToUnmanagedArray());

        public void Apply(TextureUnitNumber textureUnit) => _boneMoveData.Apply(textureUnit);

        void IComponent.OnAttached(ComponentOwner owner)
        {
            _core.OnAttached(owner);
            owner.EarlyUpdated += OwnerEarlyUpdated;
        }

        void IComponent.OnDetached(ComponentOwner owner)
        {
            owner.EarlyUpdated -= OwnerEarlyUpdated;
            _core.OnDetachedForDisposable(owner, this);
        }

        private void OwnerEarlyUpdated(FrameObject sender)
        {
            Debug.Assert(_move is null == false);

            var move = _move.AsSpan();
            //for(int i = 0; i < move.Length; i++) {
            //    move[i] += new Vector4(0.02f, 0, 0, 0);
            //}
            // ここでボーン動かす
            //_boneMoveData.Dispose();
            //_boneMoveData = new FloatDataTextureImpl();
            //_boneMoveData.Load(move.MarshalCast<Vector4, Color4>());
            _boneMoveData.Update(move.MarshalCast<Vector4, Color4>(), 0);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }

            _positions?.Dispose();
            _positions = null;
            _move?.Dispose();
            _move = null;

            if(disposing) {
                _boneMoveData.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(Skeleton));
            }
            _disposed = true;
        }
    }
}