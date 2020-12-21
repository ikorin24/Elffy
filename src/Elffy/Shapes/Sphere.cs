#nullable enable
using Elffy.Core;
using System;

namespace Elffy.Shapes
{
    [Obsolete("Not implemented yet", true)]
    public class Sphere : Renderable
    {
        public Sphere()
        {
            throw new NotImplementedException();
        }

        protected override void OnActivated()
        {
            throw new NotImplementedException();
            //base.OnActivated();
            //LoadGraphicBuffer(vertexArray.Span, indexArray.Span);
        }
    }
}
