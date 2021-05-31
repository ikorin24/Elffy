#nullable enable
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal readonly struct Connection : IEquatable<Connection>
    {
        public readonly ConnectionType ConnectionType;
        public readonly long SourceID;
        public readonly long DestID;

        public Connection(ConnectionType type, long source, long dest)
        {
            ConnectionType = type;
            SourceID = source;
            DestID = dest;
        }

        public override bool Equals(object? obj) => obj is Connection connection && Equals(connection);

        public bool Equals(Connection other) => ConnectionType == other.ConnectionType && SourceID == other.SourceID && DestID == other.DestID;

        public override int GetHashCode() => HashCode.Combine(ConnectionType, SourceID, DestID);

        public override string ToString() => DebugDisplay();

        private string DebugDisplay() => $"({ConnectionType}) {SourceID} -- {DestID}";
    }
}
