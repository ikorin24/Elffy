#nullable enable
using Elffy;
using Elffy.Diagnostics;
using Elffy.Core;
using Sandbox;

[assembly: GenerateResourceFile("Resources", "Resources.dat")]
[assembly: GenerateCustomVertex(nameof(Sandbox) + ".CustomVertex",
    "Position", typeof(Vector3), 0, VertexFieldMarshalType.Float, 3
)]

[assembly: GameLaunchSetting.EntryPoint(typeof(Startup), nameof(Startup.Start), true)]
[assembly: GameLaunchSetting.ScreenSize(1200, 675)]
[assembly: GameLaunchSetting.ScreenTitle("Sandbox")]
//[assembly: GameLaunchSetting.ScreenIcon("icon.ico")]
[assembly: GameLaunchSetting.ResourceLoader(typeof(LocalResourceLoader), "Resources.dat")]
[assembly: GameLaunchSetting.AllowMultiLaunch]
[assembly: GameLaunchSetting.DoNotNeedSynchronizationContext]


DevEnv.Run();
GameEntryPoint.Start();
