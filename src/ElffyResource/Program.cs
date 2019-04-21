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
            var sw = new Stopwatch();
            sw.Start();
            var dir = @"..\..\Resources";
            var output = "Resources.dat";
            var password = "ikorin24";
            ResourceManager.Build(dir, output, password);
            sw.Stop();
            Debug.WriteLine($"{sw.ElapsedMilliseconds}ms");
        }
    }
}
