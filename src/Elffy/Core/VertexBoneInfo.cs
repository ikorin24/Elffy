#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    public readonly struct VertexBoneInfo : IEquatable<VertexBoneInfo>
    {
        public readonly int RefBone0;
        public readonly int RefBone1;
        public readonly int RefBone2;
        public readonly int RefBone3;
        public readonly float Weight0;
        public readonly float Weight1;
        public readonly float Weight2;
        public readonly float Weight3;

        public override bool Equals(object? obj)
        {
            return obj is VertexBoneInfo info && Equals(info);
        }

        public bool Equals(VertexBoneInfo other)
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
}
