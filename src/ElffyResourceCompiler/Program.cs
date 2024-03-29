﻿using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;

namespace ElffyResourceCompiler
{
    class Program
    {
        private static readonly string[] VALID_OPTION = new[] { "-o", "-h" };
        private const string DefaultOutput = "Resources.dat";

        public static int Main(string[] args)
        {
            // Arguments
            // "-o output-path resource-dir"
            var param = CommandLineArgParser.Parse(args);
            if(param.OptionalArgs.ContainsKey("-h")) {
                ShowHelp();
                return 0;
            }
            var invalidOption = param.OptionalArgs.Keys.Where(option => !VALID_OPTION.Contains(option)).FirstOrDefault();
            if(invalidOption != null) {
                Console.WriteLine($"Unknown option '{invalidOption}'");
                ShowHelp();
                return -1;
            }
            if(param.Args.Count < 1) {
                Console.WriteLine("Argument not sufficient");
                ShowHelp();
                return -1;
            }
            var resourceDir = param.Args[0];

            if(!param.OptionalArgs.TryGetValue("-o", out var output)) {
                output = Path.Combine(Assembly.GetEntryAssembly()!.Location, DefaultOutput);
            }

            var sw = new Stopwatch();
            sw.Start();
            Compiler.Compile(resourceDir, output);
            sw.Stop();
            Console.WriteLine($"Resouce compiled : {sw.ElapsedMilliseconds}ms");
            return 0;
        }

        private static void ShowHelp()
        {
            var exe = Path.GetFileName(Assembly.GetEntryAssembly()!.Location);
            Console.WriteLine($"usage : {exe} [-h] [-o output-path] resource-dir");
        }
    }
}
