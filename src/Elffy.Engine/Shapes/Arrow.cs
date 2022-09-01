#nullable enable
using Cysharp.Threading.Tasks;

namespace Elffy.Shapes
{
    public class Arrow : Renderable
    {
        public Arrow()
        {
            Activating.Subscribe(static (sender, ct) =>
            {
                var self = SafeCast.As<Arrow>(sender);
                PrimitiveMeshProvider<Vertex>.GetArrow(self, (self, vertices, indices) => self.LoadMesh(vertices, indices));
                return UniTask.CompletedTask;
            });
        }

        public void SetDirection(in Vector3 direction)
        {
            Rotation = Quaternion.FromTwoVectors(Vector3.UnitX, direction);
        }

        public void SetDirection(in Vector3 direction, in Vector3 origin)
        {
            Position = origin;
            Rotation = Quaternion.FromTwoVectors(Vector3.UnitX, direction);
        }

        public void SetStartAndEnd(in Vector3 start, in Vector3 end)
        {
            var startToEnd = end - start;
            Position = start;
            Rotation = Quaternion.FromTwoVectors(Vector3.UnitX, startToEnd);
            Scale = new Vector3(startToEnd.Length);
        }
    }
}
