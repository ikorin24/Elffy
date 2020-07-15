#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("VBO={Value}, Length={Length}")]
    public readonly struct VBO : IEquatable<VBO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)


#pragma warning disable 0649    // Disable 'Field is never assigned to, and is always default'
        private readonly int _vbo;
#pragma warning restore 0649
        internal readonly int Length;

        internal readonly int ElementSize;

        internal readonly int Value => _vbo;
        internal readonly bool IsEmpty => _vbo == Consts.NULL;

        internal static VBO Create()
        {
            var vbo = new VBO();
            Unsafe.AsRef(vbo._vbo) = GL.GenBuffer();
            return vbo;
        }

        internal static unsafe void Delete(ref VBO vbo)
        {
            if(!vbo.IsEmpty) {
                GL.DeleteBuffer(vbo.Value);
                Unsafe.AsRef(vbo.ElementSize) = default;
                Unsafe.AsRef(vbo) = default;
            }
        }

        public static void Bind(in VBO vbo)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo._vbo);
        }

        public static void Unbind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, Consts.NULL);
        }

        internal static unsafe void BindBufferData<TVertex>(ref VBO vbo, ReadOnlySpan<TVertex> vertices, BufferUsageHint usage) where TVertex : unmanaged
        {
            Bind(vbo);
            Unsafe.AsRef(vbo.ElementSize) = sizeof(TVertex);
            Unsafe.AsRef(vbo.Length) = vertices.Length;
            fixed(TVertex* ptr = vertices) {
                GL.BufferData(BufferTarget.ArrayBuffer, vbo.Length * vbo.ElementSize, (IntPtr)ptr, usage);
            }
        }



        public override string ToString() => _vbo.ToString();

        public override bool Equals(object? obj) => obj is VBO vbo && Equals(vbo);

        public bool Equals(VBO other) => _vbo == other._vbo && Length == other.Length;

        public override int GetHashCode() => HashCode.Combine(_vbo, Length);

        public static bool operator ==(VBO left, VBO right) => left.Equals(right);

        public static bool operator !=(VBO left, VBO right) => !(left == right);
    }
}
