#nullable enable
using Elffy.Shapes;
using System.Threading;
using Elffy.Serialization.Wavefront;
using Elffy.Serialization.Fbx;
using Elffy.Serialization.Gltf;

namespace Elffy
{
    public static class ModelResourceLoaderExtension
    {
        public static Model3D CreateFbxModel(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return FbxModelBuilder.CreateLazyLoadingFbx(file, cancellationToken);
        }

        public static Model3D CreateObjModel(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return ObjModelBuilder.CreateLazyLoadingObj(file, cancellationToken);
        }

        public static Model3D CreateGlbModel(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return GlbModelBuilder.CreateLazyLoadingGlb(file, cancellationToken);
        }
    }
}
