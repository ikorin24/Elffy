using System;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace ElffyResourceCompiler
{
    class Program
    {
        public const string OutputFile = "Resources.dat";

        public static int Main(string[] args)
        {
            if(args.Length < 1) {
                Console.WriteLine("Please set resource directory as arg[0].");
                Console.WriteLine("[ERROR] Resource is not compiled !!");
                return -1;
            }

            if(args.Length < 2) {
                Console.WriteLine("Please set output directory as arg[1].");
                Console.WriteLine("[ERROR] Resource is not compiled !!");
                return -1;
            }

            //if(args.Length < 3) {
            //    Console.WriteLine("Please set password which compile resources as arg[2].");
            //    Console.WriteLine("[ERROR] Resource is not compiled !!");
            //    return -1;
            //}

            var resourceDir = args[0];
            var output = Path.Combine(args[1], OutputFile);
            //var password = args[2];

            Console.WriteLine($"resourceDir : {resourceDir}");
            Console.WriteLine($"output : {output}");
            //Console.WriteLine($"password : {password}");

            var sw = new Stopwatch();
            sw.Start();
            Compiler.Compile(resourceDir, output);
            sw.Stop();
            Console.WriteLine($"Resouce compiled : {sw.ElapsedMilliseconds}ms");
            return 0;
        }
    }
}
