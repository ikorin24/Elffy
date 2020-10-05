#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Serialization;
using UnmanageUtility;

namespace Elffy.Shapes
{
    public class Model3D : Renderable
    {
        private readonly UnmanagedArray<Vertex> _vertexArray;
        private readonly UnmanagedArray<int> _indexArray;

        internal Model3D(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            _vertexArray = vertexArray.ToUnmanagedArray();
            _indexArray = indexArray.ToUnmanagedArray();
        }

        public ReadOnlySpan<Vertex> GetVertexArray() => _vertexArray.AsSpan();

        public ReadOnlySpan<int> GetIndexArray() => _indexArray.AsSpan();

        public void UpdateVertex(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            LoadGraphicBuffer(vertexArray, indexArray);
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            LoadGraphicBuffer(_vertexArray.AsSpan(), _indexArray.AsSpan());
        }

        protected override void OnDead()
        {
            base.OnDead();
            _vertexArray.Dispose();
            _indexArray.Dispose();
        }

        public static UniTask<Model3D> LoadResourceAsync(string name)
        {
            return UniTask.Run(n =>
            {
                var name = SafeCast.As<string>(n);
                using(var stream = Resources.GetStream(name)) {
                    var (vertices, indices) = FbxModelBuilder.LoadModel(stream);
                    return new Model3D(vertices, indices);
                }
            }, name);
        }
    }
}
