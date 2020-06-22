#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;

namespace Elffy.Components
{
    public interface IComponent
    {
        void OnAttached(ComponentOwner owner) { }

        void OnDetached(ComponentOwner owner) { }
    }
}
