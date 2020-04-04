#nullable enable
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Elffy.Core
{
    [DebuggerDisplay("{Position}")]
    public unsafe struct Vertex : IEquatable<Vertex>
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;
        public Vector2 TexCoord;

        static Vertex()
        {
            VertexStructLayouter<Vertex>.SetLayouter(() =>
            {
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);
                GL.EnableVertexAttribArray(2);
                GL.EnableVertexAttribArray(3);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), 0);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), sizeof(Vector3));
                GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, sizeof(Vertex), sizeof(Vector3) + sizeof(Vector3));
                GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), sizeof(Vector3) + sizeof(Vector3) + sizeof(Color4));
            });
        }

        public Vertex(Vector3 position, Vector3 normal, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = new Color4(Math.Abs(normal.X), Math.Abs(normal.Y), Math.Abs(normal.Z), 1f);
            TexCoord = texcoord;
        }

        public Vertex(Vector3 position, Vector3 normal, Color4 color, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TexCoord = texcoord;
        }

        public override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

        public bool Equals(Vertex other) => Position.Equals(other.Position) &&
                                            Normal.Equals(other.Normal) &&
                                            Color.Equals(other.Color) &&
                                            TexCoord.Equals(other.TexCoord);

        public override int GetHashCode() => HashCode.Combine(Position, Normal, Color, TexCoord);

        public static bool operator ==(Vertex left, Vertex right) => left.Equals(right);

        public static bool operator !=(Vertex left, Vertex right) => !(left == right);
    }
}
