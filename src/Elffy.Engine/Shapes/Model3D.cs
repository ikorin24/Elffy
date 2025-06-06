﻿#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Shading;

namespace Elffy.Shapes
{
    /// <summary><see cref="Renderable"/> which users can inject how to build and how to render.</summary>
    public sealed class Model3D : Renderable
    {
        private object? _obj;
        private unsafe delegate*<Model3D, object?, Delegate, UniTask> _onActivating;    // UniTask func(Model3D self, object? obj, Delegate builder)
        private Delegate? _builder;
        private readonly Model3DRenderingDelegate? _onRendering;

        private unsafe Model3D(object? obj,
                               delegate*<Model3D, object?, Delegate, UniTask> onActivating,
                               Delegate builder,
                               Model3DRenderingDelegate? onRendering)
        {
            Debug.Assert(builder is not null);
            _obj = obj;
            _onActivating = onActivating;
            _builder = builder;
            _onRendering = onRendering;
            Activating.Subscribe((f, ct) => SafeCast.As<Model3D>(f).OnActivating(ct));
        }

        private async UniTask OnActivating(CancellationToken cancellationToken)
        {
            Debug.Assert(_builder is not null);
            unsafe {
                Debug.Assert(_onActivating is not null);
            }

            try {
                await InvokeCallback(this);
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }
            finally {
                _obj = null;
                _builder = null;
                unsafe {
                    _onActivating = null;
                }
            }

            static unsafe UniTask InvokeCallback(Model3D model3D)
            {
                Debug.Assert(model3D._builder is not null);
                return model3D._onActivating(model3D, model3D._obj, model3D._builder);
            }
        }

        protected override void OnRendering(in RenderingContext context)
        {
            if(_onRendering is null) {
                base.OnRendering(in context);
            }
            else {
                _onRendering.Invoke(in context, new Model3DDrawElementsDelegate(this));
            }
        }

        // This method is used by Model3DLoadMeshDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LoadMeshInternal<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged, IVertex
        {
            LoadMesh(vertices, indices);
        }

        // This method is used by Model3DLoadMeshDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DrawElementsInternal(int startIndex, uint indexCount)
        {
            DrawElements(startIndex * sizeof(int), indexCount);
        }

        /// <summary>Create new <see cref="Model3D"/> by using specified builder.</summary>
        /// <typeparam name="T">type of the builder argument</typeparam>
        /// <param name="obj">the argument of the builder</param>
        /// <param name="builder">builder method delegate</param>
        /// <param name="onRendering">rendering method delegate (null if use default rendering)</param>
        /// <returns>new <see cref="Model3D"/> instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Model3D Create<T>(T? obj,
                                               Model3DBuilderDelegate<T> builder,
                                               Model3DRenderingDelegate? onRendering = null) where T : class
        {
            if(builder is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(builder));
            }

            return new Model3D(obj, &CallbackOnActivating, builder, onRendering);

            static UniTask CallbackOnActivating(Model3D model, object? obj, Delegate builder)
            {
                // Restore types of builder and obj.
                var typedBuilder = SafeCast.As<Model3DBuilderDelegate<T?>>(builder);
                var typedObj = SafeCast.As<T>(obj);

                // Call builder
                return typedBuilder(typedObj, model, new Model3DLoadMeshDelegate(model));
            }
        }
    }

    public delegate UniTask Model3DBuilderDelegate<T>(T obj, Model3D model3D, Model3DLoadMeshDelegate loadMesh) where T : class?;

    public delegate void Model3DRenderingDelegate(in RenderingContext context, Model3DDrawElementsDelegate drawElements);

    public readonly struct Model3DLoadMeshDelegate : IEquatable<Model3DLoadMeshDelegate>
    {
        private readonly Model3D? _model;

        internal Model3DLoadMeshDelegate(Model3D model)
        {
            _model = model;
        }

        /// <summary>Load mesh to <see cref="Model3D"/></summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices to load <see cref="Model3D"/></param>
        /// <param name="indices">indices to load <see cref="Model3D"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged, IVertex
        {
            _model?.LoadMeshInternal(vertices, indices);
        }

        /// <summary>Load mesh to <see cref="Model3D"/></summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices to load <see cref="Model3D"/></param>
        /// <param name="indices">indices to load <see cref="Model3D"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<TVertex>(Span<TVertex> vertices, Span<int> indices) where TVertex : unmanaged, IVertex
        {
            _model?.LoadMeshInternal(vertices.AsReadOnly(), indices.AsReadOnly());
        }

        public override bool Equals(object? obj) => obj is Model3DLoadMeshDelegate d && Equals(d);

        public bool Equals(Model3DLoadMeshDelegate other) => ReferenceEquals(_model, other._model);

        public override int GetHashCode() => _model?.GetHashCode() ?? 0;

        public static bool operator ==(Model3DLoadMeshDelegate left, Model3DLoadMeshDelegate right) => left.Equals(right);

        public static bool operator !=(Model3DLoadMeshDelegate left, Model3DLoadMeshDelegate right) => !(left == right);
    }

    public readonly struct Model3DDrawElementsDelegate : IEquatable<Model3DDrawElementsDelegate>
    {
        private readonly Model3D? _model;

        internal Model3DDrawElementsDelegate(Model3D model)
        {
            _model = model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke()
        {
            _model?.DrawElementsInternal(0, _model.IBO.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int startIndex, uint indexCount)
        {
            _model?.DrawElementsInternal(startIndex, indexCount);
        }

        public override bool Equals(object? obj) => obj is Model3DDrawElementsDelegate d && Equals(d);

        public bool Equals(Model3DDrawElementsDelegate other) => ReferenceEquals(_model, other._model);

        public override int GetHashCode() => _model?.GetHashCode() ?? 0;

        public static bool operator ==(Model3DDrawElementsDelegate left, Model3DDrawElementsDelegate right) => left.Equals(right);

        public static bool operator !=(Model3DDrawElementsDelegate left, Model3DDrawElementsDelegate right) => !(left == right);
    }
}
