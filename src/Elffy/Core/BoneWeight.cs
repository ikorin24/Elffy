#nullable enable
using System;
using System.Diagnostics;

namespace Elffy.Core
{
    [Obsolete("たぶんいらない", true)]
    [DebuggerDisplay("{Type}, [{RefBone0}] {Weight0.ToString(\"F9\")}, [{RefBone1}], {Weight1.ToString(\"F9\")}, [{RefBone2}],{Weight2.ToString(\"F9\")}, [{RefBone0}],{Weight3.ToString(\"F9\")}")]
    public readonly struct BoneWeight : IEquatable<BoneWeight>
    {
        public readonly int RefBone0;
        public readonly int RefBone1;
        public readonly int RefBone2;
        public readonly int RefBone3;
        public readonly float Weight0;
        public readonly float Weight1;
        public readonly float Weight2;
        public readonly float Weight3;
        public readonly WeightTransformType Type;

        public BoneWeight(int refBone0, int refBone1, int refBone2, int refBone3, float weight0, float weight1, float weight2, float weight3, WeightTransformType type)
        {
            RefBone0 = refBone0;
            RefBone1 = refBone1;
            RefBone2 = refBone2;
            RefBone3 = refBone3;
            Weight0 = weight0;
            Weight1 = weight1;
            Weight2 = weight2;
            Weight3 = weight3;
            Type = type;
        }

        public override bool Equals(object? obj)
        {
            return obj is BoneWeight info && Equals(info);
        }

        public bool Equals(BoneWeight other)
        {
            return RefBone0 == other.RefBone0 &&
                   RefBone1 == other.RefBone1 &&
                   RefBone2 == other.RefBone2 &&
                   RefBone3 == other.RefBone3 &&
                   Weight0 == other.Weight0 &&
                   Weight1 == other.Weight1 &&
                   Weight2 == other.Weight2 &&
                   Weight3 == other.Weight3;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RefBone0, RefBone1, RefBone2, RefBone3, Weight0, Weight1, Weight2, Weight3);
        }
    }

    public enum WeightTransformType
    {
        BDEF1,
        BDEF2,
        BDEF4,
    }
}
