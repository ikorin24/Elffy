using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Physics
{
    public sealed class SphereRigidbody : Rigidbody3D
    {
        public float Radius { get; }

        public SphereRigidbody(float radius)
        {
            if(radius < 0 || float.IsNaN(radius)) { throw new ArgumentException($"{nameof(radius)} is negative or NaN."); }
            Radius = radius;
        }

        /// <summary>衝突判定を行います</summary>
        /// <remarks>高速化のため引数の null チェックは省略して実装します</remarks>
        /// <param name="target">衝突判定を行う対象の <see cref="Rigidbody"/></param>
        /// <returns>衝突しているか</returns>
        internal override bool CollideWith(Rigidbody target)
        {
            if(target.Type == RigidbodyType.Rigidbody2D) { return false; }
            if(target is SphereRigidbody sphere) {
                var limit = Radius + sphere.Radius;
                return (limit * limit) >= (sphere.WorldPosition - WorldPosition).LengthSquared;
            }

            return false;       // TODO: 他の条件
        }
    }
}
