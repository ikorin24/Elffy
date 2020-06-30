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
    public sealed class ShaderProgram : IDisposable
    {
        private static readonly Dictionary<ShaderSource, (ProgramObject po, int count)> _onMemoryCache
            = new Dictionary<ShaderSource, (ProgramObject po, int count)>();

        private ProgramObject _program;
        private ShaderSource _shaderSource;
        private bool _initialized;

        internal bool IsReleased => _program.IsEmpty;

        internal ShaderProgram(ShaderSource shaderSource, Func<ProgramObject> compiledProgramObjectFactory)
        {
            if(shaderSource is null) { throw new ArgumentNullException(nameof(shaderSource)); }

            // OpenGL のシェーダーは実行時にしかコンパイルできないため、
            // 一度コンパイルしてGPU側にロードされたプログラムは、
            // ソースをキーにしてキャッシュしておくことで2回目以降コンパイルせずに済む。

            if(_onMemoryCache.TryGetValue(shaderSource, out var onMemory)) {
                _onMemoryCache[shaderSource] = (onMemory.po, onMemory.count + 1);
                _program = onMemory.po;
            }
            else {
                _program = compiledProgramObjectFactory();
                _onMemoryCache.Add(shaderSource, (_program, 1));
                Debug.Assert(_program.IsEmpty == false);
            }
            _shaderSource = shaderSource;
        }


        ~ShaderProgram() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Apply(Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsReleased) { throw new InvalidOperationException("this shader program is empty or deleted."); }
            if(!_initialized) { throw new InvalidOperationException("The shader is not initialized."); }
            ProgramObject.Bind(_program);
            _shaderSource!.SendUniforms(_program, target, lights, model, view, projection);
        }

        internal void Initialize(in VAO vao, in VBO vbo)
        {
            VAO.Bind(vao);
            VBO.Bind(vbo);
            _shaderSource!.DefineLocation(_program);
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

                // OpenGL 内のシェーダープログラムの実体は、参照数が0になった時に削除する。
                // このインスタンスの持つプログラムは Empty にしておくが、他に使っているインスタンスがまだあれば
                // 削除自体は行わない。

                var onMemory = _onMemoryCache[_shaderSource];
                onMemory.count--;
                if(onMemory.count == 0) {
                    _onMemoryCache.Remove(_shaderSource);
                    ProgramObject.Delete(ref _program);
                }
                else {
                    _onMemoryCache[_shaderSource] = onMemory;
                    _program = ProgramObject.Empty;
                }
            }
            else {
                // Can not release resources because finalizer is called from another thread.
                throw new MemoryLeakException(GetType());
            }
        }
    }
}
