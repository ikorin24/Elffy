using Elffy.UI;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    internal interface IGameScreen : IDisposable
    {
        VSyncMode VSync { get; set; }
        double TargetRenderFrequency { get; set; }
        IUIRoot UIRoot { get; }
        Size ClientSize { get; }
        LayerCollection Layers { get; }

        event EventHandler Initialized;
        event EventHandler Rendering;
        event EventHandler Rendered;

        void Run();
        void Close();
    }
}
