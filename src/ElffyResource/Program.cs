using System;

namespace ElffyResource
{
    class MainClass
    {
        const string HELP = 
@"How to use:
    -b | build a resource file
    -d | decompres a resource file to current directory
    -h | show help
";

        public static void Main(string[] args)
        {
            if(args.Length > 1) {
                switch(args[0]) {
                    case "-b": {
                        var manager = new ResourceManager();
                        return;
                    }
                    case "-d": {
                        var manager = new ResourceManager();
                        return;
                    }
                    case "-h": {
                        Console.WriteLine(HELP);
                        return;
                    }
                    default: {
                        break;
                    }
                }
            }
            Console.WriteLine($"Invalid Usege.{Environment.NewLine}{HELP}");
        }
    }
}
