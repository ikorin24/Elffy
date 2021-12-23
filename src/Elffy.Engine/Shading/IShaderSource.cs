#nullable enable

namespace Elffy.Shading
{
    internal interface IShaderSource
    {
        string VertexShaderSource { get; }
        string FragmentShaderSource { get; }
        string? GeometryShaderSource { get; }

        internal int GetSourceHash();

        internal ShaderProgram Compile(Renderable owner);
    }
}
