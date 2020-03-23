#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    public struct SceneMatrix
    {
        public Matrix4 Projection;
        public Matrix4 View;
        public Matrix4 Model;

        internal unsafe static readonly int Size = sizeof(SceneMatrix);

        public SceneMatrix(Matrix4 projection, Matrix4 view, Matrix4 model)
        {
            Projection = projection;
            View = view;
            Model = model;
        }
    }
}
