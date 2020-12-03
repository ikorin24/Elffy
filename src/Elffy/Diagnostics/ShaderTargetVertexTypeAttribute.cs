#nullable enable
using Cysharp.Text;
using Elffy.Shading;
using System;
using System.Linq;

namespace Elffy.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ShaderTargetVertexTypeAttribute : Attribute
    {
        public Type VertexType { get; }

        public ShaderTargetVertexTypeAttribute(Type vertexLikeType)
        {
            VertexType = vertexLikeType ?? throw new ArgumentNullException(nameof(vertexLikeType));
        }

        public static void CheckVertexType(Type shaderSourceType, Type vertexType)
        {
            if(shaderSourceType is null) { throw new ArgumentNullException(nameof(shaderSourceType)); }
            if(vertexType is null) { throw new ArgumentNullException(nameof(vertexType)); }
            if(shaderSourceType.IsSubclassOf(typeof(ShaderSource)) == false) {
                throw new ArgumentException($"{nameof(shaderSourceType)} is not subclass of {typeof(ShaderSource)}");
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


            //if(GetCustomAttribute(shaderSourceType, typeof(ShaderTargetVertexTypeAttribute)) is ShaderTargetVertexTypeAttribute attr) {

            //    if(attr.VertexType != vertexType) {
            //        throw new ArgumentException($"Vertex type mismatch. Shader requires {attr.VertexType.FullName}. Actual vertex is {vertexType.FullName}");
            //    }
            //    else {
            //        return;
            //    }
            //}
            //else {
            //    throw new ArgumentException($"Shader source does not have {typeof(ShaderTargetVertexTypeAttribute).FullName} attribute.");
            //}
        }
    }
}
