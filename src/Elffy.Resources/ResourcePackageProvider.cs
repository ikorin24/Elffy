#nullable enable

namespace Elffy
{
    public static class ResourcePackageProvider
    {
        public static IResourcePackage CreateLocalResourcePackage(string packageName, string resourcePackageFilePath)
        {
            return new LocalResourcePackage(packageName, resourcePackageFilePath);
        }
    }
}
