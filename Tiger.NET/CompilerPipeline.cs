using System;
using System.Collections.Generic;
using System.IO;

namespace Tiger.NET
{
    public static class CompilerPipeline
    {
        public static bool Run(
            CompilerOptions options)
        {
            if (string.IsNullOrEmpty(
                    options.SourceFilePath) ||
                !File.Exists(
                    options.SourceFilePath))
            {
                Console.WriteLine(
                    $"[Error] Source file not found: " +
                    options.SourceFilePath);

                return false;
            }

            try
            {
                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Reading source file...");

                string source =
                    File.ReadAllText(
                        options.SourceFilePath);

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Lexing...");

                var lexer =
                    new Lexer(source);

                List<Token> tokens =
                    lexer.Tokenize();

                if (options.VerboseOutput)
                    Console.WriteLine(
                        $"[Pipeline] Tokens: {tokens.Count}");

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Parsing...");

                var parser =
                    new Parser(tokens);

                ExpNode ast =
                    parser.Parse();

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Type checking...");

                var checker =
                    new TypeChecker();

                checker.Check(ast);

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Generating C#...");

                string csharp =
                    CodeGenerator.EmitCSharp(
                        ast);

                if (options.VerboseOutput)
                {
                    Console.WriteLine(
                        "[Pipeline] Generated C#:");

                    Console.WriteLine(
                        csharp);
                }

                if (options.VerboseOutput)
                    Console.WriteLine(
                        "[Pipeline] Compiling...");

                bool success =
                    CodeGenerator.CompileToAssembly(
                        csharp,
                        options);

                if (!success)
                {
                    Console.WriteLine(
                        "[Error] Compilation failed.");

                    return false;
                }

                Console.WriteLine(
                    $"[Success] Compilation succeeded -> " +
                    $"{options.OutputFilePath}");

                return true;
            }
            catch (TypeCheckException ex)
            {
                Console.WriteLine(
                    $"[Type Error] {ex.Message}");

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Error] {ex.Message}");

                if (options.VerboseOutput)
                    Console.WriteLine(
                        ex.StackTrace);

                return false;
            }
        }
    }
}