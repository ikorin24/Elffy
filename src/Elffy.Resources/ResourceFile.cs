#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elffy
{
    [DebuggerDisplay("{DebugView,nq}")]
    public readonly struct ResourceFile : IEquatable<ResourceFile>
    {
        private readonly IResourceLoader? _loader;
        private readonly string? _name;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => _loader is null ? "<empty>" : $"{nameof(ResourceFile)}: \"{Name}\" (in {_loader.Name})";

        public string Name => _name ?? "";

        public bool IsNone => _loader is null;

        public ReadOnlySpan<char> FileExtension => ResourcePath.GetExtension(_name.AsSpan());

        public static ResourceFile None => default;

        public long FileSize => ResourceLoader.TryGetSize(_name, out var size) ? size : 0;

        public IResourceLoader ResourceLoader => _loader ?? EmptyResourceLoader.Null;

        internal ResourceFile(IResourceLoader loader, string name)
        {
            _loader = loader;
            _name = name;
        }

        [Obsolete($"{nameof(ResourceFile)} does not support default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ResourceFile()
        {
            throw new NotSupportedException($"{nameof(ResourceFile)} does not support default constructor.");
        }

        public static void ThrowArgumentExceptionIfInvalid(ResourceFile resource, [CallerArgumentExpression("resource")] string? paramName = null)
        {
            if(resource._loader is null) {
                Throw(paramName);
            }
            [DoesNotReturn] static void Throw(string? paramName) => throw new ArgumentException(paramName);
        }

        public ResourceFileHandle GetHandle() => ResourceLoader.TryGetHandle(_name, out var handle) ? handle : ResourceFileHandle.None;

        public Stream GetStream() => ResourceLoader.TryGetStream(_name, out var stream) ? stream : Stream.Null;

        public override bool Equals(object? obj) => obj is ResourceFile file && Equals(file);

        public bool Equals(ResourceFile other) => _loader == other._loader && _name == other._name;

        public override int GetHashCode() => HashCode.Combine(_loader, _name);

        public static bool operator ==(ResourceFile left, ResourceFile right) => left.Equals(right);

        public static bool operator !=(ResourceFile left, ResourceFile right) => !(left == right);
    }
}
