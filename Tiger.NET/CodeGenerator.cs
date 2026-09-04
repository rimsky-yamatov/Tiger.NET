using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Basic.Reference.Assemblies;

namespace Tiger.NET
{
    public class CodeGenerator
    {
        public static string EmitCSharp(ExpNode ast)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("namespace Tiger.NET.Runtime {");
            sb.AppendLine("    public static class ExecutableProgram {");
            sb.AppendLine("        public static void Main(string[] args) {");
            sb.AppendLine("            TigerStdLib.Init();");

            EmitNode(ast, sb, "            ");

            sb.AppendLine("        }");
            sb.AppendLine("    }");

            // Tiger Runtime Standard Library
            sb.AppendLine("    public static class TigerStdLib {");
            sb.AppendLine("        public static void Init() {}");
            sb.AppendLine("        public static void print(string s) => Console.Write(s);");
            sb.AppendLine("        public static void printline(string s) => Console.WriteLine(s);");
            sb.AppendLine("        public static void printint(int i) => Console.Write(i);");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void EmitNode(ExpNode node, StringBuilder sb, string indent)
        {
            if (node is LetExpNode letNode)
            {
                foreach (var dec in letNode.Decs)
                {
                    if (dec is VarDeclNode v)
                    {
                        sb.Append($"{indent}var {v.Name} = ");
                        EmitExprInline(v.Init, sb);
                        sb.AppendLine(";");
                    }
                }
                foreach (var b in letNode.Body)
                {
                    sb.Append(indent);
                    EmitExprInline(b, sb);
                    sb.AppendLine(";");
                }
            }
            else
            {
                sb.Append(indent);
                EmitExprInline(node, sb);
                sb.AppendLine(";");
            }
        }

        private static void EmitExprInline(ExpNode node, StringBuilder sb)
        {
            if (node is StringLiteralNode s) sb.Append($"\"{s.Value.Replace("\"", "\\\"")}\"");
            else if (node is IntLiteralNode i) sb.Append(i.Value);
            else if (node is VarAccessNode v) sb.Append(v.Name);
            else if (node is BinaryExpNode b)
            {
                sb.Append("(");
                EmitExprInline(b.Left, sb);
                sb.Append($" {b.Op} ");
                EmitExprInline(b.Right, sb);
                sb.Append(")");
            }
            else if (node is CallExpNode c)
            {
                sb.Append($"TigerStdLib.{c.FuncName}(");
                for (int i = 0; i < c.Args.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    EmitExprInline(c.Args[i], sb);
                }
                sb.Append(")");
            }
        }

        public static bool CompileToAssembly(string csharpCode, CompilerOptions options)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
            string assemblyName = Path.GetFileNameWithoutExtension(options.OutputFilePath);

            OutputKind outputKind = options.TargetType switch
            {
                OutputType.Dll => OutputKind.DynamicallyLinkedLibrary,
                OutputType.WindowsExe => OutputKind.WindowsApplication,
                _ => OutputKind.ConsoleApplication
            };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: Net90.References.All,
                options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release)
            );

            using var stream = File.Create(options.OutputFilePath);
            var result = compilation.Emit(stream);

            if (!result.Success)
            {
                Console.WriteLine("[Error] Compilation Failed:");
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                        Console.WriteLine($"  {diagnostic.Id}: {diagnostic.GetMessage()}");
                }
                return false;
            }

            Console.WriteLine($"[Success] Primary Assembly Generated: {options.OutputFilePath}");
            return true;
        }
    }
}