#nullable enable
using System.IO;

namespace Elffy
{
    public interface IResourceLoader
    {
        bool TryGetStream(string? name, out Stream stream);
        bool TryGetSize(string? name, out long size);
        bool Exists(string? name);
    }
}
