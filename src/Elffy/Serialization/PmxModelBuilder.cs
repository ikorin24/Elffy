#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Elffy.Shape;
using Elffy.Effective;
using Elffy.Effective.Internal;
using Elffy.Components;
using Elffy.Core;
using System.Linq;
using MMDTools;

namespace Elffy.Serialization
{
    internal static class PmxModelBuilder
    {
        public static unsafe Model3D LoadModel(Stream stream)
        {
            var pmx = PMXParser.Parse(stream);
            var allIndex = pmx.SurfaceList.Span.MarshalCast<Surface, int>();
            var allVertex = pmx.VertexList.Span;
            var materials = pmx.MaterialList.Span;
            var boneList = pmx.BoneList.Span;

            fixed(int* p = allIndex) {
                Model3DTool.ReverseTrianglePolygon(new Span<int>(p, allIndex.Length));
            }

            using(var vertexArray = allVertex.SelectToUnmanagedArray(GetVertex))
            using(var weightArray = allVertex.SelectToUnmanagedArray(GetBoneWeight))
            using(var boneTreeElements = boneList.SelectToUnmanagedArray(GetBoneTreeElement)) {
                var model = new Model3D(vertexArray.AsSpan(), allIndex);
                var bone = new Components.Bone(boneTreeElements.AsSpan(), weightArray.AsSpan());
                model.AddComponent(bone);
                return model;
            }
        }

        private static Core.WeightTransformType ToWeightType(MMDTools.WeightTransformType t)
        {
            return t switch
            {
                MMDTools.WeightTransformType.BDEF1 => Core.WeightTransformType.BDEF1,
                MMDTools.WeightTransformType.BDEF2 => Core.WeightTransformType.BDEF2,
                MMDTools.WeightTransformType.BDEF4 => Core.WeightTransformType.BDEF4,
                _ => throw new NotSupportedException($"Not supported weight type. Type : {t}"),
            };
        }

        private static Core.Vertex GetVertex(MMDTools.Vertex v) 
            => new Core.Vertex(v.Position.ToVector3(), v.Normal.ToVector3(), v.UV.ToVector2());

        private static BoneTreeElement GetBoneTreeElement(MMDTools.Bone b, int i)
            => new BoneTreeElement(i, (b.ParentBone < 0) ? b.ParentBone : (int?)null, b.Position.ToVector3());

        static BoneWeight GetBoneWeight(MMDTools.Vertex v)
            => new BoneWeight(v.BoneIndex1, v.BoneIndex2, v.BoneIndex3, v.BoneIndex4,
                              v.Weight1, v.Weight2, v.Weight3, v.Weight4, ToWeightType(v.WeightTransformType));
    }
}

