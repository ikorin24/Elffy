#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public sealed class ResourcePackage
    {
        private readonly IResourceLoader _loader;

        public IResourceLoader ResourceLoader => _loader;

        internal ResourcePackage(IResourceLoader loader)
        {
            ArgumentNullException.ThrowIfNull(loader);
            _loader = loader;
        }

        public ResourceFile this[string name] => GetFile(name);

        public bool TryGetFile(string name, out ResourceFile file)
        {
            if(_loader.Exists(name) == false) {
                file = ResourceFile.None;
                return false;
            }
            file = new ResourceFile(_loader, name);
            return true;
        }

        public ResourceFile GetFile(string name)
        {
            if(TryGetFile(name, out var file) == false) {
                ThrowNotFound(name);
            }
            return file;
        }

        [DoesNotReturn]
        private static void ThrowNotFound(string name) => throw new ArgumentException($"Resource file \"{name}\" is not found.");
    }
}
