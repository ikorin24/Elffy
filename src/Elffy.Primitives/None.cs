#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Elffy;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct None : IEquatable<None>
{
    public static readonly None Default;

    public override int GetHashCode() => 0;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is None none && Equals(none);

    public bool Equals(None other) => true;

    public override string ToString() => "()";
}
