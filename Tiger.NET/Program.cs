using System;
using System.IO;

namespace Tiger.NET
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Tiger.NET Compiler");
                Console.WriteLine("Usage: Tiger.NET <source.tig> [/O:<output.exe>] [/tfw:<tfw>] [/singlefile]");
                return;
            }

            var options = new CompilerOptions();

            foreach (var arg in args)
            {
                if (arg.StartsWith("/O:", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("/o:", StringComparison.OrdinalIgnoreCase))
                {
                    options.OutputFilePath = arg.Substring(3);
                }
                // /tfw: オプションの解析
                else if (arg.StartsWith("/tfw:", StringComparison.OrdinalIgnoreCase))
                {
                    options.TargetFramework = arg.Substring(5).ToLower();
                }
                else if (arg.Equals("/singlefile", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSingleFile = true;
                }
                else if (!arg.StartsWith("/"))
                {
                    options.SourceFilePath = arg;
                }
            }

            if (string.IsNullOrEmpty(options.SourceFilePath) || !File.Exists(options.SourceFilePath))
            {
                Console.WriteLine($"[Error] Source file not found: {options.SourceFilePath}");
                return;
            }

            CompilerPipeline.Run(options);
        }
    }
}