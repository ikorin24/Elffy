#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Components;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Serialization;
using Elffy.Shapes;
using MMDTools.Unmanaged;
using System;
using System.Drawing;
using System.IO;

namespace Elffy
{
    public interface IResourceLoader
    {
        Stream GetStream(string name);
    }
}
