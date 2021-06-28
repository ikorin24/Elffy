#nullable enable
using FbxTools;
using System.Collections;
using System.Collections.Generic;

namespace Elffy.Serialization.Fbx
{
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
