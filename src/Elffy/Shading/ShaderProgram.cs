#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
using Elffy.Core;
using Elffy.OpenGL;
using System.Diagnostics;
using System.Collections.Generic;

namespace Elffy.Shading
{
    public sealed class ShaderProgram : IDisposable // TODO: IDisposable をやめる、internal からしか破棄できないようにする
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
