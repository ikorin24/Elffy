#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Core;
using System;
using System.Threading;

namespace Elffy.Shapes
{
    [Obsolete("Not implemented yet", true)]
    public class Sphere : Renderable
    {
        public Sphere()
        {
            throw new NotImplementedException();
        }

        protected override UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
