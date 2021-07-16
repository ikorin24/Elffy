#nullable enable
using System;
using Elffy.Serialization.Fbx.Semantic;

namespace Elffy.Serialization.Fbx
{
    public readonly struct BoneData : IEquatable<BoneData>
    {
        public readonly int? ParentIndex;
        internal readonly long LimbNodeID;

        internal BoneData(int? parentIndex, long limbNodeID)
        {
            ParentIndex = parentIndex;
            LimbNodeID = limbNodeID;
        }

        public override bool Equals(object? obj) => obj is BoneData data && Equals(data);

        public bool Equals(BoneData other) => ParentIndex == other.ParentIndex && LimbNodeID == other.LimbNodeID;

        public override int GetHashCode() => HashCode.Combine(ParentIndex, LimbNodeID);
    }
}
