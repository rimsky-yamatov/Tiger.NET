using System;
using System.IO;

namespace Tiger.NET
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 1;
            }

            var options =
                new CompilerOptions();

            foreach (var arg in args)
            {
                if (arg.StartsWith(
                    "/o:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    options.OutputFilePath =
                        arg.Substring(3);

                    continue;
                }

                if (arg.StartsWith(
                    "/tfm:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    options.TargetFramework =
                        arg.Substring(5)
                            .ToLowerInvariant();

                    continue;
                }

                if (arg.StartsWith(
                    "/target:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string target =
                        arg.Substring(8)
                            .ToLowerInvariant();

                    options.TargetType =
                        target switch
                        {
                            "dll" =>
                                OutputType.Dll,

                            "win" or "winexe" =>
                                OutputType.WindowsApplication,

                            _ =>
                                OutputType.ConsoleApplication
                        };

                    continue;
                }

                if (arg.StartsWith(
                    "/optimize:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string level =
                        arg.Substring(10);

                    options.OptimizationLevel =
                        level switch
                        {
                            "0" =>
                                OptimizationLevelKind.None,

                            "debug" =>
                                OptimizationLevelKind.Debug,

                            _ =>
                                OptimizationLevelKind.Release
                        };

                    continue;
                }

                if (arg.Equals(
                    "/debug",
                    StringComparison.OrdinalIgnoreCase))
                {
                    options.IncludeDebugInfo = true;
                    options.OptimizationLevel =
                        OptimizationLevelKind.Debug;

                    continue;
                }

                if (arg.Equals(
                    "/singlefile",
                    StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSingleFile = true;
                    continue;
                }

                if (arg.Equals(
                    "/selfcontained",
                    StringComparison.OrdinalIgnoreCase))
                {
                    options.IsSelfContained = true;
                    continue;
                }

                if (arg.Equals(
                        "/verbose",
                        StringComparison.OrdinalIgnoreCase))
                {
                    options.VerboseOutput = true;
                    continue;
                }

                if (arg.Equals("/?") ||
                    arg.Equals(
                        "/help",
                        StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    return 0;
                }

                if (!arg.StartsWith("/"))
                {
                    options.SourceFilePath = arg;
                    continue;
                }

                Console.WriteLine(
                    $"[Error] Unknown option: {arg}");

                return 1;
            }

            if (string.IsNullOrEmpty(
                    options.SourceFilePath))
            {
                Console.WriteLine(
                    "[Error] No source file specified.");

                return 1;
            }

            if (!File.Exists(
                    options.SourceFilePath))
            {
                Console.WriteLine(
                    $"[Error] Source file not found: " +
                    options.SourceFilePath);

                return 1;
            }

            return CompilerPipeline.Run(
                options)
                ? 0
                : 1;
        }

        private static void PrintHelp()
        {
            Console.WriteLine(
                "Tiger.NET Compiler");

            Console.WriteLine(
                "Version: 1.0.0");

            Console.WriteLine(
                "Usage: Tiger.NET <source.tig> [options]");

            Console.WriteLine(
                "Options:");

            Console.WriteLine(
                "    /o:<file>");

            Console.WriteLine(
                "    /tfm:<framework>");

            Console.WriteLine(
                "    /target:exe|win|dll");

            Console.WriteLine(
                "    /optimize:0|1|2");

            Console.WriteLine(
                "    /debug");

            Console.WriteLine(
                "    /singlefile");

            Console.WriteLine(
                "    /selfcontained");

            Console.WriteLine(
                "    /verbose");

            Console.WriteLine(
                "    /help");
        }
    }
}