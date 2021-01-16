#nullable enable

namespace Elffy.Shading
{
    internal interface IShaderSource
    {
        string VertexShaderSource { get; }
        string FragmentShaderSource { get; }

        internal int GetSourceHash();
    }
}
