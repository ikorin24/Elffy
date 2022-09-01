#nullable enable
using Cysharp.Threading.Tasks;

namespace Elffy.Shapes
{
    /// <summary>Square plain 3D object</summary>
    public class Plain : Renderable
    {
        /// <summary>Create new <see cref="Plain"/></summary>
        public Plain()
        {
            Activating.Subscribe(static (f, ct) =>
            {
                var self = SafeCast.As<Plain>(f);
                PrimitiveMeshProvider<Vertex>.GetPlain(self, static (self, vertices, indices) => self.LoadMesh(vertices, indices));
                return UniTask.FromResult(AsyncUnit.Default);
            });
        }
    }
}
