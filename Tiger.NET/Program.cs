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
                return;
            }

            var options = new CompilerOptions();

            foreach (var arg in args)
            {
                if (arg.StartsWith("/O:") || arg.StartsWith("/o:"))
                {
                    options.OutputFilePath = arg.Substring(3);
                }
                else if (arg.StartsWith("/tfm:"))
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

            // CompilerPipeline に処理を委譲する場合は Pipeline を呼び出す
            CompilerPipeline.Run(options);
        }
    }
}