using System;

namespace Tiger.NET
{
    public enum OutputType { ConsoleExe, WindowsExe, Dll }

    public class CompilerOptions
    {
        public string OutputFilePath { get; set; } = "a.exe";
        public OutputType TargetType { get; set; } = OutputType.ConsoleExe;
        public bool IsSingleFile { get; set; } = false;
        public bool IsSelfContained { get; set; } = false;
        public string SourceFilePath { get; set; } = "";
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var options = ParseArguments(args);
            if (string.IsNullOrEmpty(options.SourceFilePath))
            {
                Console.WriteLine("[Error] Source file missing.");
                return;
            }

            var pipeline = new CompilerPipeline(options);
            pipeline.Execute();
        }

        static CompilerOptions ParseArguments(string[] args)
        {
            var options = new CompilerOptions();
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("/O:", StringComparison.OrdinalIgnoreCase))
                {
                    options.OutputFilePath = arg.Substring(3);
                }
                else if (arg.Equals("/target:dll", StringComparison.OrdinalIgnoreCase))
                {
                    options.TargetType = OutputType.Dll;
                }
                else if (arg.Equals("/target:winexe", StringComparison.OrdinalIgnoreCase))
                {
                    options.TargetType = OutputType.WindowsExe;
                }
                else if (arg.Equals("/singlefile", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSingleFile = true;
                }
                else if (arg.Equals("/standalone", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSelfContained = true;
                }
                else
                {
                    options.SourceFilePath = arg;
                }
            }
            return options;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Tiger.NET Compiler for .NET Environment");
            Console.WriteLine("Usage: Tiger.NET <source.tig> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  /O:<file>        Output file path (e.g. /O:hello.exe)");
            Console.WriteLine("  /target:dll      Output as Dynamic Link Library (.dll)");
            Console.WriteLine("  /target:winexe   Output as Windows Application");
            Console.WriteLine("  /singlefile      Package as Single-file executable");
            Console.WriteLine("  /standalone      Self-contained deployment (No .NET Runtime required)");
        }
    }
}