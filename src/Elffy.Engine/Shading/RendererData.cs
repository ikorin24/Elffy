#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Graphics.OpenGL;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Elffy.Shading
{
    internal readonly struct RendererData   // not IDisposable, dispose only from internal
    {
        private readonly IRenderingShader? _shader;
        private readonly ProgramObject _program;

        public ProgramObject Program => _program;
        public IRenderingShader? Shader => _shader;

        public bool HasShader => _shader is not null;

        public bool IsShaderCompiled => _program.IsEmpty == false;

        internal static RendererData Empty => default;

        [Obsolete("Don't use default constructor", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RendererData() => throw new NotSupportedException("Don't use default constructor");

        public RendererData(IRenderingShader? shader)
        {
            _shader = shader;
            _program = ProgramObject.Empty;
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

        internal static bool Compile(ref RendererData rendererData)
        {
            if(rendererData._program.IsEmpty == false) { return false; }
            var shader = rendererData._shader;
            if(shader is null) { ThrowEmptyShader(); }

            var key = new SourceKey(shader);
            var screen = Engine.GetValidCurrentContext();
            Unsafe.AsRef(in rendererData._program) = CompiledProgramCacheStore.GetCacheOrCompile(screen, key);
            return true;
        }

        internal static bool Release(ref RendererData rendererData)
        {
            // [NOTE] The struct is readonly struct, but I change the field by Unsafe.
            // Be careful.

            if(rendererData.Program.IsEmpty || rendererData._shader is null) {
                return false;
            }
            var screen = Engine.GetValidCurrentContext();
            var key = new SourceKey(rendererData._shader);
            CompiledProgramCacheStore.Delete(screen, key, ref Unsafe.AsRef(in rendererData._program));
            var shader = rendererData._shader;
            Unsafe.AsRef<IRenderingShader?>(rendererData._shader) = null;
            shader.InvokeOnProgramDisposed();
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

            public string VertSoruce => _shaderSource.VertexShaderSource;
            public string FragSource => _shaderSource.FragmentShaderSource;
            public string? GeometrySource => _shaderSource.GeometryShaderSource;

            public SourceKey(IRenderingShader shaderSource)
            {
                _shaderSource = shaderSource;
            }

            public override bool Equals(object? obj) => obj is SourceKey key && Equals(key);

            public bool Equals(SourceKey other)
            {
                if(ReferenceEquals(_shaderSource, other._shaderSource)) {
                    return true;
                }
                return (VertSoruce == other.VertSoruce)
                    && (FragSource == other.FragSource)
                    && (GeometrySource == other.GeometrySource);
            }

            public override int GetHashCode() => _shaderSource.GetSourceHash();
        }
    }
}
