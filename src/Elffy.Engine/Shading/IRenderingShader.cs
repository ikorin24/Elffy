﻿#nullable enable

namespace Elffy.Shading
{
    internal interface IRenderingShader
    {
        string VertexShaderSource { get; }
        string FragmentShaderSource { get; }
        string? GeometryShaderSource { get; }

        void InvokeOnProgramDisposed();

        int GetSourceHash();
    }
}