#nullable enable
using Elffy.Shading;
using System;
using System.Linq;

namespace Elffy.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ShaderTargetVertexTypeAttribute : Attribute
    {
        public Type VertexType { get; }

        public ShaderTargetVertexTypeAttribute(Type vertexLikeType)
        {
            VertexType = vertexLikeType ?? throw new ArgumentNullException(nameof(vertexLikeType));
        }

        internal static void CheckVertexType(Type shaderSourceType, Type vertexType)
        {
            if(shaderSourceType is null) { throw new ArgumentNullException(nameof(shaderSourceType)); }
            if(vertexType is null) { throw new ArgumentNullException(nameof(vertexType)); }
            if(shaderSourceType.IsAssignableTo(typeof(IShaderSource)) == false) {
                throw new ArgumentException($"{nameof(shaderSourceType)} does not inherit {typeof(IShaderSource)}.");
            }

            var isNonChecking = GetCustomAttribute(shaderSourceType, typeof(NonCheckingShaderTargetVertexTypeAttribute)) is not null;
            if(isNonChecking) {
                return;
            }

            var attrs = GetCustomAttributes(shaderSourceType, typeof(ShaderTargetVertexTypeAttribute))
                .OfType<ShaderTargetVertexTypeAttribute>()
                .Where(a => a.VertexType == vertexType);
            foreach(var attr in attrs) {
                if(attr is ShaderTargetVertexTypeAttribute shaderAttr && shaderAttr.VertexType == vertexType) {
                    return;
                }
            }

            var availableTypes = string.Join(", ", attrs.Select(a => a.VertexType.FullName));
            throw new ArgumentException($"Vertex type mismatch. Specified vertex type: {vertexType.FullName}. Available vertex type(s): {availableTypes}");
        }
    }
}
