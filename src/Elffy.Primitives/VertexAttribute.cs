#nullable enable
using System;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class VertexAttribute : Attribute
    {
        public static bool IsVertexType(Type type)
        {
            if(type is null) { throw new ArgumentNullException(nameof(type)); }
            return GetCustomAttribute(type, typeof(VertexAttribute)) is not null;
        }
    }
}
