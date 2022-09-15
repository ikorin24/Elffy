#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Shading
{
    internal interface IRenderingShader
    {
        ShaderSource GetShaderSourceInternal(Renderable target, ObjectLayer layer);
        void OnProgramDisposedInternal();
        void OnAttachedInternal(Renderable target);
        void OnDetachedInternal(Renderable detachedTarget);
    }

    internal interface ISingleTargetRenderingShader : IRenderingShader
    {
        Renderable? Target { get; }

        [MemberNotNullWhen(true, nameof(Target))]
        bool HasTarget { get; }
    }
}
