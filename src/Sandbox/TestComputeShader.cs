#nullable enable
using Elffy;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using System;

namespace Sandbox;

public sealed unsafe class TestComputeShader : IComputeShader
{
    private readonly StatefulFunc<Ssbo> _ssboProvider;

    public TestComputeShader(Func<Ssbo> ssboProvider)
    {
        _ssboProvider = StatefulFunc.Create(ssboProvider);
    }

    public TestComputeShader(in StatefulFunc<Ssbo> ssboProvider)
    {
        if(ssboProvider.IsNone) {
            throw new ArgumentException($"{nameof(ssboProvider)} is none.", nameof(ssboProvider));
        }
        _ssboProvider = ssboProvider;
    }

    void IComputeShader.OnDispatching(ShaderDataDispatcher dispatcher, ComputeShaderContext context)
    {
        var ssbo = _ssboProvider.Invoke();
        dispatcher.BufferBase(ssbo, 0);

        var r = MathF.Sin(context.Screen.Time.TotalSeconds / 2f * MathF.PI * 2f) * 0.5f + 0.5f;
        dispatcher.SendUniform("_r", r);
    }

    ReadOnlySpan<byte> IComputeShader.GetShaderSource() =>
    """
    #version 430

    layout(local_size_x=1, local_size_y=1, local_size_z=1) in;

    uniform float _r;

    layout (std430, binding=0) buffer SSBO
    {
        vec4 _data[];
    };

    void main()
    {
        //uint index = gl_WorkGroupSize.x * gl_NumWorkGroups.x * gl_GlobalInvocationID.y + gl_GlobalInvocationID.x;
        //_data[index] = vec4(0, 1, 0, 1);

        vec2 v = vec2(gl_WorkGroupID) / vec2(gl_NumWorkGroups);
        _data[gl_WorkGroupID.y * gl_NumWorkGroups.x + gl_WorkGroupID.x] = vec4(_r, v.x, v.y, 1);
    }
    """u8;
}

public sealed class TestShader : RenderingShader
{
    private readonly StatefulFunc<(Ssbo Ssbo, int Width, int Height)> _ssboProvider;

    public TestShader(Func<(Ssbo Ssbo, int Width, int Height)> ssboProvider)
    {
        _ssboProvider = StatefulFunc.Create(ssboProvider);
    }

    public TestShader(in StatefulFunc<(Ssbo Ssbo, int Width, int Height)> ssboProvider)
    {
        if(_ssboProvider.IsNone == false) {
            throw new ArgumentException($"{nameof(ssboProvider)} is none.", nameof(ssboProvider));
        }
        _ssboProvider = ssboProvider;
    }

    protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, "_pos", VertexSpecialField.Position);
        definition.Map(vertexType, "_v_uv", VertexSpecialField.UV);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("_mvp", context.Projection * context.View * context.Model);
        var (ssbo, w, h) = _ssboProvider.Invoke();
        dispatcher.BufferBase(ssbo, 0);
        dispatcher.SendUniform("_size", new Vector2(w, h));
    }

    protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer) => new()
    {
        OnlyContainsConstLiteralUtf8 = true,
        VertexShader =
        """
        #version 430
        in vec3 _pos;
        in vec2 _v_uv;
        out vec2 _uv;
        uniform mat4 _mvp;
        void main()
        {
            _uv = _v_uv;
            gl_Position = _mvp * vec4(_pos, 1.0);
        }
        """u8,
        FragmentShader =
        """
        #version 430
        in vec2 _uv;
        uniform vec2 _size;
        out vec4 _outColor;
        layout (std430, binding=0) buffer SSBO
        {
            vec4 _data[];
        };
        void main()
        {
            ivec2 pos = ivec2(_uv * _size);
            int index = int(pos.y * int(_size.y) + pos.x);
            _outColor = _data[index];
        }
        """u8,
    };
}
