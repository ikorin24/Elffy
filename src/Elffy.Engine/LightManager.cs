#nullable enable
using Elffy.Effective;
using Elffy.Features;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public sealed class LightManager
    {
        private readonly int _maxLightCount;

        private readonly IHostScreen _screen;
        private MappedFloatDataTextureCore<Vector4> _lightPos;
        private MappedFloatDataTextureCore<Color4> _lightColor;
        private MappedFloatDataTextureCore<Matrix4> _lightMatrices;
        private ValueTypeRentMemory<ShadowMapData> _shadowMaps;

        private ILight?[]? _lights;
        private int _lightCount;
        private IndexGenerator _indexGenerator;
        private Int16TokenFactory _tokenFactory;

        public IHostScreen Screen => _screen;

        public int MaxLightCount => _maxLightCount;

        public int LightCount => _lightCount;

        public TextureObject PositionTexture => _lightPos.TextureObject;
        public TextureObject ColorTexture => _lightColor.TextureObject;
        public TextureObject MatrixTexture => _lightMatrices.TextureObject;

        internal LightManager(IHostScreen screen)
        {
            _maxLightCount = 32;
            _indexGenerator = new IndexGenerator(_maxLightCount);
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

        internal bool TryRegisterLight<TLight>(Func<LightManager, int, short, TLight> ctor, [MaybeNullWhen(false)] out TLight light) where TLight : class, ILight
        {
            Debug.Assert(Engine.IsThreadMain);
            if(_lightCount >= _maxLightCount) {
                light = default;
                return false;
            }

            var token = _tokenFactory.CreateToken();
            var index = _indexGenerator.GetIndex();
            light = ctor.Invoke(this, index, token);
            Debug.Assert(light.LifeState == LifeState.New);
            _lights ??= new ILight[_maxLightCount];
            Debug.Assert(_lights[index] == null);
            _lights[index] = light;
            _lightCount++;
            return true;
        }

        internal void RemoveLight(ILight light)
        {
            Debug.Assert(Engine.IsThreadMain);

            var index = light.Index;
            if(index < 0) { return; }
            var lights = _lights;
            Debug.Assert(lights != null);
            lights[index] = null;
            _indexGenerator.ReturnIndex(index);
            _lightCount--;
        }

        internal void InitializeShadowMap(int index, int size)
        {
            ref var shadowMap = ref _shadowMaps[index];
            Debug.Assert(shadowMap.IsEmpty);
            shadowMap.Initialize(size, size);
        }

        internal bool ValidateToken(int index, short token)
        {
            var lights = _lights;
            if(lights == null) { return false; }
            if((uint)index >= (uint)lights.Length) {
                return false;
            }
            var light = lights[index];
            if(light == null) {
                return false;
            }
            return light.Token == token;
        }

        public LightCollection GetLights() => new LightCollection(this);

        internal ref readonly Vector4 GetPosition(int index) => ref _lightPos[index];

        internal ref readonly Color4 GetColor(int index) => ref _lightColor[index];

        internal ref readonly Matrix4 GetMatrix(int index) => ref _lightMatrices[index];

        internal ref readonly ShadowMapData GetShadowMap(int index) => ref _shadowMaps[index];

        [SkipLocalsInit]
        internal void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset)
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

        internal void UpdatePositions(SpanUpdateAction<Vector4> action) => _lightPos.Update(action);

        internal void UpdatePositions<TArg>(TArg arg, SpanUpdateAction<Vector4, TArg> action) => _lightPos.Update(arg, action);

        internal void UpdateColors(ReadOnlySpan<Color4> colors, int offset) => _lightColor.Update(colors, offset);

        internal void UpdateColors(SpanUpdateAction<Color4> action) => _lightColor.Update(action);

        internal void UpdateColors<TArg>(TArg arg, SpanUpdateAction<Color4, TArg> action) => _lightColor.Update(arg, action);

        internal void Release()
        {
            _lightColor.Dispose();
            _lightPos.Dispose();
            _lightMatrices.Dispose();
            foreach(var map in _shadowMaps.AsSpan()) {
                map.Release();
            }
            _shadowMaps.Dispose();
            _lights.AsSpan().Clear();
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

        private struct IndexGenerator
        {
            private readonly int[] _buf;
            private int _next;


            [Obsolete("Don't use default constructor.", true)]
            public IndexGenerator() => throw new NotSupportedException("Don't use default constructor.");

            public IndexGenerator(int max)
            {
                var buf = new int[max];
                for(int i = 0; i < buf.Length; i++) {
                    buf[i] = i;
                }
                _buf = buf;
                _next = 0;
            }

            public int GetIndex()
            {
                // not thread-safe
                Debug.Assert(_next < _buf.Length);
                return _buf[_next++];
            }

            public void ReturnIndex(int id)
            {
                // not thread-safe
                Debug.Assert(_next > 0);
                _buf[--_next] = id;
            }
        }

        public readonly struct LightCollection : ICollection<ILight>, IEnumerable<ILight>, IEquatable<LightCollection>
        {
            private readonly LightManager? _manager;

            public int Count => _manager?.LightCount ?? 0;

            bool ICollection<ILight>.IsReadOnly => true;

            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Don't use default constructor.", true)]
            public LightCollection() => throw new NotSupportedException("Don't use default constructor.");

            internal LightCollection(LightManager manager) => _manager = manager;

            public Enumerator GetEnumerator() => new Enumerator(_manager);

            public ILight First(Func<ILight, bool> condition)
            {
                ArgumentNullException.ThrowIfNull(condition);
                foreach(var light in this) {
                    if(condition.Invoke(light)) {
                        return light;
                    }
                }
                throw new InvalidOperationException("Sequence contains no matching element");
            }

            public ILight? FirstOrDefault(Func<ILight, bool> condition)
            {
                ArgumentNullException.ThrowIfNull(condition);
                foreach(var light in this) {
                    if(condition.Invoke(light)) {
                        return light;
                    }
                }
                return null;
            }

            public ILight First()
            {
                var e = GetEnumerator();
                if(e.MoveNext() == false) {
                    ThrowNoElements();

                    [DoesNotReturn] static void ThrowNoElements() => throw new InvalidOperationException("Sequence contains no elements");
                }
                return e.Current;
            }

            public ILight? FirstOrDefault()
            {
                var e = GetEnumerator();
                if(e.MoveNext() == false) {
                    return null;
                }
                return e.Current;
            }

            public override bool Equals(object? obj) => obj is LightCollection collection && Equals(collection);

            public bool Equals(LightCollection other) => _manager == other._manager;

            public override int GetHashCode() => _manager?.GetHashCode() ?? 0;

            IEnumerator<ILight> IEnumerable<ILight>.GetEnumerator() => new Enumerator(_manager);
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_manager);

            void ICollection<ILight>.Add(ILight item) => throw new NotSupportedException();

            void ICollection<ILight>.Clear() => throw new NotSupportedException();

            bool ICollection<ILight>.Contains(ILight item)
            {
                var comparer = EqualityComparer<ILight>.Default;
                foreach(var light in this) {
                    if(comparer.Equals(light, item)) {
                        return true;
                    }
                }
                return false;
            }

            void ICollection<ILight>.CopyTo(ILight[] array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);
                if(array.Length < arrayIndex + Count) {
                    throw new ArgumentException("Destination is too short to copy to.", nameof(array));
                }
                int i = 0;
                foreach(var light in this) {
                    array[i + arrayIndex] = light;
                    i++;
                }
            }

            bool ICollection<ILight>.Remove(ILight item) => throw new NotSupportedException();

            public struct Enumerator : IEnumerator<ILight>
            {
                private ArraySliceEnumerator<ILight?> _e;

                public ILight Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        var current = _e.Current;
                        Debug.Assert(current != null);
                        return current;
                    }
                }

                object IEnumerator.Current => ((IEnumerator)_e).Current;

                [EditorBrowsable(EditorBrowsableState.Never)]
                [Obsolete("Don't use default constructor.", true)]
                public Enumerator() => throw new NotSupportedException("Don't use default constructor.");

                internal Enumerator(LightManager? manager)
                {
                    var lights = manager?._lights;
                    _e = new ArraySliceEnumerator<ILight?>(lights, lights?.Length ?? 0);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                LOOP:
                    if(_e.MoveNext() == false) {
                        return false;
                    }
                    var current = _e.Current;
                    if(current == null) {
                        goto LOOP;
                    }
                    return true;
                }

                public void Dispose() => _e.Dispose();

                public void Reset() => _e.Reset();
            }
        }
    }
}
