using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    public delegate void ActionEventHandler<T>(T sender);

    public delegate void ActionEventHandler<T, TArg>(T sender, TArg e) where TArg : EventArgs ;
}
