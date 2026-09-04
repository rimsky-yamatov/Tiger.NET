using System;
using System.IO;
using System.Diagnostics;

namespace Tiger.NET
{
    public static class CompilerPipeline
    {
        /// <summary>
        /// Tiger.NET コンパイルパイプラインのエントリーポイント
        /// </summary>
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

                // 1. 構文解析（AST生成）
                Console.WriteLine("[Pipeline] Parsing source code...");

                ExpNode ast = null;

                // --- Parser.Parse の呼び出し処理 ---
                // プロジェクトの Lexer / Parser 実装に合わせて自動対応します。
                // 1) Lexer クラスが存在する場合:
                // var lexer = new Lexer(tigerCode);
                // ast = Parser.Parse(lexer);

                // 2) Parse(sourceText, fileName) のオーバーロードが存在する場合:
                // ast = Parser.Parse(tigerCode, options.SourceFilePath);

                // 3) Parse(Lexer) や Parse(string, string) が見つからない場合の基本呼び出し:
                try
                {
                    // 最初に Lexer を使った呼び出しを試行（リフレクションによる互換性吸収）
                    var lexerType = Type.GetType("Tiger.NET.Lexer") ?? Type.GetType("Lexer");
                    if (lexerType != null)
                    {
                        var lexerInstance = Activator.CreateInstance(lexerType, tigerCode);
                        var parseMethod = typeof(Parser).GetMethod("Parse", new[] { lexerType });
                        if (parseMethod != null)
                        {
                            ast = parseMethod.Invoke(null, new[] { lexerInstance }) as ExpNode;
                        }
                    }

                    // 上記で見つからない場合、(string, string) 引数の Parse を試行
                    if (ast == null)
                    {
                        var parseMethodTwoArgs = typeof(Parser).GetMethod("Parse", new[] { typeof(string), typeof(string) });
                        if (parseMethodTwoArgs != null)
                        {
                            ast = parseMethodTwoArgs.Invoke(null, new object[] { tigerCode, options.SourceFilePath }) as ExpNode;
                        }
                    }

                    // 依然としてヌルの場合、通常呼び出しを試行
                    if (ast == null)
                    {
                        // 既存の Parser.Parse 呼び出し
                        ast = Parser.Parse(tigerCode, options.SourceFilePath);
                    }
                }
                catch
                {
                    // フォールバック: 直接 2 引数呼び出し
                    ast = Parser.Parse(tigerCode, options.SourceFilePath);
                }

                if (ast == null)
                {
                    Console.WriteLine("[Error] Parsing failed to produce AST.");
                    return false;
                }

                // 2. C# コード生成
                Console.WriteLine("[Pipeline] Generating C# Code...");
                string csharpCode = CodeGenerator.EmitCSharp(ast);

                // 3. Roslyn による C# から .NET アセンブリ (DLL / EXE) の出力
                Console.WriteLine($"[Pipeline] Compiling to Assembly (Target: {options.TargetFramework})...");
                bool success = CodeGenerator.CompileToAssembly(csharpCode, options);

                if (!success)
                {
                    Console.WriteLine("[Error] Primary compilation failed.");
                    return false;
                }

                // 4. SingleFile オプション指定時のパブリッシュ処理
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