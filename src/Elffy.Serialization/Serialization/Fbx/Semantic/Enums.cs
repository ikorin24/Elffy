#nullable enable
using System;
using FbxTools;
using Elffy.Serialization.Fbx.Internal;

namespace Elffy.Serialization.Fbx.Semantic
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

    internal enum ConnectionType
    {
        /// <summary>object-object connection</summary>
        OO,
        /// <summary>object-property connection</summary>
        OP,
    }

    internal enum ModelType
    {
        Null,
        LimbNode,
        Mesh,

        Unknown,    // There are no 'Unknown' model type in fbx. It represents everything else except what I use in this program.
    }

    internal static class EnumFromRawStringExtension
    {
        public static ModelType ToModelType(this RawString str)
        {
            ReadOnlySpan<byte> Null = stackalloc byte[4] { (byte)'N', (byte)'u', (byte)'l', (byte)'l' };
            ReadOnlySpan<byte> LimbNode = stackalloc byte[8] { (byte)'L', (byte)'i', (byte)'m', (byte)'b', (byte)'N', (byte)'o', (byte)'d', (byte)'e' };
            ReadOnlySpan<byte> Mesh = stackalloc byte[4] { (byte)'M', (byte)'e', (byte)'s', (byte)'h' };

            return str.SequenceEqual(Null) ? ModelType.Null
                 : str.SequenceEqual(LimbNode) ? ModelType.LimbNode
                 : str.SequenceEqual(Mesh) ? ModelType.Mesh
                 : ModelType.Unknown;
        }

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

        public static ConnectionType ToConnectionType(this RawString str)
        {
            ReadOnlySpan<byte> OO = stackalloc byte[2] { (byte)'O', (byte)'O' };
            ReadOnlySpan<byte> OP = stackalloc byte[2] { (byte)'O', (byte)'P' };

            if(str.SequenceEqual(OO)) {
                return ConnectionType.OO;
            }
            else if(str.SequenceEqual(OP)) {
                return ConnectionType.OP;
            }
            else {
                return Throw();
                static ConnectionType Throw() => throw new FormatException();
            }
        }
    }
}
