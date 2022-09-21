#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Elffy
{
    internal readonly struct ContainerType : IEquatable<ContainerType>
    {
        private readonly ulong _value;

        private static ReadOnlySpan<byte> _mesh => "mesh    "u8;
        public static ref readonly ContainerType Mesh => ref Unsafe.As<byte, ContainerType>(ref MemoryMarshal.GetReference(_mesh));

        public override bool Equals(object? obj) => obj is ContainerType type && Equals(type);

        public bool Equals(ContainerType other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(ContainerType left, ContainerType right) => left.Equals(right);

        public static bool operator !=(ContainerType left, ContainerType right) => !(left == right);

        public unsafe ReadOnlySpan<byte> AsByteSpan()
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ContainerType, byte>(ref Unsafe.AsRef(this)), sizeof(ContainerType));
        }

        public unsafe override string ToString() => Encoding.UTF8.GetString(AsByteSpan());
    }
}
