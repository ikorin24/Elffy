#nullable enable
using Elffy.Effective;
using Elffy.Features;
using Elffy.Features.Internal;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Shading
{
    public sealed class LightManager
    {
        private const int ShadowMapSize = 2048;
        private readonly int _maxLightCount;

        private readonly IHostScreen _screen;
        private MappedFloatDataTextureCore<Vector4> _lightPos;
        private MappedFloatDataTextureCore<Color4> _lightColor;
        private MappedFloatDataTextureCore<Matrix4> _lightMatrices;
        private ValueTypeRentMemory<ShadowMapData> _shadowMaps;
        private ArrayPooledListCore<Light> _lightList;


        public IHostScreen Screen => _screen;

        public int LightCount => _lightList.Count;

        public Light this[int index] => _lightList[index];

        public TextureObject PositionTexture => _lightPos.TextureObject;
        public TextureObject ColorTexture => _lightColor.TextureObject;
        public TextureObject MatrixTexture => _lightMatrices.TextureObject;

        internal LightManager(IHostScreen screen)
        {
            _maxLightCount = 32;
            _screen = screen;
        }

        internal void Initialize()
        {
            Debug.Assert(Engine.IsThreadMain);
            var count = _maxLightCount;
            _lightPos.Load(count, static span => span.Clear());
            _lightColor.Load(count, static span => span.Clear());
            _lightMatrices.Load(count, static span => span.Clear());
            _shadowMaps = new ValueTypeRentMemory<ShadowMapData>(count, true);
        }

        public Light CreatePointLight(in Vector3 position, in Color4 color)
        {
            Engine.GetValidCurrentContext();
            var index = _lightList.Count;

            // TODO: shadow map index can be out of range

            ref var shadowMap = ref _shadowMaps[index];
            shadowMap.Initialize(ShadowMapSize, ShadowMapSize);
            var light = new Light(this, index);
            _lightList.Add(light);

            light.Position = new Vector4(position, 1);
            light.Color = color;

            return light;
        }

        internal ref readonly Vector4 GetPosition(int index) => ref _lightPos[index];

        internal ref readonly Color4 GetColor(int index) => ref _lightColor[index];

        internal ref readonly Matrix4 GetMatrix(int index) => ref _lightMatrices[index];

        internal ref readonly ShadowMapData GetShadowMap(int index) => ref _shadowMaps[index];

        public ReadOnlySpan<Vector4> GetPositions() => _lightPos.AsSpan(0, LightCount);

        public ReadOnlySpan<Color4> GetColors() => _lightColor.AsSpan(0, LightCount);

        public ReadOnlySpan<Matrix4> GetMatrices() => _lightMatrices.AsSpan(0, LightCount);

        public ReadOnlySpan<ShadowMapData> GetShadowMaps() => _shadowMaps.AsSpan(0, LightCount);

        [SkipLocalsInit]
        public void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset)
        {
            if(positions.Length == 0) { return; }

            _lightPos.Update(positions, offset);

            const int Threshold = 32;
            var useStack = positions.Length <= Threshold;
            using var buf = useStack ? default : new ValueTypeRentMemory<Matrix4>(positions.Length, false);
            var matrices = useStack ? stackalloc Matrix4[positions.Length] : buf.AsSpan();
            for(int i = 0; i < positions.Length; i++) {
                CreateLightMatrix(positions[i], out var lView, out var lProj);
                matrices[i] = lProj * lView;
            }
            _lightMatrices.Update(matrices.MarshalCast<Matrix4, Color4>(), offset * 4);
        }

        public void UpdatePositions(SpanUpdateAction<Vector4> action) => _lightPos.Update(action);

        public void UpdatePositions<TArg>(TArg arg, SpanUpdateAction<Vector4, TArg> action) => _lightPos.Update(arg, action);

        public void UpdateColors(ReadOnlySpan<Color4> colors, int offset) => _lightColor.Update(colors, offset);

        public void UpdateColors(SpanUpdateAction<Color4> action) => _lightColor.Update(action);

        public void UpdateColors<TArg>(TArg arg, SpanUpdateAction<Color4, TArg> action) => _lightColor.Update(arg, action);

        internal void Release()
        {
            _lightColor.Dispose();
            _lightPos.Dispose();
            _lightMatrices.Dispose();
            foreach(var map in _shadowMaps.AsSpan()) {
                map.Release();
            }
            _shadowMaps.Dispose();
            _lightList.Clear();
        }

        private static void CreateLightMatrix(Vector4 position, out Matrix4 lView, out Matrix4 lProj)
        {
            // TODO:
            if(position.W == 0) {
                const float Near = 0f;
                const float Far = 1000;
                const float L = 3;
                var vec = position.Xyz.Normalized();
                var v = new Vector3(vec.X, 0, vec.Z);
                var up = Quaternion.FromTwoVectors(v, vec) * Vector3.UnitY;
                Matrix4.LookAt(vec * (Far * 0.5f), Vector3.Zero, up, out lView);
                Matrix4.OrthographicProjection(-L, L, -L, L, Near, Far, out lProj);
            }
            else {
                const float L = 3;
                var pos = position.Xyz / position.W;
                var vec = pos.Normalized();
                var up = Quaternion.FromTwoVectors(new Vector3(vec.X, 0, vec.Z), vec) * Vector3.UnitY;
                Matrix4.LookAt(pos, Vector3.Zero, up, out lView);
                Matrix4.PerspectiveProjection(-L, L, -L, L, 1f, 1000, out lProj);
            }
        }
    }

    public sealed class Light
    {
        private readonly LightManager _lightManager;
        private readonly int _index;

        public Vector4 Position
        {
            get => _lightManager.GetPosition(_index);
            set
            {
                if(value == Position) { return; }
                var positions = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
                _lightManager.UpdatePositions(positions, _index);
            }
        }

        public Color4 Color
        {
            get => _lightManager.GetColor(_index);
            set
            {
                if(value == Color) { return; }
                var colors = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
                _lightManager.UpdateColors(colors, _index);
            }
        }

        public ref readonly Matrix4 LightMatrix => ref _lightManager.GetMatrix(_index);

        public ref readonly ShadowMapData ShadowMap => ref _lightManager.GetShadowMap(_index);

        internal Light(LightManager lightManager, int index)
        {
            _lightManager = lightManager;
            _index = index;
        }

        public IHostScreen GetValidScreen() => _lightManager.Screen;
    }
}
