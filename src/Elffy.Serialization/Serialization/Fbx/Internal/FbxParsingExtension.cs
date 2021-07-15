#nullable enable
using FbxTools;

namespace Elffy.Serialization.Fbx.Internal
{
    internal static class FbxParsingExtension
    {
        public static bool IsMeshGeometry(this FbxNode node)
        {
            var props = node.Properties;
            var isMeshGeometry = (props.Length > 2) && props[2].TryAsString(out var type) && type.SequenceEqual(FbxConstStrings.Mesh());
            return isMeshGeometry;
        }
    }
}
