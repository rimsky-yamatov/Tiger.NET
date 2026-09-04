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
                PrintHelp();
                return;
            }

            var options = new CompilerOptions();

            foreach (var arg in args)
            {
                // /o: 出力ファイル指定
                if (arg.StartsWith("/o:", StringComparison.OrdinalIgnoreCase))
                {
                    options.OutputFilePath = arg.Substring(3);
                }
                // /tfw: ターゲットフレームワーク指定
                else if (arg.StartsWith("/tfw:", StringComparison.OrdinalIgnoreCase))
                {
                    options.TargetFramework = arg.Substring(5).ToLower();
                }
                // /target: 出力ターゲット形式指定 (exe, win, dll)
                else if (arg.StartsWith("/target:", StringComparison.OrdinalIgnoreCase))
                {
                    string target = arg.Substring(8).ToLower();
                    options.TargetType = target switch
                    {
                        "dll" => OutputType.Dll,
                        "win" or "winexe" => OutputType.WindowsApplication,
                        _ => OutputType.ConsoleApplication
                    };
                }
                // /optimize: 最適化レベル指定 (0, 1, 2)
                else if (arg.StartsWith("/optimize:", StringComparison.OrdinalIgnoreCase))
                {
                    string level = arg.Substring(10);
                    options.OptimizationLevel = level switch
                    {
                        "0" => OptimizationLevelKind.None,
                        "1" or "2" => OptimizationLevelKind.Release,
                        _ => OptimizationLevelKind.Release
                    };
                }
                // /singlefile 単一ファイル出力
                else if (arg.Equals("/singlefile", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSingleFile = true;
                }
                // /selfcontained ランタイム同梱
                else if (arg.Equals("/selfcontained", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSelfContained = true;
                }
                // /debug デバッグ情報出力
                else if (arg.Equals("/debug", StringComparison.OrdinalIgnoreCase))
                {
                    options.IncludeDebugInfo = true;
                    options.OptimizationLevel = OptimizationLevelKind.Debug;
                }
                // /verbose 詳細ログ出力
                else if (arg.Equals("/verbose", StringComparison.OrdinalIgnoreCase))
                {
                    options.VerboseOutput = true;
                }
                // ヘルプ表示
                else if (arg.Equals("/?") || arg.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    return;
                }
                // ソースファイルパス
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

        private static void PrintHelp()
        {
            Console.WriteLine("Tiger.NET Compiler");
            Console.WriteLine("Usage: Tiger.NET <source.tig> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  /o:<file>             Specify output executable/dll path");
            Console.WriteLine("  /tfw:<framework>      Specify target framework (e.g. net9.0, net10.0)");
            Console.WriteLine("  /target:<type>        Target build type: exe, win, dll");
            Console.WriteLine("  /optimize:<level>     Optimization level: 0, 1, 2");
            Console.WriteLine("  /singlefile           Bundle output into a single file");
            Console.WriteLine("  /selfcontained        Package runtime inside the output binary");
            Console.WriteLine("  /debug                Generate debug build");
            Console.WriteLine("  /verbose              Show verbose compiler pipeline logs");
        }
    }
}