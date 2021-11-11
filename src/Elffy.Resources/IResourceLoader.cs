#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Elffy
{
    public interface IResourceLoader
    {
        ResourceFile this[string name] => new ResourceFile(this, name);
        Stream GetStream(string name);
        long GetSize(string name);
        bool HasResource(string name);

        //bool TryGetStream(string name, [MaybeNullWhen(false)] out Stream stream);
        //bool TryGetSize(string name, out long size);
        //bool Exists(string name);
    }
}
