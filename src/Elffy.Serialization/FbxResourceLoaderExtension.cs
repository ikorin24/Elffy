#nullable enable
using Elffy.Shapes;
using Elffy.Serialization;
using System.Threading;

namespace Elffy
{
    public static class FbxResourceLoaderExtension
    {
        public static Model3D CreateFbxModel(this IResourceLoader source, string name, CancellationToken cancellationToken = default)
        {
            return FbxModelBuilder.CreateLazyLoadingFbx(source, name, cancellationToken);
        }
    }
}
