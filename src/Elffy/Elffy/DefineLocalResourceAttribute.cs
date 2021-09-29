#nullable enable
using System;
using Elffy.Core;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class DefineLocalResourceAttribute : Attribute
    {
        public DefineLocalResourceAttribute(string importedName, string packageFilePath)
        {
        }
    }

    public static class ResourceProvider
    {
        public static IResourceLoader LocalResource(string resourcePackageFilePath)
        {
            return new LocalResourceLoader(resourcePackageFilePath);
        }
    }
}
