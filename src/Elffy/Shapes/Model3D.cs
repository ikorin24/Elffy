#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Elffy.Core;
using UnmanageUtility;

namespace Elffy.Shapes
{
    public unsafe class Model3D : Renderable
    {
        private object? _vertices;  // This field is null after loaded.
        private object? _indices;   // This field is null after loaded.
        private delegate*<Model3D, object, object, void> _loader;   // This field is null after loaded.

        private Model3D(object vertices, object indices, delegate*<Model3D, object, object, void> loader)
        {
            Debug.Assert(vertices is not null);
            Debug.Assert(indices is not null);
            Debug.Assert(loader is not null);
            _vertices = vertices;
            _indices = indices;
            _loader = loader;
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            Debug.Assert(_vertices is not null);
            Debug.Assert(_indices is not null);

            // Load vertices and indices
            _loader(this, _vertices, _indices);
            _vertices = null;
            _indices = null;
            _loader = null;
        }

        /// <summary>Create <see cref="Model3D"/> with specified vertices and indices data.</summary>
        /// <remarks>
        /// [NOTE] Don't dispose <paramref name="vertices"/> and <paramref name="indices"/>. They are automatically disposed by <see cref="Model3D"/> after loaded.
        /// </remarks>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices of type <typeparamref name="TVertex"/></param>
        /// <param name="indices">indices of type <see cref="int"/></param>
        /// <returns><see cref="Model3D"/> instance</returns>
        public static Model3D Create<TVertex>(UnmanagedArray<TVertex> vertices, UnmanagedArray<int> indices) where TVertex : unmanaged
        {
            if(vertices is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(vertices));
            }
            if(indices is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(indices));
            }

            return new Model3D(vertices, indices, &Loader);

            // Loader method is called from OnAlive().
            static void Loader(Model3D model, object vertices, object indices)
            {
                var vert = SafeCast.As<UnmanagedArray<TVertex>>(vertices);
                var ind = SafeCast.As<UnmanagedArray<int>>(indices);
                model.LoadGraphicBuffer(vert.AsSpan(), ind.AsSpan());
            }
        }

        public static Model3D Create<TVertex>(UnmanagedList<TVertex> vertices, UnmanagedList<int> indices) where TVertex : unmanaged
        {
            if(vertices is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(vertices));
            }
            if(indices is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(indices));
            }

            return new Model3D(vertices, indices, &Loader);

            // Loader method is called from OnAlive().
            static void Loader(Model3D model, object vertices, object indices)
            {
                var vert = SafeCast.As<UnmanagedList<TVertex>>(vertices);
                var ind = SafeCast.As<UnmanagedList<int>>(indices);
                model.LoadGraphicBuffer(vert.AsSpan(), ind.AsSpan());
            }
        }

        /// <summary>Create <see cref="Model3D"/> with specified vertices and indices data.</summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices of type <typeparamref name="TVertex"/></param>
        /// <param name="indices">indices of type <see cref="int"/></param>
        /// <returns><see cref="Model3D"/> instance</returns>
        public static Model3D Create<TVertex>(TVertex[] vertices, int[] indices) where TVertex : unmanaged
        {
            if(vertices is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(vertices));
            }
            if(indices is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(indices));
            }

            return new Model3D(vertices, indices, &Loader);

            // Loader method is called from OnAlive().
            static void Loader(Model3D model, object vertices, object indices)
            {
                var vert = SafeCast.As<TVertex[]>(vertices).AsSpan();
                var ind = SafeCast.As<int[]>(indices).AsSpan();
                model.LoadGraphicBuffer(vert, ind);
            }
        }

        /// <summary>Create <see cref="Model3D"/> with specified vertices and indices data, which MUST be pinned and in heap memory.</summary>
        /// <remarks>
        /// [NOTE] <paramref name="vertices"/> and <paramref name="indices"/> must be pinned and in heap memory.<para/>
        /// If they are unmanaged memory, specify handles as default(<see cref="GCHandle"/>), otherwise their types must be <see cref="GCHandleType.Pinned"/>.
        /// </remarks>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices of type <typeparamref name="TVertex"/></param>
        /// <param name="verticesLen">length of <paramref name="vertices"/></param>
        /// <param name="verticesHandle"><see cref="GCHandle"/> of <paramref name="vertices"/>. (See method remarks.)</param>
        /// <param name="indices">indices of type <see cref="int"/></param>
        /// <param name="indicesLen">length of <paramref name="indices"/></param>
        /// <param name="indicesHandle"><see cref="GCHandle"/> of <paramref name="indices"/>. (See method remarks.)</param>
        /// <returns><see cref="Model3D"/> instance</returns>
        public static Model3D CreateFromPinned<TVertex>(TVertex* vertices, int verticesLen, GCHandle verticesHandle, int* indices, int indicesLen, GCHandle indicesHandle)
            where TVertex : unmanaged
        {
            var vert = Pinned.GetInstance(vertices, verticesLen, verticesHandle);
            var ind = Pinned.GetInstance(indices, indicesLen, indicesHandle);
            return new Model3D(vert, ind, &Loader);

            static void Loader(Model3D model, object vertices, object indices)
            {
                // Pinning is released on disposed
                using var vert = SafeCast.As<Pinned>(vertices);
                using var ind = SafeCast.As<Pinned>(indices);
                model.LoadGraphicBuffer(new ReadOnlySpan<TVertex>(vert.Pointer, vert.Length),
                                        new ReadOnlySpan<int>(vert.Pointer, vert.Length));
            }
        }

        // Wrapper class of pinned pointer
        private sealed class Pinned : IDisposable
        {
            private GCHandle _handle;
            public void* Pointer { get; private set; }
            public int Length { get; private set; }

            private Pinned(void* pointer, int length, GCHandle handle)
            {
                Pointer = pointer;
                Length = length;
                _handle = handle;
            }

            public void Dispose()
            {
                // Release if pinned
                if(_handle.IsAllocated) {
                    _handle.Free();
                }
                _handle = default;
                Pointer = null;
                Length = 0;
            }

            public static Pinned GetInstance(void* pointer, int length, GCHandle handle)
            {
                // HACK: I have no idea that I should pool instances or not from the perspective of performance.
                return new Pinned(pointer, length, handle);
            }
        }
    }
}
