using Elffy;
using Elffy.Core;

[assembly: GameLaunchSetting.GenerateMainMethod]
[assembly: GameLaunchSetting.ScreenSize((int)(1200 * 1.5), (int)(675 * 1.5f))]
[assembly: GameLaunchSetting.ScreenTitle("Sandbox")]
[assembly: GameLaunchSetting.ScreenIcon("icon.ico")]
[assembly: GameLaunchSetting.WindowStyle(WindowStyle.Default)]
[assembly: GameLaunchSetting.LaunchDevEnv]
[assembly: GameLaunchSetting.ResourceLoader(typeof(LocalResourceLoader), "Resources.dat")]
[assembly: GenerateLocalResource("Resources", "Resources.dat")]
