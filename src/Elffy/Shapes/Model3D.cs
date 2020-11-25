#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Shapes
{
    public unsafe class Model3D : Renderable
    {
        private object? _obj;
        private delegate*<Model3D, object?, Delegate, void> _callbackOnActivated;  // void func(Model3D self, object obj, Delegate loader)
        private Delegate? _builder;

        private Model3D(object? obj, delegate*<Model3D, object?, Delegate, void> callbackOnAlive, Delegate builder)
        {
            Debug.Assert(builder is not null);
            _obj = obj;
            _callbackOnActivated = callbackOnAlive;
            _builder = builder;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            if(IsTerminated) {
                return;
            }
            Debug.Assert(_builder is not null);
            Debug.Assert(_callbackOnActivated is not null);

            try {
                _callbackOnActivated(this, _obj, _builder);
            }
            catch {
                Terminate();
            }
            finally {
                _obj = null;
                _callbackOnActivated = null;
                _builder = null;
            }
        }

        // This method is used by Model3DLoadDelegate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LoadGraphicBufferInternal<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged
        {
            LoadGraphicBuffer(vertices, indices);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Model3D Create<T>(T? obj, Action<T, Model3D, Model3DLoadDelegate> builder) where T : class
        {
            return new Model3D(obj, &CallbackOnActivated, builder);

            // ジェネリクス型<T>をローカル関数に含め、関数ポインタを渡すことで、
            // builder の呼び出しを OnActivated() まで遅延させつつ、<T>を復元できる。
            static void CallbackOnActivated(Model3D model, object? obj, Delegate builder)
            {
                // Restore types of builder and obj.
                var typedBuilder = SafeCast.As<Action<T, Model3D, Model3DLoadDelegate>>(builder);
                var typedObj = SafeCast.As<T>(obj);
                
                // Call builder
                typedBuilder(typedObj, model, new Model3DLoadDelegate(model));
            }
        }
    }

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
}
