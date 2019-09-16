using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public abstract class Definition
    {
        protected virtual void Initialize() { }

        public virtual void Activate() { }
    }
}
