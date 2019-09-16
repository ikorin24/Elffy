using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DefinitionGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = new DirectoryInfo(".");
            var output = new DirectoryInfo(@".elffy\auto-generated\");
            CodeGenerator.GenerateAll(dir, output);
        }
    }
}
