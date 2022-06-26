#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Elffy.Markup;

namespace Elffy
{
    [DebuggerDisplay("{DebugView,nq}")]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(MarkupPattern, MarkupEmit)]
    public readonly struct ResourceFile : IEquatable<ResourceFile>
    {
        // lang=regex
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const string MarkupPattern = @"^(?<p>[^:]+)\:(?<n>.+)$";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const string MarkupEmit = @"global::Elffy.Resources.${p}.GetFile(""${n}"")";

        private readonly IResourcePackage? _package;
        private readonly string? _name;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => _package is null ? "<empty>" : $"{nameof(ResourceFile)}: \"{Name}\" (in {_package.Name})";

        public string Name => _name ?? "";

        public bool IsNone => _package is null;

        public ReadOnlySpan<char> FileExtension => ResourcePath.GetExtension(_name.AsSpan());

        [LiteralMarkupMember]
        public static ResourceFile None => default;

        public long FileSize => Package.TryGetSize(_name, out var size) ? size : 0;

        public IResourcePackage Package => _package ?? EmptyResourcePackage.Instance;

        internal ResourceFile(IResourcePackage package, string name)
        {
            _package = package;
            _name = name;
        }

        [Obsolete($"{nameof(ResourceFile)} does not support default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ResourceFile() => throw new NotSupportedException($"{nameof(ResourceFile)} does not support default constructor.");

        public static void ThrowArgumentExceptionIfNone(ResourceFile resource, [CallerArgumentExpression("resource")] string? paramName = null)
        {
            if(resource.IsNone) {
                Throw(paramName);
            }
            [DoesNotReturn] static void Throw(string? paramName) => throw new ArgumentException(paramName);
        }

        public int ReadBytes(Span<byte> buffer) => ReadBytes(buffer, 0);

        public int ReadBytes(Span<byte> buffer, long fileOffset)
        {
            unsafe {
                fixed(byte* ptr = buffer) {
                    return (int)ReadBytes((IntPtr)ptr, (nuint)buffer.Length, fileOffset);
                }
            }
        }

        public long ReadBytes(IntPtr buffer, nuint bufferLength) => ReadBytes(buffer, bufferLength, 0);

        public long ReadBytes(IntPtr buffer, nuint bufferLength, long fileOffset)
        {
            if(TryGetHandle(out var handle)) {
                try {
                    return handle.Read(buffer, bufferLength, fileOffset);
                }
                finally {
                    handle.Dispose();
                }
            }
            else {
                using var stream = GetStream();
                return ReadStreamAll(stream, fileOffset, buffer, (long)bufferLength);
            }

            static unsafe long ReadStreamAll(Stream stream, long fileOffset, IntPtr buffer, long bufferLength)
            {
                if(fileOffset != 0) {
                    if(stream.CanSeek) {
                        stream.Position = fileOffset;
                    }
                    else {
                        throw new NotSupportedException("Cannot seek the specified stream.");    // TODO: unseekable stream
                    }
                }

                long totalLen = 0;
                while(true) {
                    var dest = (byte*)buffer + totalLen;
                    var destLen = (int)Math.Min(bufferLength - totalLen, int.MaxValue);
                    var readLen = stream.Read(new Span<byte>(dest, destLen));
                    totalLen += readLen;
                    if(totalLen == bufferLength || readLen == 0) {
                        return totalLen;
                    }
                }
            }
        }

        public bool TryGetHandle(out ResourceFileHandle handle) => Package.TryGetHandle(_name, out handle);

        public ResourceFileHandle GetHandle() => Package.TryGetHandle(_name, out var handle) ? handle : ResourceFileHandle.None;

        public Stream GetStream() => Package.TryGetStream(_name, out var stream) ? stream : Stream.Null;

        public override bool Equals(object? obj) => obj is ResourceFile file && Equals(file);

        public bool Equals(ResourceFile other) => _package == other._package && _name == other._name;

        public override int GetHashCode() => HashCode.Combine(_package, _name);

        public static bool operator ==(ResourceFile left, ResourceFile right) => left.Equals(right);

        public static bool operator !=(ResourceFile left, ResourceFile right) => !(left == right);

        private sealed class EmptyResourcePackage : IResourcePackage
        {
            private static readonly EmptyResourcePackage _instance = new EmptyResourcePackage();
            public static EmptyResourcePackage Instance => _instance;

            public string Name => "Empty";

            public bool Exists(string? name) => false;

            public bool TryGetHandle(string? name, out ResourceFileHandle handle)
            {
                handle = ResourceFileHandle.None;
                return false;
            }

            public bool TryGetSize(string? name, out long size)
            {
                size = 0;
                return false;
            }

            public bool TryGetStream(string? name, out Stream stream)
            {
                stream = Stream.Null;
                return false;
            }
        }
    }
}
