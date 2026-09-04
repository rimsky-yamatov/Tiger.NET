using System;
using System.IO;
using System.Reflection;
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

                ExpNode? ast = InvokeParser(tigerCode, options.SourceFilePath);

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

        private static ExpNode? InvokeParser(string tigerCode, string sourceFilePath)
        {
            Type? parserType = typeof(CompilerPipeline).Assembly.GetType("Tiger.NET.Parser")
                            ?? typeof(CompilerPipeline).Assembly.GetType("Parser");

            if (parserType == null)
            {
                Console.WriteLine("[Error] Parser type not found in assembly.");
                return null;
            }

            // Parser 内の Parse メソッドをすべて検索
            var methods = parserType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            foreach (var m in methods)
            {
                if (m.Name != "Parse") continue;

                var parameters = m.GetParameters();

                try
                {
                    // 1. 引数なしの Parse()
                    if (parameters.Length == 0 && !m.IsStatic)
                    {
                        // インスタンス作成を試行 (Lexer 等を探す)
                        object? parserInstance = CreateParserInstance(parserType, tigerCode);
                        if (parserInstance != null)
                        {
                            return m.Invoke(parserInstance, null) as ExpNode;
                        }
                    }
                    // 2. 引数1つの Parse(arg)
                    else if (parameters.Length == 1)
                    {
                        Type pType = parameters[0].ParameterType;

                        // Parse(string)
                        if (pType == typeof(string))
                        {
                            object? target = m.IsStatic ? null : CreateParserInstance(parserType, tigerCode);
                            return m.Invoke(target, new object[] { tigerCode }) as ExpNode;
                        }

                        // Parse(Lexer) や Parse(List<Token>)
                        object? argObj = CreateLexerOrTokenList(pType, tigerCode);
                        if (argObj != null)
                        {
                            object? target = m.IsStatic ? null : CreateParserInstance(parserType, tigerCode);
                            return m.Invoke(target, new object[] { argObj }) as ExpNode;
                        }
                    }
                    // 3. 引数2つの Parse(arg1, arg2)
                    else if (parameters.Length == 2)
                    {
                        object? target = m.IsStatic ? null : CreateParserInstance(parserType, tigerCode);
                        return m.Invoke(target, new object[] { tigerCode, sourceFilePath }) as ExpNode;
                    }
                }
                catch
                {
                    // 呼び出し失敗時は次のオーバーロードを試行
                }
            }

            Console.WriteLine("[Error] Could not find a matching Parse method overload.");
            return null;
        }

        private static object? CreateParserInstance(Type parserType, string tigerCode)
        {
            var ctors = parserType.GetConstructors();
            foreach (var ctor in ctors)
            {
                var paramsInfo = ctor.GetParameters();
                if (paramsInfo.Length == 1)
                {
                    if (paramsInfo[0].ParameterType == typeof(string))
                        return ctor.Invoke(new object[] { tigerCode });

                    object? argObj = CreateLexerOrTokenList(paramsInfo[0].ParameterType, tigerCode);
                    if (argObj != null)
                        return ctor.Invoke(new object[] { argObj });
                }
            }
            return Activator.CreateInstance(parserType);
        }

        private static object? CreateLexerOrTokenList(Type targetType, string tigerCode)
        {
            try
            {
                // Lexer(string) のインスタンス作成
                var lexerCtor = targetType.GetConstructor(new[] { typeof(string) });
                if (lexerCtor != null)
                {
                    return lexerCtor.Invoke(new object[] { tigerCode });
                }

                // Lexer クラスが別で存在する場合
                Type? lexerType = typeof(CompilerPipeline).Assembly.GetType("Tiger.NET.Lexer")
                               ?? typeof(CompilerPipeline).Assembly.GetType("Lexer");

                if (lexerType != null && targetType.IsAssignableFrom(lexerType))
                {
                    return Activator.CreateInstance(lexerType, tigerCode);
                }
            }
            catch { }

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