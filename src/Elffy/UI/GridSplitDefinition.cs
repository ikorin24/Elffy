#nullable enable
using System;
using UnmanageUtility;

namespace Elffy.UI
{
    internal struct GridSplitDefinition : IDisposable
    {
        private UnmanagedArray<LayoutLength>? _def;
        private UnmanagedArray<(float c, float p)>? _pos;
        private float _constSum;
        private float _proportionSum;

        public float EvalSize(float parentSize, int index)
        {
            if(_def is null || (uint)index >= (uint)_def.Length) {
                return parentSize;
            }
            ref var d = ref _def.GetReference(index);
            switch(d.Type) {
                case LayoutLengthType.Length:
                default:
                    return d.Value;
                case LayoutLengthType.Proportion:
                    return (parentSize - _constSum) * d.Value / _proportionSum;
            }
        }

        public float EvalPosition(float parentSize, int index)
        {
            if(_pos is null || (uint)index >= (uint)_pos.Length) {
                return 0f;
            }
            var (c, p) = _pos[index];
            return c + p * parentSize;
        }

        public void SetDefinition(ReadOnlySpan<LayoutLength> layout)
        {
            if(_def is null) {
                _def = layout.ToUnmanagedArray();
            }
            else if(_def.Length != layout.Length) {
                _def.Dispose();
                _def = layout.ToUnmanagedArray();
            }

            if(_pos is null) {
                _pos = new UnmanagedArray<(float, float)>(layout.Length);
            }
            else if(_pos.Length != layout.Length) {
                _pos.Dispose();
                _pos = new UnmanagedArray<(float, float)>(layout.Length);
            }

            _constSum = 0;
            _proportionSum = 0;
            foreach(var l in layout) {
                switch(l.Type) {
                    case LayoutLengthType.Length:
                    default:
                        _constSum += l.Value;
                        break;
                    case LayoutLengthType.Proportion:
                        _proportionSum += l.Value;
                        break;
                }
            }

            float c = 0f;
            float p = 0f;
            for(int i = 0; i < layout.Length; i++) {
                _pos[i] = (c, p);
                switch(layout[i].Type) {
                    case LayoutLengthType.Length:
                    default:
                        c += layout[i].Value;
                        break;
                    case LayoutLengthType.Proportion:
                        p += layout[i].Value / _proportionSum;
                        break;
                }
            }
        }

        public void Dispose()
        {
            _def?.Dispose();
            _pos?.Dispose();
        }
    }
}
