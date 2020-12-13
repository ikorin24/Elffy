#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Shapes
{
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

        protected override async void OnActivated()
        {
            base.OnActivated();
            if(LifeState == LifeState.Terminated) {  // if terminated in event of activation
                return;
            }
            Debug.Assert(_builder is not null);
            unsafe {
                Debug.Assert(_callbackOnActivated is not null);
            }

            try {
                await InvokeCallback(this, _obj, _builder);

                unsafe UniTask InvokeCallback(Model3D model, object? obj, Delegate builder) => _callbackOnActivated(model, obj, builder);
            }
            catch {
                var screen = HostScreen;
                if(!screen.RunningToken.IsCancellationRequested) {
                    if(!screen.IsThreadMain) {
                        await screen.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.Update);
                    }
                    Terminate();
                }
                // Don't throw. No one can catch it.
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

        // This method is used by Model3DLoadDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LoadGraphicBufferInternal<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            LoadGraphicBuffer(vertices, indices);
        }

        // This method is used by Model3DLoadDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DrawElementsInternal(int startIndex, int indexCount)
        {
            DrawElements(indexCount, startIndex * sizeof(int));
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

            return new Model3D(obj, &CallbackOnActivated, builder, onRendering);

            // ジェネリクス型<T>をローカル関数に含め、関数ポインタを渡すことで、
            // builder の呼び出しを OnActivated() まで遅延させつつ、<T>を復元できる。
            static UniTask CallbackOnActivated(Model3D model, object? obj, Delegate builder)
            {
                // Restore types of builder and obj.
                var typedBuilder = SafeCast.As<Model3DBuilderDelegate<T>>(builder);
                var typedObj = SafeCast.As<T>(obj);
                
                // Call builder
                return typedBuilder(typedObj, model, new Model3DLoadDelegate(model));
            }
        }
    }

    public delegate UniTask Model3DBuilderDelegate<T>(T obj, Model3D model3D, Model3DLoadDelegate load) where T : class;

    public delegate void Model3DRenderingDelegate(Model3D model3D, in Matrix4 model, in Matrix4 view, in Matrix4 projection, Model3DDrawElementsDelegate drawElements);

    public readonly struct Model3DLoadDelegate
    {
        private readonly Model3D _model;

        internal Model3DLoadDelegate(Model3D model)
        {
            _model = model;
        }

        /// <summary>Load vertices and indices to <see cref="Model3D"/></summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices to load <see cref="Model3D"/></param>
        /// <param name="indices">indices to load <see cref="Model3D"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            _model.LoadGraphicBufferInternal(vertices, indices);
        }

        /// <summary>Load vertices and indices to <see cref="Model3D"/></summary>
        /// <typeparam name="TVertex">type of vertex</typeparam>
        /// <param name="vertices">vertices to load <see cref="Model3D"/></param>
        /// <param name="indices">indices to load <see cref="Model3D"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<TVertex>(Span<TVertex> vertices, Span<int> indices) where TVertex : unmanaged
        {
            _model.LoadGraphicBufferInternal(vertices.AsReadOnly(), indices.AsReadOnly());
        }
    }

    public readonly struct Model3DDrawElementsDelegate
    {
        private readonly Model3D _model;

        internal Model3DDrawElementsDelegate(Model3D model)
        {
            _model = model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke()
        {
            _model.DrawElementsInternal(0, _model.IBO.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int startIndex, int indexCount)
        {
            _model.DrawElementsInternal(startIndex, indexCount);
        }
    }
}
