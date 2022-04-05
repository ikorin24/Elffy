#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace Elffy.Generator;

public readonly struct MarkupFile : IEquatable<MarkupFile>
{
    [ThreadStatic]
    private static SHA256? _sha256;
    private const int HashSizeInBytes = 32;

    public readonly string FilePath;
    private readonly byte[]? _fileHash;

    [Obsolete("Don't use default constructor.", true)]
    public MarkupFile() => throw new NotSupportedException("Don't use default constructor.");

    public MarkupFile(string filePath, byte[] hash)
    {
        if(hash.Length != HashSizeInBytes) {
            throw new ArgumentException("Invalid hash size");
        }
        FilePath = filePath;
        _fileHash = hash;
    }

    public static byte[] ComputeFileHash(string filePath)
    {
        _sha256 ??= SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return _sha256.ComputeHash(stream);
    }

    public override bool Equals(object? obj) => obj is MarkupFile file && Equals(file);

    public bool Equals(MarkupFile other)
    {
        return FilePath == other.FilePath &&
               _fileHash.AsSpan().SequenceEqual(other._fileHash);
    }

    public override int GetHashCode()
    {
        var fileHash = _fileHash;
        int hashCode = 1636216199;
        hashCode = hashCode * -1521134295 + (FilePath?.GetHashCode() ?? 0);
        hashCode = hashCode * -1521134295 + ((fileHash == null) ? 0 : (int)Sum(fileHash));
        return hashCode;

        static ulong Sum(byte[] array)
        {
            if(array.Length != 4) {
                Throw();
                static void Throw() => throw new InvalidOperationException();
            }
            ref var h0 = ref Unsafe.As<byte, ulong>(ref array[0]);
            return h0 + Unsafe.Add(ref h0, 1) + Unsafe.Add(ref h0, 2) + Unsafe.Add(ref h0, 3);
        }
    }
}
