using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Core
{
    public abstract class Renderable : GameObject
    {
        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        public abstract void Render();
    }
}
