#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using Elffy.Core;
using Elffy.Diagnostics;

namespace ElffyGenerator
{
    //[Generator]
    public class EntryPointGenerator : ISourceGenerator
    {
        private const string AttributesDef =
@"#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    public static class GameLaunchSetting
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
        public sealed class ResourceTypeAttribute : Attribute
        {
            public ResourceTypeAttribute(Type resourceType, string arg) { }
        }
    }
}
";

        public void Execute(GeneratorExecutionContext context)
        {
            if(context.SyntaxReceiver is not SyntaxReceiver receiver) { throw new Exception("Why is the receiver null ??"); }

            context.AddSource(nameof(EntryPointGenerator), receiver.DumpSource(context.Compilation));
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
            private readonly Regex _resourceTypeRegex = new Regex(@"^(global::)?(Elffy\.)?GameLaunchSetting\.ResourceType(Attribute)?$");

            private AttributeSyntax? _entryPoint;
            private AttributeSyntax? _screenSize;
            private AttributeSyntax? _screenTitle;
            private AttributeSyntax? _screenIcon;
            private AttributeSyntax? _allowMulti;
            private AttributeSyntax? _doNotNeedSyncContext;
            private AttributeSyntax? _resourceType;

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
                else if(_resourceTypeRegex.IsMatch(attrName)) {
                    _resourceType = attr;
                }
            }

            private void AppendStart(StringBuilder sb)
            {
                sb.Append(
@"
        public static void Start()
        {");
                if(_allowMulti is null) {
                    sb.Append(
@"
            ProcessHelper.SingleLaunch(Launch);");
                }
                else {
                    sb.Append(
@"
            Launch();");
                }

                sb.Append(
@"
        }
");
            }

            private void AppendLaunch(StringBuilder sb, Compilation compilation)
            {
                Debug.Assert(_entryPoint is not null);
                var resourceTypeName = GetAttrArgTypeName(_resourceType!, 0, compilation);
                var resourceArg = GetAttrArgString(_resourceType!, 1, compilation);
                sb.Append(@"
        private static void Launch()
        {
            try {");
                sb.Append($@"
                Resources.Initialize(arg => new {resourceTypeName}(arg), ""{resourceArg}"");");
                sb.Append(@"
                var screen = CreateScreen();
                screen.Initialized += OnScreenInitialized;");

                if(_doNotNeedSyncContext is null) {
                    sb.Append(@"
                CustomSynchronizationContext.CreateIfNeeded(out _, out var syncContextReciever);
                screen.Show();
                while(Engine.HandleOnce()) {
                    syncContextReciever?.DoAll();
                }
            }
            finally {
                Resources.Close();
                CustomSynchronizationContext.Restore();
                Engine.Stop();
            }");
                }
                else {
                    sb.Append(@"
                screen.Show();
                while(Engine.HandleOnce()) { }
            }
            finally {
                Resources.Close();
                Engine.Stop();
            }");
                }

                sb.Append(@"
        }
");
            }

            private void AppendOnScreenInitialized(StringBuilder sb, Compilation compilation)
            {
                var entryTypeName = GetAttrArgTypeName(_entryPoint!, 0, compilation);
                var methodName = GetAttrArgString(_entryPoint!, 1, compilation);
                var awaiting = GetAttrArgBool(_entryPoint!, 2, compilation);

                sb.Append(@"
        private static async void OnScreenInitialized(IHostScreen screen)
        {
            Timing.Initialize(screen);
            Game.Initialize(screen);
            GameUI.Initialize(screen.UIRoot);
            try {");
                if(awaiting) {
                    sb.Append($@"
                await {entryTypeName}.{methodName}();");
                }
                else {
                    sb.Append($@"
                {entryTypeName}.{methodName}();");
                }
                sb.Append(@"
            }
            catch {
                // Don't throw. (Ignore exceptions in user code)
            }
        }
");
            }

            private void AppendScreenRun(StringBuilder sb, Compilation compilation)
            {
                var width = GetAttrArgInt(_screenSize!, 0, compilation);
                var height = GetAttrArgInt(_screenSize!, 1, compilation);
                var title = GetAttrArgString(_screenTitle!, 0, compilation);

                sb.Append(@"
        private static IHostScreen CreateScreen()
        {
            static IHostScreen PlatformSwitch(int width, int height, string title, ReadOnlySpan<RawImage> icon)
            {
                switch(Platform.PlatformType) {
                    case PlatformType.Windows:
                    case PlatformType.MacOSX:
                    case PlatformType.Linux:
                        return new Window(width, height, title, WindowStyle.Default, icon));
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
                    var iconName = GetAttrArgString(_screenIcon, 0, compilation);
                    sb.Append(
$@"
            ReadOnlySpan<RawImage> icon = stackalloc RawImage[1];
            using var iconStream = Resources.Loader.GetStream(""{iconName}"");
            using var bitmap = new Bitmap(iconStream);");
                    sb.Append(
@"
            using var pixels = iconBitmap.GetPixels(ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var pixelSpan = pixels.AsSpan();

            // Pixels of System.Drawing.Bitmap is layouted as (B, G, R, A).
            // Convert them as (R, G, B, A)
            for(int i = 0; i < pixelSpan.Length / 4; i++) {
                var (r, g, b) = (pixelSpan[i * 4 + 2], pixelSpan[i * 4 + 1], pixelSpan[i * 4]);
                pixelSpan[i * 4] = r;
                pixelSpan[i * 4 + 1] = g;
                pixelSpan[i * 4 + 2] = b;
            }
            icon[0] = new RawImage(pixels.Width, pixels.Height, pixels.Ptr);
");
                    sb.Append(
$@"
            return PlatformSwitch({width}, {height}, ""{title}"", icon)");
                }

                sb.Append(@"
        }
");
            }

            public SourceText DumpSource(Compilation compilation)
            {
                if(_entryPoint is null) {
                    return SourceText.From("", Encoding.UTF8);
                }

                var sb = new StringBuilder();
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
using Cysharp.Threading.Tasks;

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


                return SourceText.From(sb.ToString(), Encoding.UTF8);
            }

            private string GetAttrArgTypeName(AttributeSyntax attr, int argNum, Compilation compilation)
            {
                var expr = (TypeOfExpressionSyntax)attr.ArgumentList!.Arguments[argNum].Expression;

                return compilation.GetSemanticModel(attr.SyntaxTree)
                                  .GetSymbolInfo(expr.Type)
                                  .Symbol!.ToString();      // fullname
            }

            private static string GetAttrArgString(AttributeSyntax attr, int argNum, Compilation compilation)
            {
                return compilation.GetSemanticModel(attr.SyntaxTree)
                                  .GetConstantValue(attr.ArgumentList!.Arguments[argNum].Expression).Value!.ToString();
            }

            private static int GetAttrArgInt(AttributeSyntax attr, int argNum, Compilation compilation)
            {
                var value = compilation.GetSemanticModel(attr.SyntaxTree)
                                       .GetConstantValue(attr.ArgumentList!.Arguments[argNum].Expression).Value;
                return (int)value!;
            }

            private static bool GetAttrArgBool(AttributeSyntax attr, int argNum, Compilation compilation)
            {
                return (bool)compilation.GetSemanticModel(attr.SyntaxTree)
                                        .GetConstantValue(attr.ArgumentList!.Arguments[argNum].Expression).Value!;
            }
        }
    }
}
