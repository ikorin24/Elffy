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

        internal static UniTask<AsyncUnit> CreateTask(FrameTimingPoint? timingPoint, CancellationToken cancellationToken)
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
                return new UniTask<AsyncUnit>(new FrameTimingAwaitableTaskSource(timingPoint, token, cancellationToken), token);
            }
            instance.InitFields(timingPoint, token, cancellationToken);
            Debug.Assert(instance._next is null);
            return new UniTask<AsyncUnit>(instance, token);
        }

        private static void Return(FrameTimingAwaitableTaskSource source)
        {
            // Clear the fields which is reference or contain reference type.
            Debug.Assert(source._next is null);
            Debug.Assert(source._timingPoint is null);
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
        private FrameTimingAwaitableTaskSource(FrameTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
        {
            InitFields(timingPoint, token, cancellationToken);
            Debug.Assert(_next is null);
        }

        [MemberNotNull(nameof(_timingPoint))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitFields(FrameTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
        {
            // All fields must be set except '_next'
            _timingPoint = timingPoint ?? _completedTimingPoint;
            _token = token;
            _cancellationToken = cancellationToken;
        }
    }
}
