#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Fbx.Semantic;
using FbxTools;
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly ref struct FbxConnectionResolver
    {
        private readonly BufferPooledDictionary<long, int>? _srcToDest;
        private readonly UnsafeRawList<UnsafeRawList<long>> _destLists;

        private readonly BufferPooledDictionary<long, int>? _destToSrc;
        private readonly UnsafeRawList<UnsafeRawList<long>> _srcLists;

        public FbxConnectionResolver(FbxNode connectionsNode)
        {
            var count = connectionsNode.Children.Count;
            var destLists = new UnsafeRawList<UnsafeRawList<long>>(count);
            var srcToDest = new BufferPooledDictionary<long, int>(count);
            var destToSrc = new BufferPooledDictionary<long, int>(count);
            var srcLists = new UnsafeRawList<UnsafeRawList<long>>(count);

            try {
                foreach(var c in connectionsNode.Children) {
                    var props = c.Properties;
                    var conn = new Connection(props[0].AsString().ToConnectionType(), props[1].AsInt64(), props[2].AsInt64());

                    // Create source-to-dest dictionary
                    {
                        UnsafeRawList<long> dests;
                        if(srcToDest.TryAdd(conn.SourceID, destLists.Count)) {
                            dests = new UnsafeRawList<long>();
                            destLists.Add(dests);
                        }
                        else {
                            dests = destLists[srcToDest[conn.SourceID]];
                        }
                        Debug.Assert(dests.IsNull == false);
                        dests.Add(conn.DestID);
                    }

                    // Create dest-to-source dictionary
                    {
                        UnsafeRawList<long> sources;
                        if(destToSrc.TryAdd(conn.DestID, srcLists.Count)) {
                            sources = new UnsafeRawList<long>();
                            srcLists.Add(sources);
                        }
                        else {
                            sources = srcLists[destToSrc[conn.DestID]];
                        }
                        Debug.Assert(sources.IsNull == false);
                        sources.Add(conn.SourceID);
                    }
                }
            }
            catch {
                destLists.Dispose();
                srcToDest.Dispose();
                destToSrc.Dispose();
                srcLists.Dispose();
                throw;
            }

            _destLists = destLists;
            _srcToDest = srcToDest;
            _destToSrc = destToSrc;
            _srcLists = srcLists;
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
