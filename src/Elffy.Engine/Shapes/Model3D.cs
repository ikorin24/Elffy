#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;

namespace Elffy.Shapes
{
    /// <summary><see cref="Renderable"/> which users can inject how to build and how to render.</summary>
    public sealed class Model3D : Renderable
    {
        private object? _obj;
        private unsafe delegate*<Model3D, object?, Delegate, UniTask> _callbackOnActivated;  // UniTask func(Model3D self, object? obj, Delegate builder)
        private Delegate? _builder;
        private readonly Model3DRenderingDelegate? _onRendering;

        private unsafe Model3D(object? obj,
                               delegate*<Model3D, object?, Delegate, UniTask> callbackOnAlive,
                               Delegate builder,
                               Model3DRenderingDelegate? onRendering)
        {
            Debug.Assert(builder is not null);
            _obj = obj;
            _callbackOnActivated = callbackOnAlive;
            _builder = builder;
            _onRendering = onRendering;
        }

        protected override async UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken)
        {
            Debug.Assert(_builder is not null);
            unsafe {
                Debug.Assert(_callbackOnActivated is not null);
            }

            try {
                await InvokeCallback(this, _obj, _builder);
                cancellationToken.ThrowIfCancellationRequested();
                return AsyncUnit.Default;

                unsafe UniTask InvokeCallback(Model3D model, object? obj, Delegate builder) => _callbackOnActivated(model, obj, builder);
            }
            finally {
                _obj = null;
                _builder = null;
                unsafe {
                    _callbackOnActivated = null;
                }
            }
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(_onRendering is null) {
                base.OnRendering(model, view, projection);
            }
            else {
                _onRendering.Invoke(this, model, view, projection, new(this));
            }
        }

        // This method is used by Model3DLoadMeshDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LoadMeshInternal<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            LoadMesh(vertices, indices);
        }

        // This method is used by Model3DLoadMeshDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DrawElementsInternal(int startIndex, int indexCount)
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

            // ジェネリクス型<T>をローカル関数に含め、関数ポインタを渡すことで、
            // builder の呼び出しを OnActivating() まで遅延させつつ、<T>を復元できる。
            // 参考: https://ikorin2.hatenablog.jp/entry/2021/01/15/110845

            // Capture the generics type <T> in the local function and store its function pointer in the instance.
            // Model3D can restore the generics type as <T>, not 'Type', when calls OnActivating().
            // Reference (my blog in Japanese): https://ikorin2.hatenablog.jp/entry/2021/01/15/110845

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

    public delegate void Model3DRenderingDelegate(Model3D model3D, in Matrix4 model, in Matrix4 view, in Matrix4 projection, Model3DDrawElementsDelegate drawElements);

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
        public void Invoke<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            _model?.LoadMeshInternal(vertices, indices);
        }

        /// <summary>Load mesh to <see cref="Model3D"/></summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices to load <see cref="Model3D"/></param>
        /// <param name="indices">indices to load <see cref="Model3D"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<TVertex>(Span<TVertex> vertices, Span<int> indices) where TVertex : unmanaged
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
        public void Invoke(int startIndex, int indexCount)
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
