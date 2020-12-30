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
    public sealed class ShaderProgram : IDisposable // TODO: IDisposable をやめる、internal からしか破棄できないようにする
    {
        private ProgramObject _program;
        private ShaderSource _shaderSource;
        private bool _initialized;

        internal bool IsReleased => _program.IsEmpty;

        internal unsafe ShaderProgram(ShaderSource shaderSource)
        {
            var screen = Engine.CurrentContext;
            if(screen is null) {
                ThrowInvalidContext();
                [DoesNotReturn] static void ThrowInvalidContext() => throw new InvalidOperationException("Invaid context");
            }

            _program = ShaderProgramCache.Get(screen, shaderSource);
            _shaderSource = shaderSource;
        }


        ~ShaderProgram() => Dispose(false);

        public void Apply(Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsReleased) { ThrowEmptyShader(); }
            if(!_initialized) { ThrowNotInitialized(); }
            ProgramObject.Bind(_program);
            _shaderSource!.SendUniforms(_program, target, model, view, projection);

            static void ThrowEmptyShader() => throw new InvalidOperationException("this shader program is empty or deleted.");
            static void ThrowNotInitialized() => throw new InvalidOperationException("The shader is not initialized.");
        }

        internal void Initialize(Renderable target)
        {
            VAO.Bind(target.VAO);
            VBO.Bind(target.VBO);
            _shaderSource!.DefineLocation(_program, target);
            _initialized = true;
            VAO.Unbind();
            VBO.Unbind();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
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
                ShaderProgramCache.Delete(screen, _shaderSource, ref _program);
            }
            else {
                // Can not release resources because finalizer is called from another thread.
                throw new MemoryLeakException(GetType());
            }
        }


        private static class ShaderProgramCache
        {
            [ThreadStatic]
            private static Dictionary<IHostScreen, Dictionary<ShaderSource, CompiledCache>>? _dic;

            public static ProgramObject Get(IHostScreen screen, ShaderSource shaderSource)
            {
                _dic ??= new();
                if(!_dic.TryGetValue(screen, out var dic)) {
                    dic = new();
                    _dic[screen] = dic;
                }
                if(!dic.TryGetValue(shaderSource, out var cache)) {
                    cache = new() { Program = ShaderSource.CompileToProgramObject(shaderSource), Count = 1 };
                }
                else {
                    cache.Count++;
                }
                dic[shaderSource] = cache;
                Debug.Assert(cache.Count > 0);
                Debug.Assert(!cache.Program.IsEmpty);
                return cache.Program;
            }

            public static void Delete(IHostScreen screen, ShaderSource shaderSource, ref ProgramObject program)
            {
                if(_dic is null) {
                    goto DELETE;
                }
                if(!_dic.TryGetValue(screen, out var dic)) {
                    goto DELETE;
                }
                if(!dic.TryGetValue(shaderSource, out var cache)) {
                    goto DELETE;
                }
                cache.Count--;
                dic[shaderSource] = cache;
                if(cache.Count <= 0) {
                    dic.Remove(shaderSource, out _);
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

            public override bool Equals(object? obj) => obj is CompiledCache cache && Equals(cache);

            public bool Equals(CompiledCache other) => Program.Equals(other.Program) && Count == other.Count;

            public override int GetHashCode() => HashCode.Combine(Program, Count);
        }
    }
}
