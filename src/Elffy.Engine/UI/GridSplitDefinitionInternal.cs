#nullable enable
using Elffy.Effective;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    internal struct GridSplitDefinitionInternal : IDisposable
    {
        private ValueTypeRentMemory<LayoutLength> _def;
        private ValueTypeRentMemory<CP> _pos;
        private float _constSum;
        private float _proportionSum;

        public float EvalSize(float parentSize, int index)
        {
            if((uint)index >= (uint)_def.Length) {
                return parentSize;
            }
            ref var d = ref _def[index];
            return d.Type switch
            {
                LayoutLengthType.Proportion => (parentSize - _constSum) * d.Value / _proportionSum,
                LayoutLengthType.Length or _ => d.Value,
            };
        }

        public float EvalPosition(float parentSize, int index)
        {
            if((uint)index >= (uint)_pos.Length) {
                return 0f;
            }
            var (c, p) = _pos[index];
            return c + p * parentSize;
        }

        public ReadOnlySpan<LayoutLength> GetDefinition() => _def.AsSpan();

        public void SetDefinition(int count, SpanAction<LayoutLength> setter)
        {
            ArgumentNullException.ThrowIfNull(setter);
            if(count <= 0) { ThrowOutOfRange(nameof(count)); }

            Initialize(count, out var defSpan, out var posSpan);
            setter.Invoke(defSpan);
            (_constSum, _proportionSum) = Update(defSpan, posSpan);
        }

        public void SetDefinition<T>(int count, T arg, SpanAction<LayoutLength, T> setter)
        {
            ArgumentNullException.ThrowIfNull(setter);
            if(count <= 0) { ThrowOutOfRange(nameof(count)); }

            Initialize(count, out var defSpan, out var posSpan);
            setter.Invoke(defSpan, arg);
            (_constSum, _proportionSum) = Update(defSpan, posSpan);
        }

        public void Dispose()
        {
            _def.Dispose();
            _pos.Dispose();
        }

        private void Initialize(int count, out Span<LayoutLength> defSpan, out Span<CP> posSpan)
        {
            _def.Dispose();
            _pos.Dispose();
            _def = new ValueTypeRentMemory<LayoutLength>(count, true);
            _pos = new ValueTypeRentMemory<CP>(count, false);
            defSpan = _def.AsSpan();
            posSpan = _pos.AsSpan();
        }

        private static (float ConstSum, float ProportionSum) Update(ReadOnlySpan<LayoutLength> def, Span<CP> pos)
        {
            float constSum = 0;
            float proportionSum = 0;
            foreach(var d in def) {
                switch(d.Type) {
                    case LayoutLengthType.Length:
                    default: {
                        constSum += d.Value;
                        break;
                    }
                    case LayoutLengthType.Proportion: {
                        proportionSum += d.Value;
                        break;
                    }
                }
            }

            float c = 0f;
            float p = 0f;
            for(int i = 0; i < def.Length; i++) {
                pos[i] = new CP(c, p);
                switch(def[i].Type) {
                    case LayoutLengthType.Length:
                    default: {
                        c += def[i].Value;
                        break;
                    }
                    case LayoutLengthType.Proportion: {
                        p += def[i].Value / proportionSum;
                        break;
                    }
                }
            }
            return (constSum, proportionSum);
        }

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);

        private record struct CP(float c, float p);
    }
}
