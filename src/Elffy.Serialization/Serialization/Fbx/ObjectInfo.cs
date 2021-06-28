#nullable enable
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal readonly struct ObjectInfo : IEquatable<ObjectInfo>
    {
        public readonly ObjectType Type;
        public readonly int Index;

        public ObjectInfo(ObjectType type, int index)
        {
            Type = type;
            Index = index;
        }

        public override bool Equals(object? obj) => obj is ObjectInfo info && Equals(info);

        public bool Equals(ObjectInfo other) => Type == other.Type && Index == other.Index;

        public override int GetHashCode() => HashCode.Combine(Type, Index);

        public override string ToString() => DebugDisplay();

        private string DebugDisplay() => $"{Type}, Index={Index}";
    }
}
