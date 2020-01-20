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
        public static Model3D LoadModel(Stream stream)
        {
            var pmx = PMXParser.Parse(stream);
            var allIndex = pmx.SurfaceList.ExtractInnerArray().MarshalCast<Surface, int>();
            var allVertex = pmx.VertexList.ExtractInnerArray();
            var materials = pmx.MaterialList.ExtractInnerArray();
            var boneList = pmx.BoneList.ExtractInnerArray();

            Core.WeightTransformType ToWeightType(MMDTools.WeightTransformType t)
                => t switch
                {
                    MMDTools.WeightTransformType.BDEF1 => Core.WeightTransformType.BDEF1,
                    MMDTools.WeightTransformType.BDEF2 => Core.WeightTransformType.BDEF2,
                    MMDTools.WeightTransformType.BDEF4 => Core.WeightTransformType.BDEF4,
                    _ => throw new NotSupportedException($"Not supported weight type. Type : {t}"),
                };

            Core.Vertex GetVertex(MMDTools.Vertex v)
                => new Core.Vertex(v.Position.AsVector3(), v.Normal.AsVector3(), v.UV.AsVector2());

            BoneTreeElement GetBoneTreeElement(MMDTools.Bone b, int i)
                => new BoneTreeElement(i, (b.ParentBone != 65535) ? b.ParentBone : (int?)null, b.Position.AsVector3());

            Core.BoneWeight GetBoneWeight(MMDTools.Vertex v)
                => new Core.BoneWeight(v.BoneIndex1, v.BoneIndex2, v.BoneIndex3, v.BoneIndex4,
                                       v.Weight1, v.Weight2, v.Weight3, v.Weight4, ToWeightType(v.WeightTransformType));

            using(var vertexArray = allVertex.SelectToUnmanagedArray(GetVertex))
            using(var weightArray = allVertex.SelectToUnmanagedArray(GetBoneWeight))
            using(var boneTreeElements = boneList.SelectToUnmanagedArray(GetBoneTreeElement)) {
                var model = new Model3D(vertexArray.AsSpan(), allIndex);
                var bone = new Components.Bone(boneTreeElements.AsSpan(), weightArray.AsSpan());
                model.AddComponent(bone);
                return model;
            }
        }
    }
}

