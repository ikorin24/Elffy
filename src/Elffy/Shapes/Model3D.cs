#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Elffy.Core;
using Elffy.Effective;
using UnmanageUtility;

namespace Elffy.Shapes
{
    public unsafe class Model3D : Renderable
    {
        private object? _obj;
        private delegate*<Model3D, object?, Delegate, void> _load;  // void func(Model3D self, object obj, Delegate loader)
        private Delegate _loader;

        private Model3D(object? obj, delegate*<Model3D, object?, Delegate, void> load, Delegate loader)
        {
            _obj = obj;
            _load = load;
            _loader = loader;
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            Debug.Assert(_loader is not null);
            Debug.Assert(_load is not null);
            _load(this, _obj, _loader);
        }

        public static Model3D Create<T, TVertex>(T? obj, Action<T, Model3D, Model3DLoadDelegate<TVertex>> loader) where T : class where TVertex : unmanaged
        {
            return new Model3D(obj, &Load, loader);

            // Loader method is called from OnAlive().
            static void Load(Model3D model, object? obj, Delegate loader)
            {
                var typedLoader = SafeCast.As<Action<T, Model3D, Model3DLoadDelegate<TVertex>>>(loader);
                var typedObj = SafeCast.As<T>(obj);
                typedLoader(typedObj, model, model.LoadGraphicBuffer);
            }
        }
    }

    public delegate void Model3DLoadDelegate<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged;
}
