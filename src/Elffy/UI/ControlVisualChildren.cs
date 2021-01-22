#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace Elffy.UI
{
    public readonly struct ControlVisualChildren : IEnumerable<Control>
    {
        private readonly Control? _parent;

        // [NOTE]
        // default instance is valid.

        internal static ControlVisualChildren Empty => default;

        internal ControlVisualChildren(Control? parent)
        {
            _parent = parent;
        }

        public Enumerator GetEnumerator() => new Enumerator(_parent);

        IEnumerator<Control> IEnumerable<Control>.GetEnumerator() => new Enumerator(_parent);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_parent);

        public struct Enumerator : IEnumerator<Control>
        {
            private readonly Control? _parent;
            private Control? _current;
            private Control? _next;

            public Control Current => _current!;

            object IEnumerator.Current => _current!;

            // [NOTE]
            // default(Enumerator) is valid.

            internal Enumerator(Control? parent)
            {
                _parent = parent;
                _current = null;
                _next = parent?.FirstVisualChild;
            }

            public void Dispose()
            {
                // nop
            }

            public bool MoveNext()
            {
                _current = _next;
                if(_current is null) {
                    return false;
                }
                _next = _current.NextVisualSibling;
                return true;
            }

            public void Reset()
            {
                _current = null;
                _next = _parent?.FirstVisualChild;      // _parent can be null when default struct instance
            }
        }
    }
}
