#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ElffyGenerator
{
    [Generator]
    public class EntryPointGenerator : ISourceGenerator
    {
        private const string AttributesDef =
@"#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    internal static class GameLaunchSetting
    {
        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class EntryPointAttribute : Attribute
        {
            public EntryPointAttribute(Type targetType, string methodName, bool awaiting) { }
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class ScreenSizeAttribute : Attribute
        {
            public ScreenSizeAttribute(int width, int height) { }
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class ScreenTitleAttribute : Attribute
        {
            public ScreenTitleAttribute(string title) { }
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class ScreenIconAttribute : Attribute
        {
            public ScreenIconAttribute(string resourceName) { }
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class AllowMultiLaunchAttribute : Attribute
        {
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class DoNotNeedSynchronizationContextAttribute : Attribute
        {
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class ResourceLoaderAttribute : Attribute
        {
            public ResourceLoaderAttribute(Type resourceLoaderType, string arg) { }
        }

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class LaunchDevEnvAttribute : Attribute
        {
            public LaunchDevEnvAttribute() { }
        }
    }
}
";

        public void Execute(GeneratorExecutionContext context)
        {
            var sb = new StringBuilder();
            sb.Append(GeneratorUtil.GetGeneratorSigniture(typeof(EntryPointGenerator)));
            sb.Append(AttributesDef);
            context.AddSource("GameLaunchSetting", SourceText.From(sb.ToString(), Encoding.UTF8));
            try {
                if(context.SyntaxReceiver is not SyntaxReceiver receiver) { throw new Exception("Why is the receiver null ??"); }

                sb.Clear();
                sb.Append(GeneratorUtil.GetGeneratorSigniture(typeof(EntryPointGenerator)));
                receiver.DumpSource(sb, context.Compilation);
                context.AddSource("GameEntryPoint", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
            catch {
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            private readonly Regex _entryPointRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.EntryPoint(Attribute)?$");
            private readonly Regex _screenSizeRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ScreenSize(Attribute)?$");
            private readonly Regex _screenTitleRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ScreenTitle(Attribute)?$");
            private readonly Regex _screenIconRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ScreenIcon(Attribute)?$");
            private readonly Regex _allowMultiRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.AllowMultiLaunch(Attribute)?$");
            private readonly Regex _doNotNeedSyncContextRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.DoNotNeedSynchronizationContext(Attribute)?$");
            private readonly Regex _resourceLoaderRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ResourceLoader(Attribute)?$");
            private readonly Regex _launchDevEnvRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.LaunchDevEnv(Attribute)?$");

            private AttributeSyntax? _entryPoint;
            private AttributeSyntax? _screenSize;
            private AttributeSyntax? _screenTitle;
            private AttributeSyntax? _screenIcon;
            private AttributeSyntax? _allowMulti;
            private AttributeSyntax? _doNotNeedSyncContext;
            private AttributeSyntax? _resourceLoader;
            private AttributeSyntax? _launchDevEnv;

            private const int DefaultWidth = 800;
            private const int DefaultHeight = 600;
            private const string DefaultTitle = "Game";

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode.IsKind(SyntaxKind.Attribute) == false) { return; }
                if(syntaxNode is not AttributeSyntax attr) { return; }

                var attrName = attr.Name.ToString();
                if(_entryPointRegex.IsMatch(attrName)) {
                    _entryPoint = attr;
                }
                else if(_screenSizeRegex.IsMatch(attrName)) {
                    _screenSize = attr;
                }
                else if(_screenTitleRegex.IsMatch(attrName)) {
                    _screenTitle = attr;
                }
                else if(_screenIconRegex.IsMatch(attrName)) {
                    _screenIcon = attr;
                }
                else if(_allowMultiRegex.IsMatch(attrName)) {
                    _allowMulti = attr;
                }
                else if(_doNotNeedSyncContextRegex.IsMatch(attrName)) {
                    _doNotNeedSyncContext = attr;
                }
                else if(_resourceLoaderRegex.IsMatch(attrName)) {
                    _resourceLoader = attr;
                }
                else if(_launchDevEnvRegex.IsMatch(attrName)) {
                    _launchDevEnv = attr;
                }
            }

            private void AppendStart(StringBuilder sb)
            {
                var singleLaunch = _allowMulti is null;
                sb.Append(@"
        public static void Start()
        {").AppendChoose(singleLaunch, @"
            ProcessHelper.SingleLaunch(Launch);", @"
            Launch();").Append(@"
        }
");
            }

            private void AppendLaunch(StringBuilder sb, Compilation compilation)
            {
                var useResource = _resourceLoader is not null;
                var resourceLoaderTypeName = useResource ? GeneratorUtil.GetAttrArgTypeName(_resourceLoader!, 0, compilation) : "";
                var resourceLoaderArg = useResource ? GeneratorUtil.GetAttrArgString(_resourceLoader!, 1, compilation) : "";

                var useDevEnv = _launchDevEnv is not null;

                var useSyncContext = _doNotNeedSyncContext is null;
                sb.Append(@"
        private static void Launch()
        {
            try {").AppendIf(useDevEnv, @"
                Elffy.Diagnostics.DevEnv.Run();").Append(@"
                Engine.Run();").AppendIf(useResource, $@"
                Resources.Initialize(arg => new {resourceLoaderTypeName}(arg), ""{resourceLoaderArg}"");").Append(@"
                var screen = CreateScreen();
                screen.Initialized += OnScreenInitialized;").AppendIf(useSyncContext, @"
                CustomSynchronizationContext.CreateIfNeeded(out _, out var syncContextReciever);").Append(@"
                screen.Show();
                while(Engine.HandleOnce()) { ").AppendIf(useSyncContext, @"
                    syncContextReciever?.DoAll();").Append(@"
                }
            }
            finally {").AppendIf(useResource, @"
                Resources.Close();").AppendIf(useSyncContext, @"
                CustomSynchronizationContext.Restore();").Append(@"
                Engine.Stop();").AppendIf(useDevEnv, @"
                Elffy.Diagnostics.DevEnv.Stop();").Append(@"
            }
        }
");
            }

            private void AppendOnScreenInitialized(StringBuilder sb, Compilation compilation)
            {
                Debug.Assert(_entryPoint is not null);
                var entryTypeName = GeneratorUtil.GetAttrArgTypeName(_entryPoint!, 0, compilation);
                var methodName = GeneratorUtil.GetAttrArgString(_entryPoint!, 1, compilation);
                var awaiting = GeneratorUtil.GetAttrArgBool(_entryPoint!, 2, compilation);

                sb.Append(@"
        private static ").AppendIf(awaiting, "async ").Append("void OnScreenInitialized(IHostScreen screen)").Append(@"
        {
            Timing.Initialize(screen);
            Game.Initialize(screen);
            GameUI.Initialize(screen.UIRoot);
            try {
                ").AppendIf(awaiting, "await ").Append($"{entryTypeName}.{methodName}();").Append(@"
            }
            catch {
                // Don't throw. (Ignore exceptions in user code)
            }
        }
");
            }

            private void AppendScreenRun(StringBuilder sb, Compilation compilation)
            {
                var width = _screenSize is not null ? GeneratorUtil.GetAttrArgInt(_screenSize, 0, compilation) : DefaultWidth;
                var height = _screenSize is not null ? GeneratorUtil.GetAttrArgInt(_screenSize, 1, compilation) : DefaultHeight;
                var title = _screenTitle is not null ? GeneratorUtil.GetAttrArgString(_screenTitle, 0, compilation) : DefaultTitle;

                sb.Append(@"
        private static IHostScreen CreateScreen()
        {
            static IHostScreen PlatformSwitch(int width, int height, string title, ReadOnlySpan<RawImage> icon)
            {
                switch(Platform.PlatformType) {
                    case PlatformType.Windows:
                    case PlatformType.MacOSX:
                    case PlatformType.Linux:
                        return new Window(width, height, title, WindowStyle.Default, icon);
                    case PlatformType.Android:
                    case PlatformType.Other:
                    default:
                        throw new PlatformNotSupportedException();
                }
            }
");
                if(_screenIcon is null) {
                    sb.Append(@$"
            return PlatformSwitch({width}, {height}, ""{title}"", ReadOnlySpan<RawImage>.Empty);");
                }
                else {
                    var iconName = GeneratorUtil.GetAttrArgString(_screenIcon, 0, compilation);
                    sb.Append($@"
            Span<RawImage> icon = stackalloc RawImage[1];").Append($@"
            using var iconStream = Resources.Loader.GetStream(""{iconName}"");").Append(@"
            using var bitmap = new Bitmap(iconStream);
            using var pixels = bitmap.GetPixels(ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var pixelSpan = pixels.AsSpan();

            // Pixels of System.Drawing.Bitmap is layouted as (B, G, R, A).
            // Convert them as (R, G, B, A)
            for(int i = 0; i < pixelSpan.Length / 4; i++) {
                var (r, g, b) = (pixelSpan[i * 4 + 2], pixelSpan[i * 4 + 1], pixelSpan[i * 4]);
                pixelSpan[i * 4] = r;
                pixelSpan[i * 4 + 1] = g;
                pixelSpan[i * 4 + 2] = b;
            }
            icon[0] = new RawImage(pixels.Width, pixels.Height, pixels.Ptr);").Append($@"
            return PlatformSwitch({width}, {height}, ""{title}"", icon);");
                }
                sb.Append(@"
        }
");
            }

            public void DumpSource(StringBuilder sb, Compilation compilation)
            {
                if(_entryPoint is null) {
                    return;
                }

                sb.Append(
@"#nullable enable
using Elffy.Core;
using Elffy.Imaging;
using Elffy.Platforms;
using Elffy.Threading;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    internal static class GameEntryPoint
    {
");
                AppendStart(sb);
                AppendLaunch(sb, compilation);
                AppendScreenRun(sb, compilation);
                AppendOnScreenInitialized(sb, compilation);
                sb.Append(
@"
    }
}
");
                return;
            }
        }
    }
}
