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
            if (string.IsNullOrEmpty(options.SourceFilePath) || !File.Exists(options.SourceFilePath))
            {
                Console.WriteLine($"[Error] Source file not found: {options.SourceFilePath}");
                return false;
            }

            try
            {
                if (options.VerboseOutput)
                    Console.WriteLine($"[Pipeline] Reading source file: {options.SourceFilePath}");

                string tigerCode = File.ReadAllText(options.SourceFilePath);

                if (options.VerboseOutput)
                    Console.WriteLine("[Pipeline] Tokenizing source code...");

                var tokens = TokenizeSource(tigerCode);
                if (tokens == null || tokens.Count == 0)
                {
                    Console.WriteLine("[Error] Lexer failed to produce tokens.");
                    return false;
                }

                if (options.VerboseOutput)
                    Console.WriteLine("[Pipeline] Parsing tokens to AST...");

                var parser = new Parser(tokens);
                ExpNode ast = parser.Parse();

                if (ast == null)
                {
                    Console.WriteLine("[Error] Parsing failed to produce AST.");
                    return false;
                }

                if (options.VerboseOutput)
                    Console.WriteLine("[Pipeline] Generating C# Code...");

                string csharpCode = CodeGenerator.EmitCSharp(ast);

                if (options.VerboseOutput)
                    Console.WriteLine($"[Pipeline] Compiling to Assembly (Target: {options.TargetFramework})...");

                bool success = CodeGenerator.CompileToAssembly(csharpCode, options);

                if (!success)
                {
                    Console.WriteLine("[Error] Primary compilation failed.");
                    return false;
                }

                if (options.IsSingleFile)
                {
                    PublishSingleFile(options);
                }

                Console.WriteLine($"[Success] Compilation succeeded -> {options.OutputFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Compiler pipeline exception: {ex.Message}");
                if (options.VerboseOutput)
                    Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        private static List<Token>? TokenizeSource(string tigerCode)
        {
            Type? lexerType = typeof(CompilerPipeline).Assembly.GetType("Tiger.NET.Lexer")
                            ?? typeof(CompilerPipeline).Assembly.GetType("Lexer");

            if (lexerType == null) return null;

            var staticMethod = lexerType.GetMethod("Tokenize", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (staticMethod != null)
            {
                return staticMethod.Invoke(null, new object[] { tigerCode }) as List<Token>;
            }

            var ctor = lexerType.GetConstructor(new[] { typeof(string) });
            if (ctor != null)
            {
                object lexerInst = ctor.Invoke(new object[] { tigerCode });
                string[] possibleNames = { "Tokenize", "Scan", "GetTokens", "ParseTokens" };
                foreach (var name in possibleNames)
                {
                    var m = lexerType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (m != null)
                    {
                        return m.Invoke(lexerInst, null) as List<Token>;
                    }
                }
            }

            return null;
        }

        private static void PublishSingleFile(CompilerOptions options)
        {
            Console.WriteLine("[Publish] Generating SingleFile Executable via dotnet publish...");

            string assemblyName = Path.GetFileNameWithoutExtension(options.OutputFilePath);
            string projectDir = Path.GetDirectoryName(Path.GetFullPath(options.OutputFilePath)) ?? "";

            string selfContainedArg = options.IsSelfContained ? "--self-contained true" : "--self-contained false";
            string tfm = string.IsNullOrEmpty(options.TargetFramework) ? "net10.0" : options.TargetFramework;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish -c Release -r win-x64 -f {tfm} {selfContainedArg} -p:PublishSingleFile=true -o \"{projectDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        Console.WriteLine("[Success] Native SingleFile Executable Published.");
                    }
                    else
                    {
                        Console.WriteLine("[Warning] SingleFile publish returned non-zero exit code:");
                        Console.WriteLine(proc.StandardError.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to run dotnet publish: {ex.Message}");
            }
        }
    }
}