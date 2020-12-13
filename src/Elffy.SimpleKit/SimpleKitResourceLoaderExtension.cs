#nullable enable
using Elffy.Shapes;
using Elffy.Serialization;
using System.Threading;

namespace Elffy
{
    public static class SimpleKitResourceLoaderExtension
    {
        public static Model3D CreateFbxModel(this IResourceLoader source, string name, CancellationToken cancellationToken = default)
        {
            return FbxModelBuilder.CreateLazyLoadingFbx(source, name, cancellationToken);
        }

        public static Model3D CreatePmxModel(this IResourceLoader source, string name, CancellationToken cancellationToken = default)
        {
            return PmxModelBuilder.CreateLazyLoadingPmx(source, name, cancellationToken);
        }
    }
}
