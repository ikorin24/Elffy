#nullable enable
using Elffy.Shapes;
using Elffy.Serialization;
using System.Threading;

namespace Elffy
{
    public static class SimpleKitResourceFileExtension
    {
        public static Model3D CreatePmxModel(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return PmxModelBuilder.CreateLazyLoadingPmx(file, cancellationToken);
        }
    }
}
