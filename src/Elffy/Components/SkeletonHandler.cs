#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Components
{
    /// <summary>Translation matrices handler of <see cref="Skeleton"/></summary>
    public readonly unsafe partial struct SkeletonHandler : IDisposable
    {
        private readonly Skeleton _skeleton;
        private readonly Matrix4* _pos;
        private readonly Matrix4* _translation;
        private readonly BoneInternal* _tree;
        private readonly int _length;

        public int BoneCount => _length;

        /// <summary>Get or set translation matrix of specified bone</summary>
        /// <remarks>The coordinate of the matrix is bone-local for the parent bone.</remarks>
        /// <param name="index">index to get translation matrix</param>
        /// <returns>translation matrix of specified index</returns>
        public ref Matrix4 this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return ref _translation[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SkeletonHandler(Skeleton skeleton, Matrix4* pos, Matrix4* translation, BoneInternal* tree, int length)
        {
            Debug.Assert(length >= 0);
            _skeleton = skeleton;
            _pos = pos;
            _translation = translation;
            _tree = tree;
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetParentIndex(int index, out int parentIndex)
        {
            if((uint)index >= (uint)_length) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            var parent = _tree[index].Parent;
            if(parent == null) {
                parentIndex = -1;
                return false;
            }
            parentIndex = parent->ID;
            Debug.Assert((uint)parentIndex < (uint)_length);
            return true;
        }

        /// <summary>Get translation matrices as <see cref="Span{T}"/></summary>
        /// <returns>translation matrices</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Matrix4> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref *_translation, _length);
        }

        /// <summary>Flush to update translation matrices.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _skeleton?.UpdateTranslations();
        }
    }
}
