#nullable enable
using System;
using System.IO;

namespace Elffy
{
    public readonly struct ResourceFile : IEquatable<ResourceFile>
    {
        private readonly IResourceLoader? _loader;
        private readonly string? _name;

        public string Name => _name ?? "";

        public ReadOnlySpan<char> FileExtension => ResourcePath.GetExtension(_name.AsSpan());

        public long FileSize => ResourceLoader.GetSize(Name);

        public IResourceLoader ResourceLoader => _loader ?? EmptyResourceLoader.Null;

        public ResourceFile(IResourceLoader loader, string name)
        {
            _loader = loader;
            _name = name;
        }

        public void ThrowIfNotFound() => ResourceNotFoundException.ThrowIfNotFound(this);

        public bool Exists() => ResourceLoader.HasResource(Name);

        public Stream GetStream() => ResourceLoader.GetStream(Name);

        public override bool Equals(object? obj) => obj is ResourceFile file && Equals(file);

        public bool Equals(ResourceFile other) => _loader == other._loader && _name == other._name;

        public override int GetHashCode() => HashCode.Combine(_loader, _name);

        public static bool operator ==(ResourceFile left, ResourceFile right) => left.Equals(right);

        public static bool operator !=(ResourceFile left, ResourceFile right) => !(left == right);
    }

    //public sealed class ResourcePackage
    //{
    //    private readonly IResourceLoader _resourceLoader;

    //    private static readonly ResourcePackage _package = new ResourcePackage(EmptyResourceLoader.Null);
    //    internal static ResourcePackage EmptyPackage => _package;

    //    private ResourcePackage(IResourceLoader loader)
    //    {
    //        _resourceLoader = loader;
    //    }

    //    public static ResourcePackage CreateLocalResourcePackage(string resourcePackagePath)
    //    {
    //        if(resourcePackagePath is null) { throw new ArgumentNullException(nameof(resourcePackagePath)); }
    //        var fullPath = Path.Combine(AppContext.BaseDirectory, resourcePackagePath);
    //        var resourceLoader = new LocalResourceLoader(fullPath);
    //        return new ResourcePackage(resourceLoader);
    //    }

    //    public ResourceFile GetFile(string name)
    //    {
    //        return new ResourceFile(_resourceLoader, name);
    //    }
    //}

    //public readonly struct ResourceDirectory : IEquatable<ResourceFile>
    //{
    //    private readonly IResourceLoader? _loader;
    //    private readonly string? _name;
    //}
}
