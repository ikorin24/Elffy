using System;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace ElffyResource
{
    class Program
    {
        public static void Main(string[] args)
        {
            // -------- for test ---------
            //Test.Build();
            //Test.Decompress();
            //Console.WriteLine("Complete !!");
            // ---------------------------

            var sw = new Stopwatch();
            sw.Start();
            var dir = @"..\..\Resources";
            var output = "Resources.dat";
            var password = "password";
            ResourceManager.Compile(dir, output, password);
            sw.Stop();
            Debug.WriteLine($"{sw.ElapsedMilliseconds}ms");

            var sw2 = new Stopwatch();
            sw2.Start();
            var inputPath = "Resources.dat";
            var password2 = "password";
            var outputDir = "Resources";
            var result = ResourceManager.Decompile(inputPath, outputDir, password2);
            sw2.Stop();
            Debug.WriteLine($"{sw2.ElapsedMilliseconds}ms");
            Console.WriteLine(result);

            Console.WriteLine("Press Any Key...");
            Console.ReadKey();
        }
    }
}
