#nullable enable
using System;
using FbxTools;

namespace Elffy.Serialization.Fbx
{
    internal enum ReferenceInformationType
    {
        Direct,
        IndexToDirect,
    }

    internal enum MappingInformationType
    {
        ByPolygonVertex,
        ByVertice,
        ByControllPoint,
    }

    internal static class EnumFromRawStringExtension
    {
        public static MappingInformationType ToMappingInformationType(this RawString str)
        {
            if(str.SequenceEqual(FbxConstStrings.ByVertice())) {
                return MappingInformationType.ByVertice;
            }
            else if(str.SequenceEqual(FbxConstStrings.ByPolygonVertex())) {
                return MappingInformationType.ByPolygonVertex;
            }
            else if(str.SequenceEqual(FbxConstStrings.ByPolygonVertex())) {
                return MappingInformationType.ByControllPoint;
            }
            else {
                throw new FormatException();
            }
        }

        public static ReferenceInformationType ToReferenceInformationType(this RawString str)
        {
            if(str.SequenceEqual(FbxConstStrings.Direct())) {
                return ReferenceInformationType.Direct;
            }
            else if(str.SequenceEqual(FbxConstStrings.IndexToDirect())) {
                return ReferenceInformationType.IndexToDirect;
            }
            else {
                throw new FormatException();
            }
        }
    }
}
