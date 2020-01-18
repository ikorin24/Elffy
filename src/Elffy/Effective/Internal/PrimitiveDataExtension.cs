#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.Effective.Internal
{
    internal static class PrimitiveDataExtension
    {
        internal static Vector2 AsVector2(this MMDTools.Vector2 source) => Unsafe.As<MMDTools.Vector2, Vector2>(ref source);
        internal static Vector3 AsVector3(this MMDTools.Vector3 source) => Unsafe.As<MMDTools.Vector3, Vector3>(ref source);
    }
}
