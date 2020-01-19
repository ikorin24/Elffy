#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Elffy.Shape;
using Elffy.Effective;
using Elffy.Effective.Internal;
using System.Linq;
using MMDTools;
using ElffyVertex = Elffy.Core.Vertex;

using TKVector2 = OpenTK.Vector2;
using TKVector3 = OpenTK.Vector3;
using TKVector4 = OpenTK.Vector4;

namespace Elffy.Serialization
{
    internal static class PmxModelBuilder
    {
        public static Model3D LoadModel(Stream stream)
        {
            var pmx = PMXParser.Parse(stream);
            var allIndex = pmx.SurfaceList.ExtractInnerArray().MarshalCast<Surface, int>();
            var allVertex = pmx.VertexList.ExtractInnerArray();
            var materials = pmx.MaterialList.ExtractInnerArray();

            var root = new Model3D(ReadOnlySpan<ElffyVertex>.Empty, ReadOnlySpan<int>.Empty);
            using(var vertexArray = GetVertexArray(allVertex)) {
                foreach(var indexArray in GetPartsIndexArray(materials, allIndex)) {
                    using(indexArray) {
                        root.Children.Add(new Model3D(vertexArray, indexArray));
                    }
                }
            }
            return root;
        }

        private static ReadOnlySpan<UnmanagedArray<int>> GetPartsIndexArray(ReadOnlySpan<MMDTools.Material> materials, ReadOnlySpan<int> allIndex)
        {
            // パーツごとに頂点インデックス配列を生成
            var partsIndexArray = new UnmanagedArray<int>[materials.Length];
            try {
                var pos = 0;
                for(int i = 0; i < partsIndexArray.Length; i++) {
                    partsIndexArray[i] = allIndex.Slice(pos, materials[i].VertexCount).ToUnmanagedArray();
                    pos += materials[i].VertexCount;
                }
            }
            catch(Exception) {
                foreach(var umArray in partsIndexArray) {
                    umArray?.Dispose();
                }
                throw;
            }
            return partsIndexArray;
        }

        private static UnmanagedArray<ElffyVertex> GetVertexArray(ReadOnlySpan<Vertex> vertexArray)
        {
            var buf = new UnmanagedArray<ElffyVertex>(vertexArray.Length);
            try {
                for(int i = 0; i < vertexArray.Length; i++) {
                    //buf[i] = new ElffyVertex(GetVector(vertexArray[i].Position), GetVector(vertexArray[i].Normal), GetVector(vertexArray[i].UV));
                    buf[i] = new ElffyVertex(vertexArray[i].Position.AsVector3(), vertexArray[i].Normal.AsVector3(), vertexArray[i].UV.AsVector2());
                }
                return buf;
            }
            catch(Exception) {
                buf.Dispose();
                throw;
            }
        }
    }
}

