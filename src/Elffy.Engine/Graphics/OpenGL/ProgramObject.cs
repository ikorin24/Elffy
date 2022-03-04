#nullable enable
using OpenTK.Graphics.OpenGL4;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Elffy.Graphics.OpenGL
{
    [DebuggerDisplay("Program={_program}")]
    public readonly struct ProgramObject : IEquatable<ProgramObject>
    {
        private readonly int _program;

        internal int Value => _program;
        internal bool IsEmpty => _program == 0;

        internal static ProgramObject Empty => default;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ProgramObject() => throw new NotSupportedException("Don't use defaut constructor.");

        private ProgramObject(int program)
        {
            _program = program;
        }

        internal static ProgramObject Create()
        {
            GLAssert.EnsureContext();
            return new ProgramObject(GL.CreateProgram());
        }

        internal static void Delete(ref ProgramObject po)
        {
            if(po._program != 0) {
                GLAssert.EnsureContext();
                GL.DeleteProgram(po._program);
                po = default;
            }
        }

        internal static void UseProgram(in ProgramObject po)
        {
            GLAssert.EnsureContext();
            GL.UseProgram(po._program);
        }

        public override bool Equals(object? obj) => obj is ProgramObject po && Equals(po);

        public bool Equals(ProgramObject other) => _program == other._program;

        public override int GetHashCode() => _program.GetHashCode();

        public override string ToString() => _program.ToString();
    }
}
