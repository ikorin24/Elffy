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
        private readonly LazyApplyingList<PipelineOperation> _operations;
        private readonly LazyApplyingList<ILight> _lights;
        private readonly IHostScreen _screen;

        internal const string DebuggerLayerName = "_DEBUGGER_LAYER";

        internal IHostScreen Screen => _screen;

        public ReadOnlySpan<ILight> Lights
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lights.AsReadOnlySpan();
        }

        public ReadOnlySpan<PipelineOperation> Operations
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _operations.AsReadOnlySpan();
        }

        internal RenderPipeline(IHostScreen screen)
        {
            _operations = new();
            _lights = new();
            _screen = screen;
        }

        public PipelineOperation FindOperation(string? name)
        {
            if(TryFindOperation(name, out var operation) == false) {
                ThrowOperationNotFound(name);
                [DoesNotReturn] static void ThrowOperationNotFound(string? name) => throw new ArgumentException($"{nameof(PipelineOperation)} '{name}' is not found.");
            }
            return operation;
        }

        public T FindOperation<T>(string? name) where T : PipelineOperation
        {
            if(TryFindOperation<T>(name, out var operation) == false) {
                ThrowOperationNotFound(name);
                [DoesNotReturn] static void ThrowOperationNotFound(string? name) => throw new ArgumentException($"{typeof(T).FullName} '{name}' is not found.");
            }
            return operation;
        }

        public bool TryFindOperation(string? name, [MaybeNullWhen(false)] out PipelineOperation operation)
        {
            foreach(var op in _operations.AsReadOnlySpan()) {
                if(op.Name == name) {
                    operation = op;
                    return true;
                }
            }
            operation = null;
            return false;
        }

        public bool TryFindOperation<T>(string? name, [MaybeNullWhen(false)] out T operation) where T : PipelineOperation
        {
            foreach(var op in _operations.AsReadOnlySpan()) {
                if(op.Name == name && op is T typedOp) {
                    operation = typedOp;
                    return true;
                }
            }
            operation = null;
            return false;
        }

        internal bool TryFindDebuggerLayer([MaybeNullWhen(false)] out ForwardRenderLayer debuggerLayer)
        {
            return TryFindOperation(DebuggerLayerName, out debuggerLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(PipelineOperation operation, Action<PipelineOperation> onAdded)
        {
            Debug.Assert(operation is not null);
            Debug.Assert(onAdded is not null);
            Debug.Assert(operation.Owner == this);
            Debug.Assert(operation.LifeState == LifeState.Activating);

            _operations.Add(operation, onAdded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(PipelineOperation operation, Action<PipelineOperation> onRemoved)
        {
            ArgumentNullException.ThrowIfNull(operation);
            _operations.Remove(operation, onRemoved);
        }

        internal void TerminateAllOperations<T>(T state, Action<T> onDead)
        {
            var operations = _operations.AsReadOnlySpan();
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
            _operations.Clear();
            _lights.Clear();
        }

        internal void NotifySizeChanged()
        {
            var screen = Screen;
            foreach(var operation in _operations.AsReadOnlySpan()) {
                operation.OnSizeChangedCallback(screen);
            }
        }

        internal void ApplyAdd()
        {
            Debug.Assert(Engine.IsThreadMain);
            _lights.ApplyAdd();
            if(_operations.ApplyAdd()) {
                _operations.Sort(static (l1, l2) => l1.SortNumber - l2.SortNumber);
            }
            foreach(var operation in _operations.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.ApplyAdd();
                }
            }
        }

        internal void ApplyRemove()
        {
            Debug.Assert(Engine.IsThreadMain);
            foreach(var operation in _operations.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.ApplyRemove();
                }
            }
            _operations.ApplyRemove();
            _lights.ApplyRemove();
        }

        internal void EarlyUpdate()
        {
            foreach(var operation in _operations.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.EarlyUpdate();
                }
            }
        }

        internal void Update()
        {
            foreach(var operation in _operations.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.Update();
                }
            }
        }

        internal void LateUpdate()
        {
            foreach(var operation in _operations.AsReadOnlySpan()) {
                if(operation is ObjectLayer layer) {
                    layer.LateUpdate();
                }
            }
        }

        internal void Render()
        {
            var screen = Screen;

            // render shadow to shadow maps
            foreach(var light in screen.RenderPipeline.Lights) {
                var shadowMap = light.ShadowMap;
                FBO.Bind(shadowMap.ShadowMappingFbo, FBO.Target.FrameBuffer);
                OpenTK.Graphics.OpenGL4.GL.Viewport(0, 0, shadowMap.Size.X, shadowMap.Size.Y);
                ElffyGL.Clear(ClearMask.DepthBufferBit);
                foreach(var operation in _operations.AsReadOnlySpan()) {
                    if(operation is ObjectLayer layer && layer.IsEnabled) {
                        layer.RenderShadowMap(screen, shadowMap);
                    }
                }
            }

            // render objects
            var fbo = FBO.Empty;
            var screenSize = screen.FrameBufferSize;
            OpenTK.Graphics.OpenGL4.GL.Viewport(0, 0, screenSize.X, screenSize.Y);
            foreach(var operation in _operations.AsReadOnlySpan()) {
                if(operation.IsEnabled) {
                    operation.Execute(screen, ref fbo);
                }
            }
        }

        internal void AddLight(ILight light, Action<ILight> callback)
        {
            Debug.Assert(Engine.IsThreadMain);
            Debug.Assert(light is not null);
            _lights.Add(light, callback);
        }

        internal void RemoveLight(ILight light, Action<ILight> callback)
        {
            Debug.Assert(Engine.IsThreadMain);
            Debug.Assert(light is not null);
            _lights.Remove(light, callback);
        }
    }
}
