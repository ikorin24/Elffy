using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public abstract class GameObject : IDisposable
    {
        public abstract void Render();

        public abstract void Dispose();
    }
}
