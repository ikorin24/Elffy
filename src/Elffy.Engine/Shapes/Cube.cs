#nullable enable
using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Elffy.Shapes
{
    public class Cube : Renderable
    {
        public Cube()
        {
            Activating.Subscribe(static (f, ct) =>
            {
                PrimitiveMeshProvider<Vertex>.GetCube(
                    SafeCast.As<Cube>(f),
                    static (self, vertices, indices) => self.LoadMesh(vertices, indices));
                return UniTask.CompletedTask;
            });
        }
    }
}
