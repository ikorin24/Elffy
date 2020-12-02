#nullable enable
using Elffy;
using Elffy.Diagnostics;
using Elffy.Core;

[assembly: GenerateResourceFile("Resources", "Resources.dat")]
[assembly: GenerateCustomVertex(nameof(Sandbox) + ".CustomVertex",
    "Position", typeof(Vector3), 0, VertexFieldMarshalType.Float, 3
)]

namespace Sandbox
{
    static class Program
    {
        static void Main()
        {
            try {
                DevEnv.Run();
                Game.Start(1200, 675, "Sandbox", "icon.ico", Startup.Start);
            }
            finally {
                DevEnv.Stop();
            }
        }
    }
}
