#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading;

public abstract class PostProcess
{
    private static ReadOnlySpan<byte> DefaultVertexShader => """
    #version 410
    in vec3 _pos;
    in vec2 _uv;
    out V2f
    {
        vec2 uv;
    } _v2f;
    uniform vec2 _postProcessUVScale;
    void main()
    {
        _v2f.uv = _uv * _postProcessUVScale;
        gl_Position = vec4(_pos, 1.0);
    }
    """u8;

    /// <summary>Send uniform variables to glsl shader code.</summary>
    /// <param name="dispatcher">helper object to send uniform variables</param>
    /// <param name="screenSize">screen size</param>
    protected abstract void OnRendering(ShaderDataDispatcher dispatcher, in Vector2i screenSize);

    protected abstract PostProcessSource GetPostProcessSource(PostProcessGetterContext context);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnRenderingInternal(ProgramObject program, in Vector2i screenSize, in Vector2 uvScale)
    {
        var uniform = new ShaderDataDispatcher(program);
        uniform.SendUniform("_postProcessUVScale", uvScale);
        OnRendering(uniform, screenSize);
    }

    /// <summary>Compile post process fragment shader.</summary>
    /// <param name="screen"></param>
    /// <returns>compiled program of post process</returns>
    [SkipLocalsInit]
    internal PostProcessProgram Compile(IHostScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var currentContext = Engine.CurrentContext;
        if(currentContext != screen) {
            ContextMismatchException.Throw(currentContext, screen);
        }

        // 0 - 3    polygon
        // | / |
        // 1 - 2

        const float z = 0f;
        ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
        {
            new(-1, 1, z, 0, 1),
            new(-1, -1, z, 0, 0),
            new(1, -1, z, 1, 0),
            new(1, 1, z, 1, 1),
        };
        ReadOnlySpan<int> indices = stackalloc int[6] { 0, 1, 3, 1, 2, 3 };

        var source = GetPostProcessSource(new PostProcessGetterContext(screen));

        VBO vbo = default;
        IBO ibo = default;
        VAO vao = default;
        var program = ProgramObject.Empty;
        try {
            vbo = VBO.Create();
            VBO.BindBufferData(ref vbo, vertices, BufferHint.StaticDraw);
            ibo = IBO.Create();
            IBO.BindBufferData(ref ibo, indices, BufferHint.StaticDraw);
            vao = VAO.Create();
            VAO.Bind(vao);
            var vertexShader = DefaultVertexShader;
            var fragmentShader = source.FragmentShader;
            program = ShaderCompiler.Compile(vertexShader, fragmentShader);
            var definition = new VertexDefinition(program);
            definition.Map<VertexSlim>("_pos", nameof(VertexSlim.Position));
            definition.Map<VertexSlim>("_uv", nameof(VertexSlim.UV));
            VAO.Unbind();
            VBO.Unbind();
            return new PostProcessProgram(this, program, vbo, ibo, vao, screen);
        }
        catch {
            VBO.Unbind();
            IBO.Unbind();
            VAO.Unbind();
            VBO.Delete(ref vbo);
            IBO.Delete(ref ibo);
            VAO.Delete(ref vao);
            ProgramObject.Delete(ref program);
            throw;
        }
    }
}

public readonly ref struct PostProcessSource
{
    public required ReadOnlySpan<byte> FragmentShader { get; init; }
}

public readonly struct PostProcessGetterContext
{
    private readonly IHostScreen _screen;
    public IHostScreen Screen => _screen;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PostProcessGetterContext() => throw new NotSupportedException("Don't use default constructor.");

    public PostProcessGetterContext(IHostScreen screen)
    {
        _screen = screen;
    }
}
