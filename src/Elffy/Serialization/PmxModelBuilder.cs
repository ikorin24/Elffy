#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Elffy.Shape;
using Elffy.Effective;
using Elffy.Effective.Internal;
using Elffy.Components;
using System.Linq;
using MMDTools;

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
            var boneList = pmx.BoneList.ExtractInnerArray();

            //var root = new Model3D(ReadOnlySpan<Core.Vertex>.Empty, ReadOnlySpan<int>.Empty);
            //using(var vertexArray = GetVertexArray(allVertex)) {
            //    foreach(var indexArray in GetPartsIndexArray(materials, allIndex)) {
            //        using(indexArray) {
            //            root.Children.Add(new Model3D(vertexArray.AsSpan(), indexArray.AsSpan()));
            //        }
            //    }
            //}

            using(var vertexArray = allVertex.SelectToUnmanagedArray(v => new Core.Vertex(v.Position.AsVector3(), v.Normal.AsVector3(), v.UV.AsVector2()))) {
                var model = new Model3D(vertexArray.AsSpan(), allIndex);

                using(var infoArray = new UnmanagedArray<Core.VertexBoneInfo>(allVertex.Length)) {
                    var bone = new Components.Bone(GetBoneTree(boneList), infoArray.AsSpan());
                    model.AddComponent(bone);
                }

                return model;
            }
        }

        private static BoneTreeElement GetBoneTree(ReadOnlySpan<MMDTools.Bone> boneList)
        {
            var s = boneList.FirstOrDefault(b => b.ParentBone == 65535);
            return new BoneTreeElement(0);
        }

        //private static ReadOnlySpan<UnmanagedArray<int>> GetPartsIndexArray(ReadOnlySpan<MMDTools.Material> materials, ReadOnlySpan<int> allIndex)
        //{
        //    // パーツごとに頂点インデックス配列を生成
        //    var partsIndexArray = new UnmanagedArray<int>[materials.Length];
        //    try {
        //        var pos = 0;
        //        for(int i = 0; i < partsIndexArray.Length; i++) {
        //            partsIndexArray[i] = allIndex.Slice(pos, materials[i].VertexCount).ToUnmanagedArray();
        //            pos += materials[i].VertexCount;
        //        }
        //    }
        //    catch(Exception) {
        //        foreach(var umArray in partsIndexArray) {
        //            umArray?.Dispose();
        //        }
        //        throw;
        //    }
        //    return partsIndexArray;
        //}

        //private static UnmanagedArray<Elffy.Core.Vertex> GetVertexArray(ReadOnlySpan<MMDTools.Vertex> vertexArray)
        //{
        //    var buf = new UnmanagedArray<Elffy.Core.Vertex>(vertexArray.Length);
        //    try {
        //        for(int i = 0; i < vertexArray.Length; i++) {
        //            buf[i] = new Elffy.Core.Vertex(vertexArray[i].Position.AsVector3(), vertexArray[i].Normal.AsVector3(), vertexArray[i].UV.AsVector2());
        //        }
        //        return buf;
        //    }
        //    catch(Exception) {
        //        buf.Dispose();
        //        throw;
        //    }
        //}
    }
}

