#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using Elffy.Effective;

namespace Elffy.Shading;

public readonly ref struct ShaderSource
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool OnlyContainsConstLiteralUtf8 { get; init; }

    public required ReadOnlySpan<byte> VertexShader { get; init; }
    public required ReadOnlySpan<byte> FragmentShader { get; init; }
    public ReadOnlySpan<byte> GeometryShader { get; init; }

    internal bool TryFixed(out FixedShaderSourceCache fixedShaderSource)
    {
        if(OnlyContainsConstLiteralUtf8) {
            fixedShaderSource = new FixedShaderSourceCache(this);
            return true;
        }
        fixedShaderSource = FixedShaderSourceCache.Empty;
        return false;
    }
}

internal unsafe readonly struct FixedShaderSourceCache : IEquatable<FixedShaderSourceCache>
{
    private readonly FixedReadOnlyUtf8String _vertexShader;
    private readonly FixedReadOnlyUtf8String _fragmentShader;
    private readonly FixedReadOnlyUtf8String _geometryShader;
    private readonly int _hashCache;

    public ReadOnlySpan<byte> VertexShader => _vertexShader.AsSpan();
    public ReadOnlySpan<byte> FragmentShader => _fragmentShader.AsSpan();
    public ReadOnlySpan<byte> GeometryShader => _geometryShader.AsSpan();

    public bool IsEmpty => VertexShader.IsEmpty && FragmentShader.IsEmpty && GeometryShader.IsEmpty;

    public static FixedShaderSourceCache Empty => default;

    public FixedShaderSourceCache(in ShaderSource s)
    {
        Debug.Assert(s.OnlyContainsConstLiteralUtf8);

        _vertexShader = FixedReadOnlyUtf8String.FromFixedReadOnlySpanDangerous(s.VertexShader);
        _fragmentShader = FixedReadOnlyUtf8String.FromFixedReadOnlySpanDangerous(s.FragmentShader);
        _geometryShader = FixedReadOnlyUtf8String.FromFixedReadOnlySpanDangerous(s.GeometryShader);

        _hashCache = HashCode.Combine(_vertexShader, _fragmentShader, _geometryShader);
    }

    public override bool Equals(object? obj) => obj is FixedShaderSourceCache cache && Equals(cache);

    public bool Equals(FixedShaderSourceCache other) =>
        _vertexShader == other._vertexShader &&
        _fragmentShader == other._fragmentShader &&
        _geometryShader == other._geometryShader;

    public override int GetHashCode() => _hashCache;

    public static bool operator ==(FixedShaderSourceCache left, FixedShaderSourceCache right) => left.Equals(right);

    public static bool operator !=(FixedShaderSourceCache left, FixedShaderSourceCache right) => !(left == right);

    private unsafe readonly struct FixedReadOnlyUtf8String : IEquatable<FixedReadOnlyUtf8String>
    {
        private readonly byte* _p;
        private readonly int _len;

        public int Length => _len;

        public static FixedReadOnlyUtf8String Empty => default;

        private FixedReadOnlyUtf8String(byte* p, int length)
        {
            if(length < 0) { throw new ArgumentOutOfRangeException(nameof(length)); }
            _p = p;
            _len = length;
        }

        public static FixedReadOnlyUtf8String FromFixedReadOnlySpanDangerous(ReadOnlySpan<byte> fixedSpan)
        {
            return new FixedReadOnlyUtf8String(fixedSpan.AsPointer(), fixedSpan.Length);
        }

        public ReadOnlySpan<byte> AsSpan() => new ReadOnlySpan<byte>(_p, _len);

        public override bool Equals(object? obj) => obj is FixedReadOnlyUtf8String @string && Equals(@string);

        public bool Equals(FixedReadOnlyUtf8String other) => AsSpan().SequenceEqual(other.AsSpan());

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.AddBytes(AsSpan());
            return hash.ToHashCode();
        }

        public static bool operator ==(FixedReadOnlyUtf8String left, FixedReadOnlyUtf8String right) => left.Equals(right);

        public static bool operator !=(FixedReadOnlyUtf8String left, FixedReadOnlyUtf8String right) => !(left == right);
    }
}
