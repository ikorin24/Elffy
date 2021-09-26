#nullable enable
using Elffy.Shapes;
using System.Threading;
using Elffy.Serialization.Wavefront;
using Elffy.Serialization.Fbx;

namespace Elffy
{
    public static class ModelResourceLoaderExtension
    {
        public static Model3D CreateFbxModel(this IResourceLoader source, string name, CancellationToken cancellationToken = default)
        {
            return FbxModelBuilder.CreateLazyLoadingFbx(source, name, cancellationToken);
        }

        [System.Obsolete("Not implemented yet", true)]
        public static Model3D CreateObjModel(this IResourceLoader source, string name, CancellationToken cancellationToken = default)
        {
            return ObjModelBuilder.CreateLazyLoadingObj(source, name, cancellationToken);
        }
    }
}
