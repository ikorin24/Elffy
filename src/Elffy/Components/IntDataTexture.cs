#nullable enable
using Elffy.OpenGL;
using System;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Exceptions;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;
using System.Runtime.InteropServices;
using Elffy.Effective;
using Elffy.Core;

namespace Elffy.Components
{
    public sealed class IntDataTexture : IComponent, IDisposable
    {
        private IntDataTextureImpl _impl;

        public TextureUnitNumber TextureUnit { get; }

        public IntDataTexture(TextureUnitNumber textureUnit)
        {
            TextureUnit = textureUnit;
        }

        public void Apply() => _impl.Apply(TextureUnit);

        public void Dispose()
        {
            if(_impl.Disposed) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public unsafe void Load(ReadOnlySpan<int> data) => _impl.Load(data);

        public unsafe void Load(ReadOnlySpan<Int32DataColor4> texels) => _impl.Load(texels);

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _impl.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(IntDataTexture));     // GC スレッドからでは解放できないので
            }
        }

        public void OnAttached(ComponentOwner owner)
        {
            // nop
        }

        public void OnDetached(ComponentOwner owner)
        {
            // nop
        }
    }

    internal struct IntDataTextureImpl : IDisposable
    {
        public bool Disposed;
        public TextureObject TextureObject;

        public void Apply(TextureUnitNumber unit)
        {
            TextureObject.Bind(TextureObject, unit);
        }

        public unsafe void Load(ReadOnlySpan<int> data)
        {
            if(data.Length % 4 != 0) { throw new ArgumentException($"{nameof(data)}.Length % 4 is not 0"); }
            Load(data.MarshalCast<int, Int32DataColor4>());
        }

        public unsafe void Load(ReadOnlySpan<Int32DataColor4> texels)
        {
            if(texels.IsEmpty) { return; }
            TextureObject = TextureObject.Create();

            var unit = TextureUnitNumber.Unit0;
            TextureObject.Bind(TextureObject, unit);
            fixed(void* ptr = texels) {

                // 実際のデータは int 型だが float として GPU 側に転送。
                // glsl 内では floatBitsToInt 関数で float のビット表現をそのまま int として取得する。
                //
                // (例 glsl) i番目のテクセルの g の要素を int として取り出す
                // int value = floatBitsToInt(tecelFetch(_sampler1D, i, 0).g);

                GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, texels.Length, 0, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind(unit);
        }

        public void Dispose()
        {
            if(Disposed) { return; }
            Disposed = true;
            TextureObject.Delete(ref TextureObject);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Int32DataColor4 : IEquatable<Int32DataColor4>
    {
        [FieldOffset(0)]
        public int R;
        [FieldOffset(4)]
        public int G;
        [FieldOffset(8)]
        public int B;
        [FieldOffset(12)]
        public int A;

        public Int32DataColor4(int r, int g, int b, int a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public readonly void Deconstruct(out int r, out int g, out int b, out int a)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        public override readonly bool Equals(object? obj) => obj is Int32DataColor4 color && Equals(color);

        public readonly bool Equals(Int32DataColor4 other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(Int32DataColor4 left, Int32DataColor4 right) => left.Equals(right);

        public static bool operator !=(Int32DataColor4 left, Int32DataColor4 right) => !(left == right);
    }
}
