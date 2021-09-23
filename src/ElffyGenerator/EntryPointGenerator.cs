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
            public ScreenIconAttribute(string resourceFileName, string resourceName) { }
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
            private readonly Regex _launchDevEnvRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.LaunchDevEnv(Attribute)?$");
            private readonly Regex _autoGenerateMainRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.GenerateMainMethod(Attribute)?$");

            private AttributeSyntax? _gameEntryPoint;
            private AttributeSyntax? _screenSize;
            private AttributeSyntax? _screenTitle;
            private AttributeSyntax? _screenIcon;
            private AttributeSyntax? _windowStyle;
            private AttributeSyntax? _allowMulti;
            private AttributeSyntax? _doNotNeedSyncContext;
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
                else if(_launchDevEnvRegex.IsMatch(attrName)) {
                    _launchDevEnv = attr;
                }
            }

            private void AppendLaunch(StringBuilder sb, Compilation compilation)
            {
                var useDevEnv = _launchDevEnv is not null;
                var useSyncContext = _doNotNeedSyncContext is null;
                sb.Append(@"
        private static void Launch()
        {
            try {").AppendIf(useDevEnv, @"
                Elffy.Diagnostics.DevEnv.Run();").Append(@"
                Engine.Run();
                var screen = CreateScreen();
                screen.Initialized += OnScreenInitialized;").AppendIf(useSyncContext, @"
                CustomSynchronizationContext.CreateIfNeeded(out _, out var syncContextReciever);").Append(@"
                screen.Activate();
                while(Engine.HandleOnce()) { ").AppendIf(useSyncContext, @"
                    syncContextReciever?.DoAll();").Append(@"
                }
            }
            finally {").AppendIf(useSyncContext, @"
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
        [DebuggerHidden]
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
                var iconResName = _screenIcon is not null ? GeneratorUtil.GetAttrArgString(_screenIcon, 0, compilation) : null;
                var iconName = _screenIcon is not null ? GeneratorUtil.GetAttrArgString(_screenIcon, 1, compilation) : null;

                sb.Append(@"
        private static IHostScreen CreateScreen()
        {").Append($@"
            const int width = {width};
            const int height = {height};
            const string title = ""{title}"";
            const WindowStyle style = (WindowStyle){windowStyle};").Append(@"
            switch(Platform.PlatformType) {
                case PlatformType.Windows:
                case PlatformType.MacOSX:
                case PlatformType.Linux: {").AppendChoose(iconName is null, @"
                    using var icon = Icon.None;", $@"
                    using var iconStream = Resources.{iconResName}.GetStream(""{iconName}"");
                    using var icon = IcoParser.Parse(iconStream);").Append(@"
                    return new Window(width, height, title, style, icon);
                }
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw new PlatformNotSupportedException();
            }
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
using System.Diagnostics;

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
