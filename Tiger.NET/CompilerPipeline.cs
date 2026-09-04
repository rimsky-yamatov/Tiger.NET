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
                Console.WriteLine($"[Pipeline] Reading source file: {options.SourceFilePath}");
                string tigerCode = File.ReadAllText(options.SourceFilePath);

                // 1. Lexer によるトークン化
                Console.WriteLine("[Pipeline] Tokenizing source code...");
                var tokens = TokenizeSource(tigerCode);
                if (tokens == null || tokens.Count == 0)
                {
                    Console.WriteLine("[Error] Lexer failed to produce tokens.");
                    return false;
                }

                // 2. Parser による構文解析 (AST生成)
                Console.WriteLine("[Pipeline] Parsing tokens to AST...");
                var parser = new Parser(tokens);
                ExpNode ast = parser.Parse();

                if (ast == null)
                {
                    Console.WriteLine("[Error] Parsing failed to produce AST.");
                    return false;
                }

                // 3. C# コード生成
                Console.WriteLine("[Pipeline] Generating C# Code...");
                string csharpCode = CodeGenerator.EmitCSharp(ast);

                // 4. Roslyn による C# から .NET アセンブリ (DLL / EXE) の出力
                Console.WriteLine($"[Pipeline] Compiling to Assembly (Target: {options.TargetFramework})...");
                bool success = CodeGenerator.CompileToAssembly(csharpCode, options);

                if (!success)
                {
                    Console.WriteLine("[Error] Primary compilation failed.");
                    return false;
                }

                // 5. SingleFile オプション指定時のパブリッシュ処理
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
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Lexer クラスの各種実装形態 (Lexer.Tokenize(code) / new Lexer(code).Tokenize() 等) に自動対応
        /// </summary>
        private static List<Token>? TokenizeSource(string tigerCode)
        {
            Type? lexerType = typeof(CompilerPipeline).Assembly.GetType("Tiger.NET.Lexer")
                            ?? typeof(CompilerPipeline).Assembly.GetType("Lexer");

            if (lexerType == null) return null;

            // 1. static メソッド Tokenize(string) の検索
            var staticMethod = lexerType.GetMethod("Tokenize", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (staticMethod != null)
            {
                return staticMethod.Invoke(null, new object[] { tigerCode }) as List<Token>;
            }

            // 2. インスタンス生成: new Lexer(string)
            var ctor = lexerType.GetConstructor(new[] { typeof(string) });
            if (ctor != null)
            {
                object lexerInst = ctor.Invoke(new object[] { tigerCode });

                // Tokenize() / Scan() / GetTokens() などのメソッドを探して実行
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