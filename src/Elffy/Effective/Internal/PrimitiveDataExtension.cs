#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.Effective.Internal
{
    internal static class PrimitiveDataExtension
    {
        internal static ref Vector2 AsVector2(this in MMDTools.Vector2 source) => ref Unsafe.As<MMDTools.Vector2, Vector2>(ref Unsafe.AsRef(source));
        internal static ref Vector3 AsVector3(this in MMDTools.Vector3 source) => ref Unsafe.As<MMDTools.Vector3, Vector3>(ref Unsafe.AsRef(source));
    }
}
