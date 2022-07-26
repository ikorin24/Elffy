#nullable enable
using Cysharp.Threading.Tasks;

namespace Elffy.Shapes
{
    public class Sphere : Renderable
    {
        public Sphere()
        {
            Activating.Subscribe(static (sender, ct) =>
            {
                var self = SafeCast.As<Sphere>(sender);
                PrimitiveMeshProvider<Vertex>.LoadSphere(self,
                    static (self, vertices, indices) => self.LoadMesh(vertices, indices));
                return UniTask.CompletedTask;
            });
        }
    }
}
