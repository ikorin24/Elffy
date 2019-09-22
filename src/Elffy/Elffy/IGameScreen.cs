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

        event EventHandler Initialized;
        event EventHandler Rendering;
        event EventHandler Rendered;

        bool AddFrameObject(FrameObject frameObject);

        bool RemoveFrameObject(FrameObject frameObject);

        FrameObject FindObject(string tag);

        List<FrameObject> FindAllObject(string tag);

        void Run();
        void Close();
    }
}
