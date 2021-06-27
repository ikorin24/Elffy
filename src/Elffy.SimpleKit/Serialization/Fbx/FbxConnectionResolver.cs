#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using FbxTools;
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    internal struct FbxConnectionResolver // Be careful, mutable object
    {
        private BufferPooledDictionary<long, int>? _srcToDest;
        private UnsafeRawList<UnsafeRawList<long>> _destLists;

        private BufferPooledDictionary<long, int>? _destToSrc;
        private UnsafeRawList<UnsafeRawList<long>> _srcLists;

        public FbxConnectionResolver(FbxNode connectionsNode)
        {
            _srcToDest = null;
            _destLists = default;
            _destToSrc = null;
            _srcLists = default;
            CreateConnectionDic(connectionsNode);
        }

        private void CreateConnectionDic(FbxNode connections)
        {
            var count = connections.Children.Count;
            _destLists = UnsafeRawList<UnsafeRawList<long>>.New(capacity: count);
            _srcToDest = new BufferPooledDictionary<long, int>(capacity: count);
            _destToSrc = new BufferPooledDictionary<long, int>(capacity: count);
            _srcLists = UnsafeRawList<UnsafeRawList<long>>.New(capacity: count);

            foreach(var c in connections.Children) {
                var props = c.Properties;
                var conn = new Connection(props[0].AsString().ToConnectionType(), props[1].AsInt64(), props[2].AsInt64());

                // Create source-to-dest dictionary
                {
                    UnsafeRawList<long> dests;
                    if(_srcToDest.TryAdd(conn.SourceID, _destLists.Count)) {
                        dests = UnsafeRawList<long>.New();
                        _destLists.Add(dests);
                    }
                    else {
                        dests = _destLists[_srcToDest[conn.SourceID]];
                    }
                    Debug.Assert(dests.IsNull == false);
                    dests.Add(conn.DestID);
                }

                // Create dest-to-source dictionary
                {
                    UnsafeRawList<long> sources;
                    if(_destToSrc.TryAdd(conn.DestID, _srcLists.Count)) {
                        sources = UnsafeRawList<long>.New();
                        _srcLists.Add(sources);
                    }
                    else {
                        sources = _srcLists[_destToSrc[conn.DestID]];
                    }
                    Debug.Assert(sources.IsNull == false);
                    sources.Add(conn.SourceID);
                }

            }
        }

        public ReadOnlySpan<long> GetDests(long sourceID)
        {
            if(_srcToDest != null && _srcToDest.TryGetValue(sourceID, out var index)) {
                return _destLists[index].AsSpan();
            }
            else {
                return ReadOnlySpan<long>.Empty;
            }
        }

        public ReadOnlySpan<long> GetSources(long destID)
        {
            if(_destToSrc != null && _destToSrc.TryGetValue(destID, out var index)) {
                return _srcLists[index].AsSpan();
            }
            else {
                return ReadOnlySpan<long>.Empty;
            }
        }

        public void Dispose()
        {
            _srcToDest?.Dispose();
            foreach(var l in _destLists.AsSpan()) {
                l.Dispose();
            }
            _destLists.Dispose();

            _destToSrc?.Dispose();
            foreach(var l in _srcLists.AsSpan()) {
                l.Dispose();
            }
            _srcLists.Dispose();
        }
    }
}
