#nullable enable
using Elffy;

//[assembly: GameLaunchSetting.GenerateMainMethod]
[assembly: GameLaunchSetting.ScreenSize((int)(1200 * 1.5), (int)(675 * 1.5f))]
[assembly: GameLaunchSetting.ScreenTitle("Sandbox")]
[assembly: GameLaunchSetting.ScreenIcon("Sandbox", "icon.ico")]
[assembly: GameLaunchSetting.WindowStyle(WindowStyle.Default)]
[assembly: GameLaunchSetting.LaunchDevEnv]
[assembly: GenerateLocalResource("Resources", "Sandbox.dat")]
[assembly: DefineLocalResource("Sandbox", "Sandbox.dat")]
