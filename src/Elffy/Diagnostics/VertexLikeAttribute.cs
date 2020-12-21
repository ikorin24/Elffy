#nullable enable
using System;

namespace Elffy.Diagnostics
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class VertexLikeAttribute : Attribute
    {
        public static bool IsVertexLike(Type type)
        {
            if(type is null) { throw new ArgumentNullException(nameof(type)); }
            return Attribute.GetCustomAttribute(type, typeof(VertexLikeAttribute)) is null == false;
        }
    }
}
