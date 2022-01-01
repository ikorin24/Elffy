#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Elffy.Shading
{
    public readonly ref struct LightUpdateContext
    {
        private readonly LightBuffer _lightBuffer;
        private readonly int _lightCount;
        private readonly LightUpdateMode _mode;
        private readonly ValueTypeRentMemory<LightData> _lights;
        private readonly Span<LightData> _lightSpan;

        public int LightCount => _lightCount;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use default constructor.", true)]
        public LightUpdateContext() => throw new NotSupportedException("Don't use default constructor.");

        internal LightUpdateContext(LightBuffer lightBuffer, LightUpdateMode mode)
        {
            _lightBuffer = lightBuffer;
            _lightCount = lightBuffer.LightCount;
            _mode = mode;
            ReadFromBuffer(lightBuffer, out var lights);
            _lights = lights;
            _lightSpan = lights.AsSpan();
        }

        public ref LightData LightOf(int index) => ref _lightSpan[index];

        public void Dispose()
        {
            try {
                if(_mode == LightUpdateMode.ReadWrite) {
                    WriteToBuffer(_lightBuffer, _lightSpan);
                }
            }
            finally {
                _lights.Dispose();
            }
        }

        private static void ReadFromBuffer(LightBuffer lightBuffer, out ValueTypeRentMemory<LightData> lights)
        {
            var count = lightBuffer.LightCount;
            using var buf = new ValueTypeRentMemory<Vector4>(count * 2, false);
            var pBuf = buf.AsSpan(0, count);
            var cBuf = buf.AsSpan(count, count).MarshalCast<Vector4, Color4>();
            lightBuffer.ReadPositions(pBuf);
            lightBuffer.ReadColors(cBuf);
            lights = new ValueTypeRentMemory<LightData>(lightBuffer.LightCount, false);
            var lightSpan = lights.AsSpan();
            Debug.Assert(lightSpan.Length == pBuf.Length);
            Debug.Assert(lightSpan.Length == cBuf.Length);
            try {
                for(int i = 0; i < lightSpan.Length; i++) {
                    var type = (pBuf.At(i).Z <= 0.0001) ? LightType.DirectLight : LightType.PointLight;
                    lightSpan[i] = new LightData(pBuf.At(i), cBuf.At(i), type);
                }
            }
            catch {
                lights.Dispose();
                throw;
            }
        }

        private static void WriteToBuffer(LightBuffer lightBuffer, ReadOnlySpan<LightData> lights)
        {
            using var buf = new ValueTypeRentMemory<Vector4>(lights.Length * 2, false);
            var pBuf = buf.AsSpan(0, lights.Length);
            var cBuf = buf.AsSpan(lights.Length, lights.Length).MarshalCast<Vector4, Color4>();
            Debug.Assert(lights.Length == pBuf.Length);
            Debug.Assert(lights.Length == cBuf.Length);
            for(int i = 0; i < lights.Length; i++) {
                pBuf.At(i) = lights[i].Position4;
                cBuf.At(i) = lights[i].Color4;
            }
            lightBuffer.UpdatePositions(pBuf, 0);
            lightBuffer.UpdateColors(cBuf, 0);
        }
    }

    public struct LightData : IEquatable<LightData>
    {
        private Vector4 _pos;
        private Color4 _color;
        private LightType _lightType;

        internal Vector4 Position4 { get => _pos; set => _pos = value; }
        internal Color4 Color4 { get => _color; set => _color = value; }

        public Vector3 Position
        {
            get => _pos.Xyz;
            set
            {
                _pos.X = value.X;
                _pos.Y = value.Y;
                _pos.Z = value.Z;
            }
        }

        public Color3 Color
        {
            get => new Color3(_color.R, _color.G, _color.B);
            set
            {
                _color.R = value.R;
                _color.G = value.G;
                _color.B = value.B;
            }
        }

        public LightType Type
        {
            get => _lightType;
            set
            {
                _lightType = value;
                if(_lightType == LightType.DirectLight) {
                    _pos.Z = 0;
                }
                else if(_lightType == LightType.PointLight) {
                    _pos.Z = 1;
                }
            }
        }

        public LightData(in Vector4 pos, in Color4 color, LightType type)
        {
            _pos = pos;
            _color = color;
            _lightType = type;
        }

        public override bool Equals(object? obj) => obj is LightData data && Equals(data);

        public bool Equals(LightData other) => _pos.Equals(other._pos) && _color.Equals(other._color) && _lightType == other._lightType;

        public override int GetHashCode() => HashCode.Combine(_pos, _color, _lightType);

        public static bool operator ==(LightData left, LightData right) => left.Equals(right);

        public static bool operator !=(LightData left, LightData right) => !(left == right);
    }

    public enum LightType
    {
        DirectLight = 0,
        PointLight = 1,
    }

    public enum LightUpdateMode : byte
    {
        Read = 0,
        ReadWrite = 1,
    }
}
