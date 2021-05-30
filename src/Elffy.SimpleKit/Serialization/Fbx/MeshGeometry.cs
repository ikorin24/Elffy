#nullable enable
using FbxTools;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    internal struct MeshGeometry
    {
        public long ID;
        public RawString Name;
        public RawArray<int> IndicesRaw;
        public RawArray<double> Positions;
        public RawArray<double> Normals;
        public RawArray<int> NormalIndices;
        public MappingInformationType NormalMappingType;
        public ReferenceInformationType NormalReferenceType;
        public RawArray<double> UV;
        public RawArray<int> UVIndices;
        public MappingInformationType UVMappingType;
        public ReferenceInformationType UVReferenceType;
        public RawArray<int> Materials;
    }

    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal struct Texture
    {
        public long ID;
        public RawString FileName;

        public Texture(long id, RawString fileName)
        {
            ID = id;
            FileName = fileName;
        }

        private string DebugDisplay() => $"(id:{ID}) \"{FileName.ToString()}\"";
    }

    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal struct Connection
    {
        public ConnectionType ConnectionType;
        public long SourceID;
        public long DestID;

        public Connection(ConnectionType type, long source, long dest)
        {
            ConnectionType = type;
            SourceID = source;
            DestID = dest;
        }

        private string DebugDisplay() => $"({ConnectionType}) {SourceID} -- {DestID}";
    }

    internal struct ConnectionList : IEnumerable<Connection>
    {
        private readonly FbxNode _connections;

        internal ConnectionList(FbxNode connectionsNode)
        {
            _connections = connectionsNode;
        }

        public Enumerator GetEnumerator() => new Enumerator(_connections);

        IEnumerator<Connection> IEnumerable<Connection>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal struct Enumerator : IEnumerator<Connection>
        {
            private FbxNodeList.Enumerator _e;
            private Connection _current;

            internal Enumerator(FbxNode connections)
            {
                _e = connections.Children.GetEnumerator();
                _current = default;
            }

            public Connection Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if(_e.MoveNext() == false) {
                    return false;
                }
                var props = _e.Current.Properties;
                _current = new Connection(props[0].AsString().ToConnectionType(), props[1].AsInt64(), props[2].AsInt64());
                return true;
            }

            public void Reset()
            {
                _e.Reset();
            }
        }
    }
}
