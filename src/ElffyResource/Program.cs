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
            Test.Build();
            Test.Decompress();
            Console.WriteLine("Complete !!");
            // ---------------------------


            //var sw = new Stopwatch();
            //sw.Start();
            //var dir = @"..\..\Resources";
            //var output = "Resources.dat";
            //var password = "ikorin24";
            //ResourceManager.Build(dir, output, password);
            //sw.Stop();
            //Debug.WriteLine($"{sw.ElapsedMilliseconds}ms");

            //var sw = new Stopwatch();
            //sw.Start();
            //var inputPath = "Resources.dat";
            //var password = "ikorin24";
            //var outputDir = "Resources";
            //var result = ResourceManager.Decompress(inputPath, outputDir, password);
            //sw.Stop();
            //Debug.WriteLine($"{sw.ElapsedMilliseconds}ms");
            //Console.WriteLine(result);

            Console.WriteLine("Press Any Key...");
            Console.ReadKey();
        }
    }
}
