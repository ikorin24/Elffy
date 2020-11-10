#nullable enable
using System.IO;

namespace Elffy
{
    public interface IResourceLoader
    {
        Stream GetStream(string name);
        long GetSize(string name);
    }
}
