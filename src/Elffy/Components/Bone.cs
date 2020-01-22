#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Shape;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy.Components
{
    /// <summary>Bone component</summary>
    public sealed class Bone : IComponent, IDisposable
    {
        private Model3D? _owner;
        private bool _disposed;
        private UnmanagedArray<Vertex> _vertexArray;
        private UnmanagedArray<int> _indexArray;
        private UnmanagedArray<BoneWeight> _weightArray;
        private UnmanagedArray<BoneTreeElement> _treeElements;
        private int _rootIndex = -1;

        public Bone(ReadOnlySpan<BoneTreeElement> treeElements, ReadOnlySpan<BoneWeight> vertexInfoArray)
        {
            for(int i = 0; i < treeElements.Length; i++) {
                if(treeElements[i].ParentID == null) {
                    _rootIndex = i;
                    break;
                }
            }
            ArgumentChecker.ThrowArgumentIf(_rootIndex < 0, "Can not find root element of bone tree");
            _treeElements = treeElements.ToUnmanagedArray();
            _weightArray = vertexInfoArray.ToUnmanagedArray() ?? throw new ArgumentNullException(nameof(vertexInfoArray));
            _vertexArray = default!;
            _indexArray = default!;
        }

        public void OnAttached(ComponentOwner owner)
        {
            Debug.Assert(owner != null);
            ArgumentChecker.CheckType<Model3D, ComponentOwner>(owner!, $"The component owner must be {nameof(Model3D)}");
            if(_owner != null) { throw new InvalidOperationException("This component has already attatched to another."); }
            var model = (Model3D)owner!;
            _owner = model;

            // 2重に持つ必要ある？
            _vertexArray = model.GetVertexArray().ToUnmanagedArray();
            _indexArray = model.GetIndexArray().ToUnmanagedArray();

            if(_vertexArray.Length != _weightArray.Length) { throw new InvalidOperationException(); }
        }

        public void OnDetached(ComponentOwner owner)
        {
            if(_owner == null) { throw new InvalidOperationException("The component is not attatched."); }
            ArgumentChecker.ThrowArgumentIf(_owner != owner, "owner is invalid");

            _vertexArray.Dispose();
            _indexArray.Dispose();
            _weightArray.Dispose();
            _treeElements.Dispose();
            _vertexArray = null!;
            _indexArray = null!;
            _weightArray = null!;
            _treeElements = null!;
        }

        ~Bone() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    _vertexArray?.Dispose();
                    _indexArray?.Dispose();
                    _weightArray?.Dispose();
                    _treeElements?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    [DebuggerDisplay("{ID}, Parent={ParentID}, ({Position.X}, {Position.Y}, {Position.Z})")]
    public struct BoneTreeElement
    {
        public int ID { get; }
        public int? ParentID { get; }

        public Vector3 Position { get; set; }

        public BoneTreeElement(int id, int? parentID, Vector3 position)
        {
            ID = id;
            ParentID = parentID;
            Position = position;
        }
    }
}
