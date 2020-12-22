#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.OpenGL;
using Elffy.Effective;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    public readonly ref struct Uniform
    {
        private readonly ProgramObject _program;

        /// <summary>Get uniform location</summary>
        /// <param name="name">name of the uniform variable</param>
        /// <returns>location of the uniform variable</returns>
        public int GetLocation(string name) => GL.GetUniformLocation(_program.Value, name);

        /// <summary>Send <see cref="float"/> value to uniform variable</summary>
        /// <param name="name">name of uniform variable</param>
        /// <param name="value">value to send</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, float value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, bool value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, int value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Vector2 value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Vector3 value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Vector4 value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Color3 value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Color4 value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, in Matrix4 value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<float> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<int> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<Vector2> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<Vector3> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<Vector4> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<Color3> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<Color4> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(string name, ReadOnlySpan<Matrix4> value) => Send(GL.GetUniformLocation(_program.Value, name), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendTexture1D(string name, in TextureObject value, TextureUnitNumber unit)
            => SendTexture1D(GL.GetUniformLocation(_program.Value, name), value, unit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendTexture2D(string name, in TextureObject value, TextureUnitNumber unit)
            => SendTexture2D(GL.GetUniformLocation(_program.Value, name), value, unit);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, float value) => GL.ProgramUniform1(_program.Value, location, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, bool value) => GL.ProgramUniform1(_program.Value, location, value ? 1 : 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, int value) => GL.ProgramUniform1(_program.Value, location, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Vector2 value)
            => GL.ProgramUniform2(_program.Value, location, 1, ref Unsafe.As<Vector2, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Vector3 value)
            => GL.ProgramUniform3(_program.Value, location, 1, ref Unsafe.As<Vector3, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Vector4 value)
            => GL.ProgramUniform4(_program.Value, location, 1, ref Unsafe.As<Vector4, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Color4 value)
            => GL.ProgramUniform4(_program.Value, location, 1, ref Unsafe.As<Color4, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Color3 value)
            => GL.ProgramUniform3(_program.Value, location, 1, ref Unsafe.As<Color3, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, in Matrix4 value)
            => GL.ProgramUniformMatrix4(_program.Value, location, 1, false, ref Unsafe.As<Matrix4, float>(ref Unsafe.AsRef(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<float> value)
        {
            GL.ProgramUniform1(_program.Value, location, value.Length, ref Unsafe.AsRef(in value.GetReference()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<int> value)
        {
            GL.ProgramUniform1(_program.Value, location, value.Length, ref Unsafe.AsRef(in value.GetReference()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<Vector2> value)
        {
            GL.ProgramUniform2(_program.Value, location, value.Length,
                               ref Unsafe.As<Vector2, float>(ref Unsafe.AsRef(in value.GetReference())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<Vector3> value)
        {
            GL.ProgramUniform3(_program.Value, location, value.Length,
                               ref Unsafe.As<Vector3, float>(ref Unsafe.AsRef(in value.GetReference())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<Vector4> value)
        {
            GL.ProgramUniform4(_program.Value, location, value.Length,
                               ref Unsafe.As<Vector4, float>(ref Unsafe.AsRef(in value.GetReference())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<Color3> value)
        {
            GL.ProgramUniform3(_program.Value, location, value.Length,
                               ref Unsafe.As<Color3, float>(ref Unsafe.AsRef(in value.GetReference())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<Color4> value)
        {
            GL.ProgramUniform4(_program.Value, location, value.Length,
                               ref Unsafe.As<Color4, float>(ref Unsafe.AsRef(in value.GetReference())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send(int location, ReadOnlySpan<Matrix4> value)
        {
            GL.ProgramUniformMatrix4(_program.Value, location, value.Length, false,
                                     ref Unsafe.As<Matrix4, float>(ref Unsafe.AsRef(in value.GetReference())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendTexture1D(int location, in TextureObject value, TextureUnitNumber unit)
        {
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
            TextureObject.Bind1D(value);
            GL.ProgramUniform1(_program.Value, location, (int)unit);

            // Don't unbind here.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendTexture2D(int location, in TextureObject value, TextureUnitNumber unit)
        {
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
            TextureObject.Bind2D(value);
            GL.ProgramUniform1(_program.Value, location, (int)unit);

            // Don't unbind here.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Uniform(ProgramObject program)
        {
            _program = program;
        }

        public override int GetHashCode() => _program.GetHashCode();

        public override string ToString() => _program.ToString();
    }
}
