#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;

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
        public sealed class WindowStyleAttribute : Attribute
        {
            public WindowStyleAttribute(WindowStyle windowStyle) { }
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

        [Conditional(""COMPILE_TIME_ONLY"")]
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class GenerateMainMethodAttribute : Attribute
        {
            public GenerateMainMethodAttribute() { }
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
                receiver.DumpSource(sb, context);
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
            private readonly Regex _gameEntryPointRegex = new Regex(@"^(global::)?(Elffy\.)?GameEntryPoint(Attribute)?$");
            private readonly Regex _screenSizeRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ScreenSize(Attribute)?$");
            private readonly Regex _screenTitleRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ScreenTitle(Attribute)?$");
            private readonly Regex _screenIconRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ScreenIcon(Attribute)?$");
            private readonly Regex _windowStyleRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.WindowStyle(Attribute)?$");
            private readonly Regex _allowMultiRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.AllowMultiLaunch(Attribute)?$");
            private readonly Regex _doNotNeedSyncContextRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.DoNotNeedSynchronizationContext(Attribute)?$");
            private readonly Regex _resourceLoaderRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ResourceLoader(Attribute)?$");
            private readonly Regex _launchDevEnvRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.LaunchDevEnv(Attribute)?$");
            private readonly Regex _autoGenerateMainRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.GenerateMainMethod(Attribute)?$");

            private AttributeSyntax? _gameEntryPoint;
            private AttributeSyntax? _screenSize;
            private AttributeSyntax? _screenTitle;
            private AttributeSyntax? _screenIcon;
            private AttributeSyntax? _windowStyle;
            private AttributeSyntax? _allowMulti;
            private AttributeSyntax? _doNotNeedSyncContext;
            private AttributeSyntax? _resourceLoader;
            private AttributeSyntax? _launchDevEnv;
            private AttributeSyntax? _generateMainMethod;

            private bool _error = true;

            private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

            private const int DefaultWidth = 800;
            private const int DefaultHeight = 600;
            private const string DefaultTitle = "Game";
            private const string DefaultWindowStyle = "default";

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode.IsKind(SyntaxKind.Attribute) == false) { return; }
                if(syntaxNode is not AttributeSyntax attr) { return; }

                var attrName = attr.Name.ToString();

                if(_gameEntryPointRegex.IsMatch(attrName)) {
                    if(_gameEntryPoint is null) {
                        _gameEntryPoint = attr;
                        _error = false;
                    }
                    else {
                        _error = true;
                        Diagnostic.Create(DiagnosticDescriptors.MultiEntryPoints, attr.GetLocation());
                    }
                }
                else if(_autoGenerateMainRegex.IsMatch(attrName)) {
                    _generateMainMethod = attr;
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
                else if(_windowStyleRegex.IsMatch(attrName)) {
                    _windowStyle = attr;
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
                Resources.Inject(new {resourceLoaderTypeName}(""{resourceLoaderArg}""));").Append(@"
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

            private void AppendOnScreenInitialized(StringBuilder sb, string entryMethodStr, bool awaiting)
            {
                sb.Append(@"
        private static ").AppendIf(awaiting, "async ").Append("void OnScreenInitialized(IHostScreen screen)").Append(@"
        {
            Timing.Initialize(screen);
            Game.Initialize(screen);
            GameUI.Initialize(screen.UIRoot);
            try {
                ").AppendIf(awaiting, "await ").Append(entryMethodStr).Append(@";
            }
            catch {
                // Don't throw. (Ignore exceptions in user code)
            }
        }");
            }

            private void AppendScreenRun(StringBuilder sb, Compilation compilation)
            {
                var width = _screenSize is not null ? GeneratorUtil.GetAttrArgInt(_screenSize, 0, compilation) : DefaultWidth;
                var height = _screenSize is not null ? GeneratorUtil.GetAttrArgInt(_screenSize, 1, compilation) : DefaultHeight;
                var title = _screenTitle is not null ? GeneratorUtil.GetAttrArgString(_screenTitle, 0, compilation) : DefaultTitle;
                var windowStyle = _windowStyle is not null ? GeneratorUtil.GetAttrArgEnumNum(_windowStyle, 0, compilation) : DefaultWindowStyle;

                sb.Append(@"
        private static IHostScreen CreateScreen()
        {
            static IHostScreen PlatformSwitch(int width, int height, string title, ReadOnlySpan<RawImage> icon)
            {
                switch(Platform.PlatformType) {
                    case PlatformType.Windows:
                    case PlatformType.MacOSX:
                    case PlatformType.Linux:
                        return new Window(width, height, title, ").Append($"(WindowStyle){windowStyle}, ").Append(@"icon);
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

            public void DumpSource(StringBuilder sb, GeneratorExecutionContext context)
            {
                var compilation = context.Compilation;

                foreach(var d in _diagnostics) {
                    context.ReportDiagnostic(d);
                }

                if(_error) { return; }

                var entryMethodSyntax = _gameEntryPoint?.Parent?.Parent as MethodDeclarationSyntax;
                var generateMain = _generateMainMethod is not null;
                if(entryMethodSyntax is null) {
                    return;
                }
                var entryMethodSymbol = compilation.GetSemanticModel(entryMethodSyntax.SyntaxTree)
                                                   .GetDeclaredSymbol(entryMethodSyntax)!;
                var entryMethodStr = entryMethodSymbol.ToString();
                var needAwait = entryMethodSymbol.IsAsync &&
                                GeneratorUtil.IsAwaitableMethod(entryMethodSyntax, compilation);

                var singleLaunch = _allowMulti is null;

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

namespace Elffy
{
    /// <summary>Provides the game entry point</summary>
    internal static class GameEntryPoint
    {
        ").AppendIf(generateMain, @"
        public static int Main(string[] args)
        {
            Start();
            return 0;
        }

        ").Append(@"
        /// <summary>Call game entry point method marked by <see cref=""Elffy.GameEntryPointAttribute""/></summary>
        public static void Start()
        {").AppendChoose(singleLaunch, @"
            ProcessHelper.SingleLaunch(Launch);", @"
            Launch();").Append(@"
        }
");
                AppendLaunch(sb, compilation);
                AppendScreenRun(sb, compilation);
                AppendOnScreenInitialized(sb, entryMethodStr, needAwait);
                sb.Append(@"
    }
}
");
                return;
            }
        }
    }
}
