using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SceneGenerator
{
    class Program
    {
        internal static void Main(string[] args)
        {
            var argParser = new CommandLineArgParser();
            var cmdArg = argParser.Parse(args);
            var sceneDir = new DirectoryInfo(cmdArg.OptionalArgs["-s"]);
            var outputDir = new DirectoryInfo(cmdArg.OptionalArgs["-o"]);
            CodeGenerator.GenerateAll(sceneDir, outputDir);
        }
    }
}
