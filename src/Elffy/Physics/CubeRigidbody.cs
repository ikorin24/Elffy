using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Physics
{
    public sealed class CubeRigidbody : Rigidbody3D
    {
        /// <summary>衝突判定を行います</summary>
        /// <remarks>高速化のため引数の null チェックは省略して実装します</remarks>
        /// <param name="target">衝突判定を行う対象の <see cref="Rigidbody"/></param>
        /// <returns>衝突しているか</returns>
        internal override bool CollideWith(Rigidbody target)
        {
            if(target.Type == RigidbodyType.Rigidbody2D) { return false; }
            throw new NotImplementedException();
        }
    }
}
