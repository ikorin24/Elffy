﻿#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Shading
{
    public readonly ref struct Uniform
    {
        private readonly int _program;

        /// <summary>Send <see cref="float"/> value to uniform variable</summary>
        /// <param name="name">name of uniform variable</param>
        /// <param name="value">value to send</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, float value) => Send(GL.GetUniformLocation(_program, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, int value) => Send(GL.GetUniformLocation(_program, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Vector2 value) => Send(GL.GetUniformLocation(_program, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Vector3 value) => Send(GL.GetUniformLocation(_program, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Vector4 value) => Send(GL.GetUniformLocation(_program, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Color4 value) => Send(GL.GetUniformLocation(_program, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Matrix4 value) => Send(GL.GetUniformLocation(_program, name), value);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, float value) => GL.ProgramUniform1(_program, location, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, int value) => GL.ProgramUniform1(_program, location, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Vector2 value)
            => GL.ProgramUniform2(_program, location, 1, ref Unsafe.As<Vector2, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Vector3 value)
            => GL.ProgramUniform3(_program, location, 1, ref Unsafe.As<Vector3, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Vector4 value)
            => GL.ProgramUniform4(_program, location, 1, ref Unsafe.As<Vector4, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Color4 value)
            => GL.ProgramUniform4(_program, location, 1, ref Unsafe.As<Color4, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Matrix4 value)
            => GL.ProgramUniformMatrix4(_program, location, 1, false, ref Unsafe.As<Matrix4, float>(ref Unsafe.AsRef(value)));

        internal Uniform(int program)
        {
            _program = program;
        }

        public override int GetHashCode() => HashCode.Combine(_program);

        public override string ToString() => _program.ToString();
    }
}