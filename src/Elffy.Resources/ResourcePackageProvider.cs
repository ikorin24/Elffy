#nullable enable

namespace Elffy
{
    public static class ResourcePackageProvider
    {
        public static ResourcePackage CreateLocalResourcePackage(string resourcePackageFilePath)
        {
            return new ResourcePackage(new LocalResourceLoader(resourcePackageFilePath));
        }
    }
}
