using Elffy.Core;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Physics
{
    /// <summary>物理演算を行うための剛体クラス</summary>
    public abstract class Rigidbody3D : Rigidbody
    {
        internal Rigidbody3D() : base(RigidbodyType.Rigidbody3D)
        {
        }
    }
}
