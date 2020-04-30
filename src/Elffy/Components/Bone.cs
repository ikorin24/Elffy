#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Shape;
using System;
using System.Diagnostics;
using UnmanageUtility;

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

            model.Updated += OnOwnerUpdated;
        }

        public void OnDetached(ComponentOwner owner)
        {
            if(_owner == null) { throw new InvalidOperationException("The component is not attatched."); }
            ArgumentChecker.ThrowArgumentIf(_owner != owner, "owner is invalid");

            owner.Updated -= OnOwnerUpdated;

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

        private void OnOwnerUpdated(FrameObject frameObject)
        {
            return;
            // TODO: 消す テスト用
            Debug.WriteLine("Bone Update");
            using(var v = new UnmanagedArray<Vertex>(_vertexArray.Length)) {
                for(int i = 0; i < v.Length; i++) {
                    var b0 = _treeElements[_weightArray[i].RefBone0].Position;
                    var b1 = _treeElements[_weightArray[i].RefBone1].Position;
                    var b2 = _treeElements[_weightArray[i].RefBone2].Position;
                    var b3 = _treeElements[_weightArray[i].RefBone3].Position;
                    var w0 = _weightArray[i].Weight0;
                    var w1 = _weightArray[i].Weight1;
                    var w2 = _weightArray[i].Weight2;
                    var w3 = _weightArray[i].Weight3;
                    var pos = _vertexArray[i].Position;

                    var resultPos = pos + (b0 * w0) + (b1 * w1);
                    v[i] = new Vertex(resultPos, v[i].Normal, v[i].Color, v[i].TexCoord);

                }
                _owner!.UpdateVertex(v.AsSpan(), _indexArray.AsSpan());
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
