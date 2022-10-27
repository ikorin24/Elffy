#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    public sealed class RenderPipeline
    {
        private readonly LazyApplyingList<PipelineOperation> _list;
        private readonly RenderingArea _owner;

        internal const string DebuggerLayerName = "_DEBUGGER_LAYER";

        internal IHostScreen Screen => _owner.OwnerScreen;

        internal RenderPipeline(RenderingArea owner)
        {
            _list = new();
            _owner = owner;
        }

        public bool TryGetOperation(string? name, [MaybeNullWhen(false)] out PipelineOperation operation)
        {
            foreach(var op in _list.AsReadOnlySpan()) {
                if(op.Name == name) {
                    operation = op;
                    return true;
                }
            }
            operation = null;
            return false;
        }

        public bool TryGetOperation<T>(string? name, [MaybeNullWhen(false)] out T operation) where T : PipelineOperation
        {
            foreach(var op in _list.AsReadOnlySpan()) {
                if(op.Name == name && op is T typedOp) {
                    operation = typedOp;
                    return true;
                }
            }
            operation = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(PipelineOperation operation, Action<PipelineOperation> onAdded)
        {
            Debug.Assert(operation is not null);
            Debug.Assert(onAdded is not null);
            Debug.Assert(operation.Owner == this);
            Debug.Assert(operation.LifeState == LifeState.Activating);

            _list.Add(operation, onAdded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(PipelineOperation operation, Action<PipelineOperation> onRemoved)
        {
            ArgumentNullException.ThrowIfNull(operation);
            _list.Remove(operation, onRemoved);
        }

        internal void TerminateAllOperations<T>(T state, Action<T> onDead)
        {
            var operations = _list.AsReadOnlySpan();
            var tasks = new UniTask<PipelineOperation>[operations.Length];
            for(int i = 0; i < tasks.Length; i++) {
                var operation = operations[i];
                try {
                    tasks[i] = operation.Terminate(FrameTiming.NotSpecified);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions
                    tasks[i] = UniTask.FromResult(operation);
                }
            }

            // TODO: await
            TerminateAllPrivate(UniTask.WhenAll(tasks), state, onDead).Forget();
            return;

            static async UniTask TerminateAllPrivate(UniTask<PipelineOperation[]> task, T state, Action<T> onDead)
            {
                try {
                    await task;
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions
                }
                finally {
                    onDead(state);
                }
            }
        }

        internal void AbortAllOperations()
        {
            _list.Clear();
        }

        internal void NotifySizeChanged()
        {
            var screen = Screen;
            foreach(var operation in _list.AsReadOnlySpan()) {
                operation.OnSizeChangedCallback(screen);
            }
        }

        internal void ApplyAdd()
        {
            var applied = _list.ApplyAdd();
            if(applied) {
                _list.AsSpan().Sort(static (l1, l2) => l1.SortNumber - l2.SortNumber);
            }
            foreach(var operation in _list.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.ApplyAdd();
                }
            }
        }

        internal void ApplyRemove()
        {
            foreach(var operation in _list.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.ApplyRemove();
                }
            }
            _list.ApplyRemove();
        }

        internal void EarlyUpdate()
        {
            foreach(var operation in _list.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.EarlyUpdate();
                }
            }
        }

        internal void Update()
        {
            foreach(var operation in _list.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.Update();
                }
            }
        }

        internal void LateUpdate()
        {
            foreach(var operation in _list.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.LateUpdate();
                }
            }
        }

        internal void Render()
        {
            var screen = Screen;

            // render shadow to shadow maps
            foreach(var light in screen.Lights.GetLights()) {
                var shadowMapRef = light.ShadowMap;
                var lightMatrixRef = light.LightMatrix;
                if(shadowMapRef.TryDerefer(out var shadowMap) == false || lightMatrixRef.TryDerefer(out var lightMatrix) == false) {
                    continue;
                }
                FBO.Bind(shadowMap.Fbo, FBO.Target.FrameBuffer);
                var size = shadowMap.Size;
                OpenTK.Graphics.OpenGL4.GL.Viewport(0, 0, size.X, size.Y);
                ElffyGL.Clear(ClearMask.DepthBufferBit);
                foreach(var operation in _list.AsReadOnlySpan()) {
                    if(operation is ObjectLayer layer && layer.IsEnabled) {
                        layer.RenderShadowMap(screen, lightMatrix);
                    }
                }
            }

            // render objects
            var fbo = FBO.Empty;
            var screenSize = screen.FrameBufferSize;
            OpenTK.Graphics.OpenGL4.GL.Viewport(0, 0, screenSize.X, screenSize.Y);
            foreach(var operation in _list.AsReadOnlySpan()) {
                if(operation.IsEnabled) {
                    operation.Execute(screen, ref fbo);
                }
            }
        }
    }
}
