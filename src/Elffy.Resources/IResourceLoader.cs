#nullable enable
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace Elffy
{
    public interface IResourceLoader
    {
        public string Name { get; }
        bool TryGetHandle(string? name, out ResourceFileHandle handle);
        bool TryGetStream(string? name, out Stream stream);
        bool TryGetSize(string? name, out long size);
        bool Exists(string? name);
    }
}
