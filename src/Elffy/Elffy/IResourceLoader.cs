#nullable enable
using System.IO;

namespace Elffy
{
    public interface IResourceLoader
    {
        ResourceFile this[string name] => new ResourceFile(this, name);
        Stream GetStream(string name);
        long GetSize(string name);
        bool HasResource(string name);
    }
}
