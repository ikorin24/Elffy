#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    partial class FrameTimingAwaitableTaskSource
    {
        private const int MaxPoolingCount = 1024;
        private static short _tokenFactory;
        private static FastSpinLock _lock;
        private static FrameTimingAwaitableTaskSource? _root;
        private static int _pooledCount;

        internal static UniTask<AsyncUnit> CreateTask(AsyncBackEndPoint endPoint, FrameTiming timing, CancellationToken cancellationToken)
        {
            short token;
            FrameTimingAwaitableTaskSource? instance;
            try {
                _lock.Enter();
                token = _tokenFactory;
                _tokenFactory = unchecked((short)(token + 1));
                instance = _root;
                if(instance is not null) {
                    Debug.Assert(_pooledCount > 0);
                    _pooledCount--;
                    _root = instance._next;
                    instance._next = null;
                }
            }
            finally {
                _lock.Exit();
            }

            if(instance is null) {
                Debug.Assert(_pooledCount == 0);
                return new UniTask<AsyncUnit>(new FrameTimingAwaitableTaskSource(endPoint, timing, token, cancellationToken), token);
            }
            instance.InitFields(endPoint, timing, token, cancellationToken);
            Debug.Assert(instance._next is null);
            return new UniTask<AsyncUnit>(instance, token);
        }

        private static void Return(FrameTimingAwaitableTaskSource source)
        {
            // Clear the fields which is reference or contain reference type.
            Debug.Assert(source._next is null);
            Debug.Assert(source._endPoint is null);
            source._cancellationToken = default;

            // Add the instance to the pool.
            try {
                _lock.Enter();
                var pooledCount = _pooledCount;
                if(pooledCount == MaxPoolingCount) { return; }
                var root = _root;
                _pooledCount = pooledCount + 1;
                if(root is null) {
                    _root = source;
                }
                else {
                    _root = source;
                    source._next = root;
                }
            }
            finally {
                _lock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameTimingAwaitableTaskSource(AsyncBackEndPoint endPoint, FrameTiming timing, short token, CancellationToken cancellationToken)
        {
            InitFields(endPoint, timing, token, cancellationToken);
            Debug.Assert(_next is null);
        }

        [MemberNotNull(nameof(_endPoint))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitFields(AsyncBackEndPoint endPoint, FrameTiming timing, short token, CancellationToken cancellationToken)
        {
            // All fields must be set except '_next'
            if(timing == FrameTiming.NotSpecified) {
                _endPoint = _completedEndPoint;
            }
            else {
                _endPoint = endPoint;
            }
            _timing = timing;
            _token = token;
            _cancellationToken = cancellationToken;
        }
    }
}
