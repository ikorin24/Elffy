#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Graphics.OpenGL;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Elffy.UI;
using System.IO;

namespace Elffy.Shading
{
    internal struct RendererData   // not IDisposable, dispose only from internal
    {
        private readonly Renderable? _target;
        private IRenderingShader? _shader;
        private Type? _vertexType;
        private ShaderSource _shaderSource;
        private ProgramObject _program;

        public readonly Renderable Target
        {
            get
            {
                Debug.Assert(_target is not null);
                return _target;
            }
        }
        public readonly ProgramObject Program => _program;
        public readonly IRenderingShader? Shader => _shader;
        public readonly Type? VertexType => _vertexType;

        public readonly RendererDataState State
        {
            get
            {
                if(_program.IsEmpty == false) {
                    Debug.Assert(_shader is not null);
                    Debug.Assert(_vertexType is not null);
                    Debug.Assert(_shaderSource.IsEmpty == false);
                    return RendererDataState.Compiled;
                }
                if(_vertexType is not null && _shader is not null) {
                    return RendererDataState.ReadyToCompile;
                }
                return RendererDataState.NotCompiled;
            }
        }

        [Obsolete("Don't use default constructor.", true)]
        public RendererData() => throw new NotSupportedException("Don't use default constructor.");

        public RendererData(Renderable target)
        {
            _target = target;
            _shader = null;
            _vertexType = null;
            _program = ProgramObject.Empty;
            _shaderSource = ShaderSource.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ProgramObject GetValidProgram()
        {
            var program = _program;
            if(program.IsEmpty) { ThrowEmptyProgram(); }
            return program;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly IRenderingShader GetValidShader()
        {
            var shader = _shader;
            if(shader is null) { ThrowEmptyShader(); }
            return shader;
        }

        public bool SetVertexType(Type? vertexType)
        {
            if(_vertexType == vertexType) { return false; }
            if(State == RendererDataState.Compiled) {
                Release();
            }
            Debug.Assert(State != RendererDataState.Compiled);
            _vertexType = vertexType;
            return true;
        }

        public bool SetShader(IRenderingShader? shader)
        {
            if(_shader == shader) { return false; }

            if(shader is ISingleTargetRenderingShader s && s.HasTarget) {
                ThrowMultiTarget();
                [DoesNotReturn] static void ThrowMultiTarget() => throw new InvalidOperationException($"It is not possible to attach to more than one target.");
            }

            if(State == RendererDataState.Compiled) {
                Release();
            }
            Debug.Assert(State != RendererDataState.Compiled);
            (var old, _shader) = (_shader, shader);
            if(old is not null) {
                old.OnDetachedInternal(Target);
            }
            if(shader is not null) {
                shader.OnAttachedInternal(Target);
            }
            return true;
        }

        private bool CompileIfNeeded()
        {
            var state = State;
            if(state == RendererDataState.Compiled) {
                return false;
            }
            Debug.Assert(_shader is not null);
            Debug.Assert(_vertexType is not null);

            var target = Target;
            var layer = target.Layer as WorldLayer; // TODO:
            var shaderSource = _shader.GetShaderSourceInternal(target, layer);
            _shaderSource = shaderSource;

            var key = new SourceKey(shaderSource, _vertexType);
            var screen = Engine.GetValidCurrentContext();
            try {
                _program = CompiledProgramCacheStore.GetCacheOrCompile(screen, key);
            }
            catch(GlslException ex) {
                throw new InvalidDataException($"Invalid GLSL shaders. (Shader: {_shader.GetType().FullName}, Target: {target.GetType().FullName}, Layer: {layer.GetType().FullName}).", ex);
            }
            return true;
        }

        public bool CompileForRenderable(Renderable renderable)
        {
            if(CompileIfNeeded() == false) {
                return false;
            }
            Debug.Assert(_vertexType is not null);
            if(_shader is RenderingShader shader) {
                shader.DefineLocationInternal(_program, renderable, _vertexType);
            }
            else {
                Debug.Fail($"Shader must be {nameof(RenderingShader)}. actual: {_shader?.GetType()?.FullName}");
            }
            return true;
        }

        public bool CompileForUI(Control control)
        {
            if(CompileIfNeeded() == false) {
                return false;
            }
            Debug.Assert(_vertexType is not null);
            if(_shader is UIRenderingShader shader) {
                shader.DefineLocationInternal(_program, control, _vertexType);
            }
            else {
                Debug.Fail($"Shader must be {nameof(UIRenderingShader)}. actual: {_shader?.GetType()?.FullName}");
            }
            return true;
        }

        public bool CompileForShadowMap(Renderable target)
        {
            if(CompileIfNeeded() == false) {
                return false;
            }
            Debug.Assert(_vertexType is not null);
            if(_shader is RenderShadowMapShader shader) {
                var definition = new VertexDefinition(_program);
                shader.DefineLocation(definition, target, _vertexType);
            }
            else {
                Debug.Fail($"Shader must be {nameof(RenderShadowMapShader)}. actual: {_shader?.GetType()?.FullName}");
            }
            return true;
        }

        public bool Release()
        {
            if(State != RendererDataState.Compiled) {
                return false;
            }
            Debug.Assert(_shader is not null);
            Debug.Assert(_vertexType is not null);
            Debug.Assert(_shaderSource.IsEmpty == false);

            var key = new SourceKey(_shaderSource, _vertexType);
            var screen = Engine.GetValidCurrentContext();
            CompiledProgramCacheStore.Delete(screen, key, ref _program);

            _shaderSource = ShaderSource.Empty;
            (var shader, _shader) = (_shader, null);
            _shader = null;
            shader.OnDetachedInternal(Target);
            try {
                shader.OnProgramDisposedInternal();
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                // ignore exceptions in user code.
            }
            Debug.Assert(State != RendererDataState.Compiled);
            return true;
        }

        [DoesNotReturn] static void ThrowEmptyProgram() => throw new InvalidOperationException("The shader program is empty or deleted.");
        [DoesNotReturn] static void ThrowEmptyShader() => throw new InvalidOperationException("The shader is empty or deleted.");

        private static class CompiledProgramCacheStore
        {
            [ThreadStatic]
            private static Dictionary<IHostScreen, Dictionary<SourceKey, CompiledCache>>? _dictionaries;

            public static ProgramObject GetCacheOrCompile(IHostScreen screen, SourceKey key)
            {
                _dictionaries ??= new();

                ref var dic = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionaries, screen, out _);
                dic ??= new();

                // Don't use CollectionsMarshal.GetValueRefOrAddDefault method.
                // Compilation of shaders may throw exception.
                ref var cache = ref CollectionsMarshal.GetValueRefOrNullRef(dic, key);
                if(Unsafe.IsNullRef(ref cache)) {
                    var program = ShaderCompiler.Compile(key.VertexShader, key.FragmentShader, key.GeometryShader);
                    var value = new CompiledCache(program, 1);
                    dic.Add(key, value);
                    Debug.Assert(!program.IsEmpty);
                    return program;
                }
                else {
                    cache.Count++;
                    Debug.Assert(cache.Count > 0);
                    Debug.Assert(!cache.Program.IsEmpty);
                    return cache.Program;
                }
            }

            public static void Delete(IHostScreen screen, SourceKey key, ref ProgramObject program)
            {
                if(_dictionaries is null) {
                    goto DELETE;
                }
                if(_dictionaries.TryGetValue(screen, out var dic) == false) {
                    goto DELETE;
                }

                ref var cache = ref CollectionsMarshal.GetValueRefOrNullRef(dic, key);
                if(Unsafe.IsNullRef(ref cache)) {
                    goto DELETE;
                }
                cache.Count--;
                if(cache.Count <= 0) {
                    dic.Remove(key);
                    goto DELETE;
                }
                program = ProgramObject.Empty;
                return;
            DELETE:
                ProgramObject.Delete(ref program);
                return;
            }
        }

        [DebuggerDisplay("{Program}, Count={Count}")]
        private struct CompiledCache : IEquatable<CompiledCache>
        {
            public ProgramObject Program;
            public int Count;

            public CompiledCache(ProgramObject program, int count)
            {
                Program = program;
                Count = count;
            }

            public override bool Equals(object? obj) => obj is CompiledCache cache && Equals(cache);

            public bool Equals(CompiledCache other) => Program.Equals(other.Program) && Count == other.Count;

            public override int GetHashCode() => HashCode.Combine(Program, Count);
        }

        private readonly struct SourceKey : IEquatable<SourceKey>
        {
            private readonly int _hashCache;
            private readonly ShaderSource _shaderSource;
            private readonly Type _vertexType;

            public string? VertexShader => _shaderSource.VertexShader;
            public string? FragmentShader => _shaderSource.FragmentShader;
            public string? GeometryShader => _shaderSource.GeometryShader;

            public SourceKey(ShaderSource shaderSource, Type vertexType)
            {
                _shaderSource = shaderSource;
                _vertexType = vertexType;
                _hashCache = HashCode.Combine(shaderSource, vertexType);
            }

            public override bool Equals(object? obj) => obj is SourceKey key && Equals(key);

            public bool Equals(SourceKey other) =>
                _shaderSource == other._shaderSource &&
                _vertexType == other._vertexType;

            public override int GetHashCode() => _hashCache;
        }
    }

    internal enum RendererDataState
    {
        NotCompiled = 0,
        ReadyToCompile,
        Compiled,
    }
}
