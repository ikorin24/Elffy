#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Features;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.UI
{
    [DebuggerTypeProxy(typeof(ControlCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct ControlCollection : IEquatable<ControlCollection>
    {
        private readonly Control _owner;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"{nameof(ControlCollection)} (Count = {Count})";

        public Control this[int index] => _owner.ChildrenCore[index];

        public int Count => _owner.ChildrenCore.Count;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ControlCollection() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ControlCollection(Control owner)
        {
            Debug.Assert(owner is not null);
            _owner = owner;
        }

        public UniTask Add(Control item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            if(item.LifeState != LifeState.New) { ThrowNotNewControl(); }
            _owner.ChildrenCore.Add(item);
            return item.AddedToListCallback(_owner, cancellationToken);
        }

        public UniTask Clear()
        {
            var controls = AsSpan();
            if(controls.Length > 0) {
                var tasks = new UniTask[controls.Length + 1];
                for(int i = 0; i < controls.Length; i++) {
                    tasks[i] = controls[i].Children.Clear();
                }
                tasks[tasks.Length - 1] = ClearPrivate(in this);
                return ParallelOperation.WhenAll(tasks);
            }
            else {
                return ClearPrivate(in this);
            }

            static UniTask ClearPrivate(in ControlCollection self)
            {
                return self._owner.ChildrenCore.Clear(static items =>
                {
                    if(items.IsEmpty) {
                        return UniTask.CompletedTask;
                    }
                    if(items.Length == 1) {
                        return items[0]?.RemovedFromListCallback() ?? UniTask.CompletedTask;
                    }
                    var tasks = new UniTask[items.Length];
                    for(int i = 0; i < items.Length; i++) {
                        tasks[i] = items[i]?.RemovedFromListCallback() ?? UniTask.CompletedTask;
                    }
                    return ParallelOperation.WhenAll(tasks);
                });
            }
        }

        public bool Contains(Control item)
        {
            return _owner.ChildrenCore.IndexOf(item) >= 0;
        }

        public int IndexOf(Control item)
        {
            return _owner.ChildrenCore.IndexOf(item);
        }

        public async UniTask<bool> Remove(Control item)
        {
            ArgumentNullException.ThrowIfNull(item);
            var index = _owner.ChildrenCore.IndexOf(item);
            if(index < 0) {
                return false;
            }
            var tasks = new UniTask[2]
            {
                ClearChildrenOfItemAndRemoveItem(_owner, item, index),
                item.RemovedFromListCallback(),
            };
            await ParallelOperation.WhenAll(tasks);
            return true;

            static async UniTask ClearChildrenOfItemAndRemoveItem(Control owner, Control item, int index)
            {
                await item.Children.Clear();
                owner.ChildrenCore.RemoveAt(index);
            }
        }

        public Control[] ToArray() => AsSpan().ToArray();

        public ReadOnlySpan<Control> AsSpan() => _owner.ChildrenCore.AsSpan();

        public ArraySliceEnumerator<Control> GetEnumerator() => _owner.ChildrenCore.GetEnumerator();

        public override bool Equals(object? obj) => obj is ControlCollection collection && Equals(collection);

        public bool Equals(ControlCollection other) => _owner == other._owner;

        public override int GetHashCode() => _owner is null ? 0 : _owner.GetHashCode();

        public static bool operator ==(ControlCollection left, ControlCollection right) => left.Equals(right);

        public static bool operator !=(ControlCollection left, ControlCollection right) => !(left == right);

        [DoesNotReturn]
        private static void ThrowNotNewControl() => throw new ArgumentException($"{nameof(Control)} object is not new.");
    }

    internal sealed class ControlCollectionDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ControlCollection _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Control[] Items => _entity.AsSpan().ToArray();

        public ControlCollectionDebuggerTypeProxy(ControlCollection entity) => _entity = entity;
    }
}
