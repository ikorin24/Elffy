#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Elffy
{
    public interface IResourcePackage
    {
        string Name { get; }
        bool TryGetHandle(string? name, out ResourceFileHandle handle);
        bool TryGetStream(string? name, out Stream stream);
        bool TryGetSize(string? name, out long size);
        bool Exists(string? name);

        public ResourceFile this[string name] => GetFile(name);

        public bool TryGetFile(string name, out ResourceFile file)
        {
            if(Exists(name) == false) {
                file = ResourceFile.None;
                return false;
            }
            file = new ResourceFile(this, name);
            return true;
        }

        public ResourceFile GetFile(string name)
        {
            if(TryGetFile(name, out var file) == false) {
                ThrowNotFound(name);

                [DoesNotReturn] static void ThrowNotFound(string name) => throw new ArgumentException($"Resource file \"{name}\" is not found.");
            }
            return file;
        }
    }
}
