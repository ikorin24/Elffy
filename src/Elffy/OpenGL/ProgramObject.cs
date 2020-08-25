#nullable enable
using Elffy.Core;
using OpenToolkit.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("Program={Value}")]
    public readonly struct ProgramObject : IEquatable<ProgramObject>
    {
#pragma warning disable 0649    // Field is not assigned.
        private readonly int _program;
#pragma warning restore 0649

        internal readonly int Value => _program;

        internal readonly bool IsEmpty => _program == Consts.NULL;

        internal static ProgramObject Empty => default;

        internal static ProgramObject Create()
        {
            var po = new ProgramObject();
            Unsafe.AsRef(po._program) = GL.CreateProgram();
            return po;
        }

        internal static void Delete(ref ProgramObject po)
        {
            if(!po.IsEmpty) {
                GL.DeleteProgram(po._program);
                Unsafe.AsRef(po._program) = Consts.NULL;
            }
        }

        internal static void Bind(in ProgramObject po)
        {
            GL.UseProgram(po._program);
        }

        internal static void Unbind()
        {
            GL.UseProgram(Consts.NULL);
        }

        public override bool Equals(object? obj) => obj is ProgramObject po && Equals(po);

        public bool Equals(ProgramObject other) => _program == other._program;

        public override int GetHashCode() => _program.GetHashCode();

        public override string ToString() => _program.ToString();
    }
}
