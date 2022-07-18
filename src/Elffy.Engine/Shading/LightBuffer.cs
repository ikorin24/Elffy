#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Features;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    internal sealed class LightBuffer : IDisposable
    {
        private IHostScreen? _screen;
        private MappedFloatDataTextureCore<Vector4> _lightPos;
        private MappedFloatDataTextureCore<Color4> _lightColor;
        private MappedFloatDataTextureCore<Matrix4> _lightMatrices;

        public int LightCount => _lightPos.DataCount;

        public TextureObject PositionTexture => _lightPos.TextureObject;
        public TextureObject ColorTexture => _lightColor.TextureObject;
        public TextureObject MatrixTexture => _lightMatrices.TextureObject;

        internal LightBuffer()
        {
        }

        ~LightBuffer() => Dispose(false);

        public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _screen;
            return _screen is not null;
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            var screen = Engine.GetValidCurrentContext();
            if(positions.Length != colors.Length) {
                ThrowInvalidLength();
                [DoesNotReturn] static void ThrowInvalidLength() => throw new ArgumentException($"{nameof(positions)} and {nameof(colors)} must have same length.");
            }
            CreateLightsBuffer(positions, colors, out _lightPos, out _lightColor, out _lightMatrices);
            ContextAssociatedMemorySafety.Register(this, screen);
            _screen = screen;
        }

        public ReadOnlySpan<Vector4> GetPositions() => _lightPos.AsSpan();

        public ReadOnlySpan<Color4> GetColors() => _lightColor.AsSpan();

        public ReadOnlySpan<Matrix4> GetMatrices() => _lightMatrices.AsSpan();

        public unsafe void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset)
        {
            _lightPos.Update(positions, offset);
            var matrices = _lightMatrices.GetWritableSpan().Slice(offset, positions.Length);
            CalcLightMatrix(positions, matrices);
            _lightMatrices.UpdateTextureFromMemory(offset, positions.Length);
        }

        public void UpdatePositions(SpanUpdateAction<Vector4> action) => _lightPos.UpdateAsVector4(action);

        public void UpdatePositions<TArg>(TArg arg, SpanUpdateAction<Vector4, TArg> action) => _lightPos.UpdateAsVector4(arg, action);

        public void UpdateColors(ReadOnlySpan<Color4> colors, int offset) => _lightColor.Update(colors, offset);

        public void UpdateColors(SpanUpdateAction<Color4> action) => _lightColor.UpdateAsColor4(action);

        public void UpdateColors<TArg>(TArg arg, SpanUpdateAction<Color4, TArg> action) => _lightColor.UpdateAsColor4(arg, action);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _lightColor.Dispose();
                _lightPos.Dispose();
                _lightMatrices.Dispose();
                _screen = null;
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }


        private unsafe static void CreateLightsBuffer(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors,
                                                      out MappedFloatDataTextureCore<Vector4> posData,
                                                      out MappedFloatDataTextureCore<Color4> colorsData,
                                                      out MappedFloatDataTextureCore<Matrix4> matrixData)
        {
            Debug.Assert(positions.Length == colors.Length);
            colorsData = new();
            posData = new();
            matrixData = new();
            try {
                colorsData.Load(colors);
                posData.Load(positions);

                fixed(Vector4* posPtr = positions) {
                    var posFixed = (Ptr: (IntPtr)posPtr, Length: positions.Length);
                    matrixData.Load(positions.Length, posFixed, static (lightMatrices, posFixed) =>
                    {
                        var posSpan = new ReadOnlySpan<Vector4>((void*)posFixed.Ptr, posFixed.Length);
                        CalcLightMatrix(posSpan, lightMatrices);
                    });
                }
            }
            catch {
                colorsData.Dispose();
                posData.Dispose();
                matrixData.Dispose();
                throw;
            }
        }

        private static void CalcLightMatrix(ReadOnlySpan<Vector4> positions, Span<Matrix4> lightMatrices)
        {
            for(int i = 0; i < positions.Length; i++) {
                const float Near = 0f;
                const float Far = 100;
                const float L = 20;
                if(positions[i].W == 0) {
                    var vec = positions[i].Xyz.Normalized();
                    var v = new Vector3(vec.X, 0, vec.Z);
                    var up = Quaternion.FromTwoVectors(v, vec) * Vector3.UnitY;
                    Matrix4.LookAt(vec * (Far * 0.5f), Vector3.Zero, up, out var view);
                    Matrix4.OrthographicProjection(-L, L, -L, L, Near, Far, out var projection);
                    lightMatrices[i] = projection * view;
                }
                else {
                    //p = positions[i].Xyz / positions[i].W;
                    throw new NotImplementedException();
                }
            }
        }
    }

    public delegate void LightUpdateAction<TArg>(UpdateTrackedSpan<Vector4> positions, UpdateTrackedSpan<Color4> colors, TArg arg);
}
