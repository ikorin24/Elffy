using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    public class FrameEventArgs : EventArgs
    {
        internal FrameEventArgs(double elapsed)
        {
            Time = elapsed;
        }

        public double Time { get; private set; }
    }

    public delegate void FrameEventHandler(FrameEventArgs e);
}
