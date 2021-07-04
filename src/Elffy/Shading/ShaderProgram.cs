#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.UI;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Shading
{
    /// <summary>Shader program associated with specific <see cref="Renderable"/>.</summary>
    public readonly struct ShaderProgram   // not IDisposable, dispose only from internal
    {
        private readonly Renderable _owner;
        private readonly ProgramObject _program;

        internal bool IsEmpty => _program.IsEmpty;

        internal static ShaderProgram Empty => default;

        private ShaderProgram(ProgramObject program, Renderable owner)
        {
            _program = program;
            _owner = owner;
        }

        internal static ShaderProgram Create(Renderable owner)
        {
            var screen = Engine.CurrentContext;
            if(screen is null) {
                ThrowInvalidContext();
                [DoesNotReturn] static void ThrowInvalidContext() => throw new InvalidOperationException("Invaid context");
            }
            var shader = owner.ShaderInternal;
            Debug.Assert(shader is not null);
            var key = new SourceKey(shader);
            return new ShaderProgram(ShaderProgramCache.Get(screen, key), owner);
        }

        /// <summary>Apply the shader program</summary>
        /// <param name="model">model matrix</param>
        /// <param name="view">view matrix</param>
        /// <param name="projection">projection matrix</param>
        public void Apply(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsEmpty) { ThrowEmptyShader(); }
            if(!_owner.IsLoaded) { ThrowNotInitialized(); }

            Debug.Assert(_owner is not UIRenderable, $"Use {nameof(ApplyForUI)} method for {nameof(UIRenderable)}.");
            Debug.Assert(_owner.Shader is not null);
            
            ProgramObject.Bind(_program);
            _owner.Shader.SendUniformsInternal(_program, _owner, model, view, projection);
        }

        internal void ApplyForUI(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsEmpty) { ThrowEmptyShader(); }
            if(!_owner.IsLoaded) { ThrowNotInitialized(); }
            ProgramObject.Bind(_program);

            SafeCast.NotNullAs<UIShaderSource>(_owner.ShaderInternal)
                    .SendUniformsInternal(_program, SafeCast.NotNullAs<UIRenderable>(_owner).Control, model, view, projection);
        }

        internal void Initialize(Type vertexType)
        {
            Debug.Assert(_owner is not UIRenderable, $"Use {nameof(InitializeForUI)} method for {nameof(UIRenderable)}.");
            Debug.Assert(_owner.Shader is not null);

            VAO.Bind(_owner.VAO);
            VBO.Bind(_owner.VBO);
            _owner.Shader.DefineLocationInternal(_program, _owner, vertexType);
            VAO.Unbind();
            VBO.Unbind();
        }

        internal void InitializeForUI()
        {
            Debug.Assert(_owner is UIRenderable);

            VAO.Bind(_owner.VAO);
            VBO.Bind(_owner.VBO);
            SafeCast.NotNullAs<UIShaderSource>(_owner.ShaderInternal)
                    .DefineLocationInternal(_program, SafeCast.NotNullAs<UIRenderable>(_owner).Control);
            VAO.Unbind();
            VBO.Unbind();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()     // not IDisposable, dispose only from internal
        {
            // [NOTE] The struct is readonly struct, but I change the field by Unsafe.
            // Be careful.

            if(IsEmpty) { return; }
            var screen = Engine.CurrentContext;
            if(screen is null) {
                ThrowInvalidContext();
                [DoesNotReturn] static void ThrowInvalidContext() => throw new InvalidOperationException("Invaid context");
            }

            Debug.Assert(_owner.ShaderInternal is not null);
            var key = new SourceKey(_owner.ShaderInternal);
            ShaderProgramCache.Delete(screen, key, ref Unsafe.AsRef(in _program));
        }

        [DoesNotReturn] static void ThrowEmptyShader() => throw new InvalidOperationException("this shader program is empty or deleted.");
        [DoesNotReturn] static void ThrowNotInitialized() => throw new InvalidOperationException("The shader is not initialized.");


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
