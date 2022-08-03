#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Components.Implementation;
using Elffy.Effective;
using Elffy.Graphics.OpenGL;

namespace Elffy.Features
{
    /// <summary>Provides a way for holding data in both memory and VRAM (texture).</summary>
    /// <typeparam name="T">element type, in most cases, which is a <see cref="float"/>, <see cref="Vector4"/>, or <see cref="Color4"/> or <see cref="Matrix4"/>.</typeparam>
    public struct MappedFloatDataTextureCore<T> : IEquatable<MappedFloatDataTextureCore<T>>, IDisposable where T : unmanaged
    {
        private FloatDataTextureCore _textureCore;
        private ValueTypeRentMemory<T> _memory;

        public static MappedFloatDataTextureCore<T> Empty => default;

        /// <summary>Byte length of the data</summary>
        public readonly int ByteLength => _memory.Length * Unsafe.SizeOf<T>();

        /// <summary>count of <see cref="float"/> value</summary>
        public readonly int FloatCount => ByteLength * sizeof(float);

        /// <summary>count of <typeparamref name="T"/> element</summary>
        public readonly int DataCount => _memory.Length;

        /// <summary>texture object (texture1D)</summary>
        /// <remarks>Don't write the texture directly via this property.</remarks>
        public readonly TextureObject TextureObject => _textureCore.TextureObject;

        public readonly ref readonly T this[int index] => ref _memory[index];

        public void Load(ReadOnlySpan<T> data, bool asPowerOfTwoTexture = true)
        {
            ThrowIfNotMultipleOfFour(data.GetByteLength());
            if(asPowerOfTwoTexture) {
                _textureCore.LoadAsPOT(data.MarshalCast<T, Color4>());
            }
            else {
                _textureCore.Load(data.MarshalCast<T, Color4>());
            }
            _memory = new ValueTypeRentMemory<T>(data.Length, false);
            data.CopyTo(_memory.AsSpan());
        }

        public void Load(int dataLength, SpanAction<T> initialize, bool asPowerOfTwoTexture = true)
        {
            ArgumentNullException.ThrowIfNull(initialize);
            Load(dataLength, initialize,
                static (span, initialize) => initialize.Invoke(span),
                asPowerOfTwoTexture);
        }

        public void Load<TState>(int dataLength, TState state, SpanAction<T, TState> initialize, bool asPowerOfTwoTexture = true)
        {
            ArgumentNullException.ThrowIfNull(initialize);
            if(dataLength < 0) { throw new ArgumentOutOfRangeException(nameof(dataLength)); }
            ThrowIfNotMultipleOfFour(Unsafe.SizeOf<T>() * dataLength);
            var memory = new ValueTypeRentMemory<T>(dataLength, false);
            try {
                var dataSpan = memory.AsSpan();
                initialize.Invoke(dataSpan, state);
                if(asPowerOfTwoTexture) {
                    _textureCore.LoadAsPOT(dataSpan.MarshalCast<T, Color4>());
                }
                else {
                    _textureCore.Load(dataSpan.MarshalCast<T, Color4>());
                }
            }
            catch {
                memory.Dispose();
                throw;
            }
            _memory = memory;
        }

        public unsafe void Update(ReadOnlySpan<Vector4> data, int xOffset) => Update(data.MarshalCast<Vector4, Color4>(), xOffset);

        public unsafe void Update(ReadOnlySpan<Color4> data, int xOffset)
        {
            var dest = AsColor4SpanPrivate().Slice(xOffset);
            data.CopyTo(dest);
            _textureCore.Update(data, xOffset);
        }

        public void Update(SpanUpdateAction<Vector4> action) => Update(action, static (span, action) => action.Invoke(span));

        public void Update<TArg>(TArg arg, SpanUpdateAction<Vector4, TArg> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            var span = _memory.AsSpan().MarshalCast<T, Vector4>();
            var tracked = new UpdateTrackedSpan<Vector4>(span, out var modifiedRange);
            action.Invoke(tracked, arg);
            var updated = span.Slice(modifiedRange.Start, modifiedRange.Length).MarshalCast<Vector4, Color4>();
            _textureCore.Update(updated, modifiedRange.Start);
        }

        public void Update(SpanUpdateAction<Color4> action) => Update(action, static (span, action) => action.Invoke(span));

        public void Update<TArg>(TArg arg, SpanUpdateAction<Color4, TArg> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            var span = _memory.AsSpan().MarshalCast<T, Color4>();
            var tracked = new UpdateTrackedSpan<Color4>(span, out var modifiedRange);
            action.Invoke(tracked, arg);
            var updated = span.Slice(modifiedRange.Start, modifiedRange.Length);
            _textureCore.Update(updated, modifiedRange.Start);
        }

        public void Dispose()
        {
            _memory.Dispose();
            _textureCore.Dispose();
        }

        public readonly ReadOnlySpan<T> AsSpan() => _memory.AsSpan();
        public readonly ReadOnlySpan<T> AsSpan(int start) => _memory.AsSpan(start);
        public readonly ReadOnlySpan<T> AsSpan(int start, int length) => _memory.AsSpan(start, length);

        public readonly ReadOnlySpan<float> AsFloatSpan() => _memory.AsSpan().MarshalCast<T, float>();

        public readonly ReadOnlySpan<Vector4> AsVector4Span() => _memory.AsSpan().MarshalCast<T, Vector4>();

        public readonly ReadOnlySpan<Color4> AsColor4Span() => _memory.AsSpan().MarshalCast<T, Color4>();

        private readonly Span<Color4> AsColor4SpanPrivate() => _memory.AsSpan().MarshalCast<T, Color4>();

        private static void ThrowIfNotMultipleOfFour(int value)
        {
            if((value & 3) != 0) {
                throw new ArgumentException("Byte length of data must be a multiple of 4. (`sizeof(T) * data.Length % 4 == 0`)");
            }
        }

        public override bool Equals(object? obj) => obj is MappedFloatDataTextureCore<T> core && Equals(core);

        public bool Equals(MappedFloatDataTextureCore<T> other) => _textureCore.Equals(other._textureCore) && _memory.Equals(other._memory);

        public override int GetHashCode() => HashCode.Combine(_textureCore, _memory);
    }

    public delegate void SpanUpdateAction<T>(UpdateTrackedSpan<T> trackedSpan);
    public delegate void SpanUpdateAction<T, in TArg>(UpdateTrackedSpan<T> trackedSpan, TArg arg);

    public readonly ref struct UpdateTrackedSpan<T>
    {
        private readonly Span<T> _span;
        private readonly Span<(int Start, int Length)> _rangeRef;     // TODO: change into ref field in C# 11

        public ReadOnlySpan<T> Span => _span;

        public UpdateTrackedSpan(Span<T> span, out (int Start, int Length) modifiedRange)
        {
            modifiedRange = default;
            _span = span;
            _rangeRef = MemoryMarshal.CreateSpan(ref modifiedRange, 1);
        }

        public T this[int index]
        {
            get => _span[index];
            set
            {
                _span[index] = value;
                ref var range = ref _rangeRef.GetReference();
                if(range.Length == 0) {
                    range.Start = index;
                    range.Length = 1;
                }
                else {
                    var end = Math.Max(index + 1, range.Start + range.Length);
                    range.Start = Math.Min(index, range.Start);
                    range.Length = end - range.Start;
                }
            }
        }
    }
}
