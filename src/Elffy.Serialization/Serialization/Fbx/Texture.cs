#nullable enable
using FbxTools;
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal readonly struct Texture : IEquatable<Texture>
    {
        public readonly long ID;
        public readonly RawString FileName;

        public Texture(long id, RawString fileName)
        {
            ID = id;
            FileName = fileName;
        }

        public override bool Equals(object? obj)
        {
            return obj is Texture texture && Equals(texture);
        }

        public bool Equals(Texture other)
        {
            return ID == other.ID &&
                   FileName.Equals(other.FileName);
        }

        public override int GetHashCode() => HashCode.Combine(ID, FileName);

        public override string ToString() => DebugDisplay();

        private string DebugDisplay() => $"(id:{ID}) \"{FileName.ToString()}\"";
    }
}
