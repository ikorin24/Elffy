#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("IBO={Value}, Length={Length}")]
    public readonly struct IBO : IEquatable<IBO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)


#pragma warning disable 0649    // Disable 'Field is never assigned to, and is always default'
        private readonly int _ibo;
#pragma warning restore 0649
        internal readonly int Length;

        internal readonly int Value => _ibo;
        internal readonly bool IsEmpty => _ibo == Consts.NULL;

        internal static IBO Create()
        {
            var ibo = new IBO();
            Unsafe.AsRef(ibo._ibo) = GL.GenBuffer();
            return ibo;
        }

        internal static void Delete(ref IBO ibo)
        {
            if(!ibo.IsEmpty) {
                GL.DeleteBuffer(ibo.Value);
                Unsafe.AsRef(ibo) = default;
            }
        }

        internal static void Bind(in IBO ibo)
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo._ibo);
        }

        internal static void Unbind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);
        }

        internal static unsafe void BindBufferData(ref IBO ibo, ReadOnlySpan<int> indices, BufferUsageHint usage)
        {
            Bind(ibo);
            fixed(int* ptr = indices) {
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), (IntPtr)ptr, usage);
            }
            Unsafe.AsRef(ibo.Length) = indices.Length;
        }



        public readonly override string ToString() => _ibo.ToString();

        public readonly override bool Equals(object? obj) => obj is IBO ibo && Equals(ibo);

        public readonly bool Equals(IBO other) => _ibo == other._ibo;

        public readonly override int GetHashCode() => HashCode.Combine(_ibo);

        public static bool operator ==(IBO left, IBO right) => left.Equals(right);

        public static bool operator !=(IBO left, IBO right) => !(left == right);
    }
}
