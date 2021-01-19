#nullable enable
using Elffy.Core;

namespace Elffy.Shading
{
    internal interface IShaderSource
    {
        string VertexShaderSource { get; }
        string FragmentShaderSource { get; }

        internal int GetSourceHash();

        internal ShaderProgram Compile(Renderable owner);
    }
}
