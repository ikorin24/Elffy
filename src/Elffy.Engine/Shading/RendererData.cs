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
        private FixedShaderSourceCache _fixedShaderSource;
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
            _fixedShaderSource = FixedShaderSourceCache.Empty;
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

        public void SetVertexType(Type vertexType)
        {
            // [NOTE]
            // Don't change vertex type after compilation of shader.
            // There is no way to do that.
            // Although it needs re-compilation if change vertex type after compilation,
            // the resources which IRenderingShader instance would have is already disposed when the instance detached.
            if(_vertexType != null) {
                throw new InvalidOperationException("Vertex type is already set. Cannot change it.");
            }
            Debug.Assert(State != RendererDataState.Compiled);
            _vertexType = vertexType;
        }

        public void SetShader(IRenderingShader? shader)
        {
            if(_shader == shader) { return; }
            if(shader is ISingleTargetRenderingShader s && s.HasTarget) {
                ThrowMultiTarget();
                [DoesNotReturn] static void ThrowMultiTarget() => throw new InvalidOperationException($"It is not possible to attach to more than one target.");
            }
            if(State == RendererDataState.Compiled) {
                Reset();
            }
            shader?.OnAttachedInternal(Target);
            _shader = shader;
        }

        private bool CompileIfNeeded()
        {
            var screen = Engine.GetValidCurrentContext();
            var state = State;
            if(state == RendererDataState.Compiled) {
                return false;
            }
            Debug.Assert(_shader is not null);
            Debug.Assert(_vertexType is not null);

            var target = Target;
            var layer = target.Layer;
            Debug.Assert(layer is not null);
            var context = new ShaderGetterContext(screen, layer, target);
            var shaderSource = _shader.GetShaderSourceInternal(in context);
            try {
                if(shaderSource.TryFixed(out var fixedShaderSource)) {
                    var key = new SourceKey(fixedShaderSource, _vertexType);
                    _fixedShaderSource = fixedShaderSource;
                    _program = CompiledProgramCacheStore.GetCacheOrCompile(screen, key);
                }
                else {
                    _program = ShaderCompiler.Compile(shaderSource.VertexShader, shaderSource.FragmentShader, shaderSource.GeometryShader);
                }
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
                var screen = renderable.Screen;
                var layer = renderable.Layer;
                Debug.Assert(screen is not null);
                Debug.Assert(layer is not null);
                var context = new LocationDefinitionContext(screen, layer, renderable, _vertexType);
                shader.DefineLocationInternal(_program, in context);
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

        public void Release()
        {
            Reset();
            _shader = null;
            _vertexType = null;
            Debug.Assert(State == RendererDataState.NotCompiled);
        }

        private void Reset()
        {
            if(State == RendererDataState.Compiled) {
                Debug.Assert(_shader is not null);
                Debug.Assert(_vertexType is not null);
                if(_fixedShaderSource.IsEmpty) {
                    ProgramObject.Delete(ref _program);
                }
                else {
                    var key = new SourceKey(_fixedShaderSource, _vertexType);
                    var screen = Engine.GetValidCurrentContext();
                    CompiledProgramCacheStore.Delete(screen, key, ref _program);
                    _fixedShaderSource = FixedShaderSourceCache.Empty;
                }
            }

            var shader = _shader;
            if(shader != null) {
                shader.OnDetachedInternal(Target);
                try {
                    shader.OnProgramDisposedInternal();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions in user code.
                }
            }
            Debug.Assert(State != RendererDataState.Compiled);
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
            private readonly FixedShaderSourceCache _shaderSource;
            private readonly Type _vertexType;

            public ReadOnlySpan<byte> VertexShader => _shaderSource.VertexShader;
            public ReadOnlySpan<byte> FragmentShader => _shaderSource.FragmentShader;
            public ReadOnlySpan<byte> GeometryShader => _shaderSource.GeometryShader;

            public SourceKey(FixedShaderSourceCache shaderSource, Type vertexType)
            {
                _shaderSource = shaderSource;
                _vertexType = vertexType;
            }

            public override bool Equals(object? obj) => obj is SourceKey key && Equals(key);

            public bool Equals(SourceKey other) =>
                _shaderSource == other._shaderSource &&
                _vertexType == other._vertexType;

            public override int GetHashCode() => HashCode.Combine(_shaderSource, _vertexType);
        }
    }

    internal enum RendererDataState
    {
        NotCompiled = 0,
        ReadyToCompile,
        Compiled,
    }
}
