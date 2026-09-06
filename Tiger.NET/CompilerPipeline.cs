using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Tiger.NET
{
    public static class CompilerPipeline
    {
        public static bool Run(CompilerOptions options)
        {
            if (string.IsNullOrEmpty(options.SourceFilePath) ||
                !File.Exists(options.SourceFilePath))
            {
                Console.WriteLine(
                    $"[Error] Source file not found: {options.SourceFilePath}");

                return false;
            }

            try
            {
                if (options.VerboseOutput)
                    Console.WriteLine(
                        $"[Pipeline] Reading source file: {options.SourceFilePath}");

                string tigerCode =
                    File.ReadAllText(
                        options.SourceFilePath);

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Tokenizing source code...");

                List<Token>? tokens =
                    TokenizeSource(tigerCode);

                if (tokens == null ||
                    tokens.Count == 0)
                {
                    Console.WriteLine(
                        "[Error] Lexer failed to produce tokens.");

                    return false;
                }

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Parsing tokens to AST...");

                var parser =
                    new Parser(tokens);

                ExpNode ast =
                    parser.Parse();

                if (ast == null)
                {
                    Console.WriteLine(
                        "[Error] Parser failed.");

                    return false;
                }

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Type checking...");

                var checker =
                    new TypeChecker();

                checker.Check(ast);

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Type checking succeeded.");

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Generating C# code...");

                string csharpCode =
                    CodeGenerator.EmitCSharp(ast);

                if (options.VerboseOutput)
                    Console.WriteLine(
                        $"[Pipeline] Compiling to Assembly (Target: {options.TargetFramework})...");

                bool success =
                    CodeGenerator.CompileToAssembly(
                        csharpCode,
                        options);

                if (!success)
                {
                    Console.WriteLine(
                        "[Error] Primary compilation failed.");

                    return false;
                }

                if (options.IsSingleFile)
                    PublishSingleFile(options);

                Console.WriteLine(
                    $"[Success] Compilation succeeded -> {options.OutputFilePath}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Error] Compiler pipeline exception: {ex.Message}");

                if (options.VerboseOutput)
                    Console.WriteLine(
                        ex.StackTrace);

                return false;
            }
        }

        private static List<Token>? TokenizeSource(
            string source)
        {
            var lexer =
                new Lexer(source);

            return lexer.Tokenize();
        }

        private static void PublishSingleFile(
            CompilerOptions options)
        {
            Console.WriteLine(
                "[Publish] Generating SingleFile Executable...");

            string outputPath =
                Path.GetFullPath(
                    options.OutputFilePath);

            string outputDir =
                Path.GetDirectoryName(
                    outputPath)
                ?? Directory.GetCurrentDirectory();

            string tfm =
                string.IsNullOrEmpty(
                    options.TargetFramework)
                    ? "net10.0"
                    : options.TargetFramework;

            string selfContained =
                options.IsSelfContained
                    ? "true"
                    : "false";

            var psi =
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments =
                        $"publish -c Release " +
                        $"-r win-x64 " +
                        $"-f {tfm} " +
                        $"--self-contained {selfContained} " +
                        $"-p:PublishSingleFile=true " +
                        $"-o \"{outputDir}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

            try
            {
                using Process? process =
                    Process.Start(psi);

                if (process == null)
                    return;

                process.WaitForExit();

                string stdout =
                    process.StandardOutput.ReadToEnd();

                string stderr =
                    process.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(stdout))
                    Console.WriteLine(stdout);

                if (process.ExitCode == 0)
                {
                    Console.WriteLine(
                        "[Success] Single-file publish succeeded.");
                }
                else
                {
                    Console.WriteLine(
                        "[Error] Single-file publish failed.");

                    if (!string.IsNullOrWhiteSpace(stderr))
                        Console.WriteLine(stderr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Warning] Failed to run dotnet publish: {ex.Message}");
            }
        }
    }
}
