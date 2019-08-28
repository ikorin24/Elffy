using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;

namespace ElffyResourceCompiler
{
    class Program
    {
        private static readonly string[] VALID_OPTION = new[] { "-r", "-s", "-h" };
        internal const string OutputFile = "Resources.dat";

        public static int Main(string[] args)
        {
            // Arguments
            // "-r <resource-dir> -s <scene-dir> <output-dir>"
            var parser = new CommandLineArgParser();
            var param = parser.Parse(args);
            if(param.OptionalArgs.ContainsKey("-h")) {
                ShowHelp();
                return 0;
            }
            var invalidOption = param.OptionalArgs.Keys.Where(option => !VALID_OPTION.Contains(option)).FirstOrDefault();
            if(invalidOption != null) {
                Console.WriteLine($"Known option '{invalidOption}'");
                ShowHelp();
                return -1;
            }
            if(param.Args.Count < 1) {
                Console.WriteLine("Argument not sufficient");
                ShowHelp();
                return -1;
            }
            param.OptionalArgs.TryGetValue("-r", out var resourceDir);
            param.OptionalArgs.TryGetValue("-s", out var sceneDir);
            var output = Path.Combine(param.Args[0], OutputFile);
            var sw = new Stopwatch();
            sw.Start();
            var setting = new CompileSetting() {
                ResourceDir = resourceDir,
                SceneDir = sceneDir,
                OutputPath = output,
            };
            Compiler.Compile(setting);
            sw.Stop();
            Console.WriteLine($"Resouce compiled : {sw.ElapsedMilliseconds}ms");
            return 0;
        }

        private static void ShowHelp()
        {
            var exe = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            Console.WriteLine($"usage : {exe} [-h] [-r <resource-dir>] [-s <scene-dir>] output-dir");
        }
    }
}
