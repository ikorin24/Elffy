#nullable enable

namespace Elffy
{
    public static class ResourceProvider
    {
        public static IResourceLoader LocalResource(string resourcePackageFilePath)
        {
            return new LocalResourceLoader(resourcePackageFilePath);
        }
    }
}
