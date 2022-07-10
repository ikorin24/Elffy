#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Graphics.OpenGL;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Elffy.Shading
{
    internal struct RendererData   // not IDisposable, dispose only from internal
    {
        private IRenderingShader? _shader;
        private Type? _vertexType;
        private ProgramObject _program;

        public ProgramObject Program => _program;
        public IRenderingShader? Shader => _shader;
        public Type? VertexType => _vertexType;

        public RendererDataState State
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

        internal static RendererData Empty => default;

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
            if(State == RendererDataState.Compiled) {
                Release();
            }
            Debug.Assert(State != RendererDataState.Compiled);
            _shader = shader;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProgramObject GetValidProgram()
        {
            var program = _program;
            if(program.IsEmpty) { ThrowEmptyProgram(); }
            return program;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IRenderingShader GetValidShader()
        {
            var shader = _shader;
            if(shader is null) { ThrowEmptyShader(); }
            return shader;
        }

        public bool Compile()
        {
            var state = State;
            if(state == RendererDataState.Compiled) {
                return false;
            }
            Debug.Assert(_shader is not null);
            Debug.Assert(_vertexType is not null);
            var key = new SourceKey(_shader, _vertexType);
            var screen = Engine.GetValidCurrentContext();
            _program = CompiledProgramCacheStore.GetCacheOrCompile(screen, key);
            return true;
        }

        public bool Release()
        {
            if(State != RendererDataState.Compiled) {
                return false;
            }
            Debug.Assert(_shader is not null);
            Debug.Assert(_vertexType is not null);
            var key = new SourceKey(_shader, _vertexType);
            var screen = Engine.GetValidCurrentContext();
            CompiledProgramCacheStore.Delete(screen, key, ref _program);
            try {
                _shader.InvokeOnProgramDisposed();
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
                ref var cache = ref CollectionsMarshal.GetValueRefOrAddDefault(dic, key, out var hasCache);
                if(hasCache == false) {
                    var program = ShaderCompiler.Compile(key.VertSoruce, key.FragSource, key.GeometrySource);
                    cache = new CompiledCache(program, 1);
                }
                else {
                    cache.Count++;
                }
                Debug.Assert(cache.Count > 0);
                Debug.Assert(!cache.Program.IsEmpty);
                return cache.Program;
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
            private readonly IRenderingShader _shaderSource;
            private readonly Type _vertexType;

            public string VertSoruce => _shaderSource.VertexShaderSource;
            public string FragSource => _shaderSource.FragmentShaderSource;
            public string? GeometrySource => _shaderSource.GeometryShaderSource;

            public SourceKey(IRenderingShader shaderSource, Type vertexType)
            {
                _shaderSource = shaderSource;
                _vertexType = vertexType;
            }

            public override bool Equals(object? obj) => obj is SourceKey key && Equals(key);

            public bool Equals(SourceKey other)
            {
                return (VertSoruce == other.VertSoruce)
                    && (FragSource == other.FragSource)
                    && (GeometrySource == other.GeometrySource)
                    && _vertexType == other._vertexType;
            }

            public override int GetHashCode() => _shaderSource.GetSourceHash() ^ _vertexType.GetHashCode();
        }
    }

    internal enum RendererDataState
    {
        NotCompiled = 0,
        ReadyToCompile,
        Compiled,
    }
}
