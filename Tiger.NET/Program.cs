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
                Console.WriteLine("Usage: Tiger.NET.exe <source.tig> [/O:<output.exe>] [/target:exe|winexe|dll] [/tfm:net9.0|net10.0]");
                return;
            }

            var options = new CompilerOptions();

            foreach (var arg in args)
            {
                if (arg.StartsWith("/O:") || arg.StartsWith("/o:"))
                {
                    options.OutputFilePath = arg.Substring(3);
                }
                else if (arg.StartsWith("/target:"))
                {
                    string target = arg.Substring(8).ToLower();
                    options.TargetType = target switch
                    {
                        "winexe" => OutputType.WindowsApplication,
                        "dll" => OutputType.Dll,
                        _ => OutputType.ConsoleApplication
                    };
                }
                else if (arg.StartsWith("/tfm:"))
                {
                    options.TargetFramework = arg.Substring(5).ToLower();
                }
                else if (arg.Equals("/singlefile", StringComparison.OrdinalIgnoreCase))
                {
                    options.SingleFile = true;
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

            try
            {
                string tigerCode = File.ReadAllText(options.SourceFilePath);

                // 1. 構文解析（抽象構文木の生成）
                ExpNode ast = Parser.Parse(tigerCode);

                // 2. C# コードの生成
                string csharpCode = CodeGenerator.EmitCSharp(ast);

                // 3. Roslyn による直接コンパイルと runtimeconfig.json の出力
                bool success = CodeGenerator.CompileToAssembly(csharpCode, options);

                if (success)
                {
                    Console.WriteLine($"[Success] Compilation finished -> {options.OutputFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Compilation failed: {ex.Message}");
            }
        }
    }
}