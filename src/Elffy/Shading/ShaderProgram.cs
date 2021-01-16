#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
using Elffy.Core;
using Elffy.OpenGL;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Shading
{
    // TODO: 構造体にできると思う
    public sealed class ShaderProgram   // not IDisposable, dispose only from internal
    {
        private ProgramObject _program;
        private IShaderSource _shaderSource;
        private bool _initialized;

        internal bool IsReleased => _program.IsEmpty;

        private ShaderProgram(ProgramObject program, IShaderSource shaderSource)
        {
            _program = program;
            _shaderSource = shaderSource;
        }

        internal static unsafe ShaderProgram Create<T>(T shaderSource) where T : IShaderSource
        {
            var screen = Engine.CurrentContext;
            if(screen is null) {
                ThrowInvalidContext();
                [DoesNotReturn] static void ThrowInvalidContext() => throw new InvalidOperationException("Invaid context");
            }
            var key = new SourceKey(shaderSource);
            return new ShaderProgram(ShaderProgramCache.Get(screen, key), shaderSource);
        }


        ~ShaderProgram() => Dispose(false);

        public void Apply(Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsReleased) { ThrowEmptyShader(); }
            if(!_initialized) { ThrowNotInitialized(); }
            ProgramObject.Bind(_program);

            Debug.Assert(_shaderSource is ShaderSource);
            SafeCast.NotNullAs<ShaderSource>(_shaderSource)
                    .SendUniforms(_program, target, model, view, projection);

            static void ThrowEmptyShader() => throw new InvalidOperationException("this shader program is empty or deleted.");
            static void ThrowNotInitialized() => throw new InvalidOperationException("The shader is not initialized.");
        }

        internal void Initialize(Renderable target)
        {
            VAO.Bind(target.VAO);
            VBO.Bind(target.VBO);
            SafeCast.NotNullAs<ShaderSource>(_shaderSource)
                    .DefineLocation(_program, target);
            _initialized = true;
            VAO.Unbind();
            VBO.Unbind();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Dispose()     // not IDisposable, dispose only from internal
        {
            if(IsReleased) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose(bool disposing)
        {
            if(disposing) {
                var screen = Engine.CurrentContext;
                if(screen is null) {
                    ThrowInvalidContext();
                    [DoesNotReturn] static void ThrowInvalidContext() => throw new InvalidOperationException("Invaid context");
                }
                var key = new SourceKey(_shaderSource);
                ShaderProgramCache.Delete(screen, key, ref _program);
            }
            else {
                // Can not release resources because finalizer is called from another thread.
                throw new MemoryLeakException(GetType());
            }
        }


        private static class ShaderProgramCache
        {
            [ThreadStatic]
            private static Dictionary<IHostScreen, Dictionary<SourceKey, CompiledCache>>? _dic;

            public static ProgramObject Get(IHostScreen screen, SourceKey key)
            {
                _dic ??= new();
                if(!_dic.TryGetValue(screen, out var dic)) {
                    dic = new();
                    _dic[screen] = dic;
                }
                if(!dic.TryGetValue(key, out var cache)) {
                    cache = new(ShaderSource.CompileToProgramObject(key.VertSoruce, key.FragSource), 1);
                }
                else {
                    cache.Count++;
                }
                dic[key] = cache;
                Debug.Assert(cache.Count > 0);
                Debug.Assert(!cache.Program.IsEmpty);
                return cache.Program;
            }

            public static void Delete(IHostScreen screen, SourceKey key, ref ProgramObject program)
            {
                if(_dic is null) {
                    goto DELETE;
                }
                if(!_dic.TryGetValue(screen, out var dic)) {
                    goto DELETE;
                }
                if(!dic.TryGetValue(key, out var cache)) {
                    goto DELETE;
                }
                cache.Count--;
                dic[key] = cache;
                if(cache.Count <= 0) {
                    dic.Remove(key, out _);
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
            private readonly IShaderSource _shaderSource;

            public string VertSoruce => _shaderSource.VertexShaderSource;
            public string FragSource => _shaderSource.FragmentShaderSource;

            public SourceKey(IShaderSource shaderSource)
            {
                _shaderSource = shaderSource;
            }

            public override bool Equals(object? obj) => obj is SourceKey key && Equals(key);

            public bool Equals(SourceKey other)
            {
                return ReferenceEquals(_shaderSource, other._shaderSource) || (VertSoruce == other.VertSoruce && FragSource == other.FragSource);
            }

            public override int GetHashCode() => _shaderSource.GetSourceHash();
        }
    }
}
