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

            // 全標準ライブラリの実装
            sb.AppendLine("    public static class TigerStdLib {");
            sb.AppendLine("        public static void Init() {}");
            sb.AppendLine("        public static void print(object s) => Console.Write(s);");
            sb.AppendLine("        public static void printline(object s) => Console.WriteLine(s);");
            sb.AppendLine("        public static void printint(int i) => Console.Write(i);");
            sb.AppendLine("        public static void flush() => Console.Out.Flush();");
            sb.AppendLine("        public static string getchar() => Console.Read() == -1 ? \"\" : ((char)Console.Read()).ToString();");
            sb.AppendLine("        public static int ord(string s) => string.IsNullOrEmpty(s) ? -1 : (int)s[0];");
            sb.AppendLine("        public static string chr(int i) => ((char)i).ToString();");
            sb.AppendLine("        public static int size(string s) => s?.Length ?? 0;");
            sb.AppendLine("        public static string substring(string s, int first, int n) => s.Substring(first, n);");
            sb.AppendLine("        public static string concat(string s1, string s2) => string.Concat(s1, s2);");
            sb.AppendLine("        public static int not(int i) => i == 0 ? 1 : 0;");
            sb.AppendLine("        public static void exit(int status) => Environment.Exit(status);");
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
                        sb.Append($"{indent}dynamic {v.Name} = ");
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
            else if (node is IfExpNode ifNode)
            {
                sb.Append($"{indent}if (");
                EmitExprInline(ifNode.Cond, sb);
                sb.AppendLine(" != 0) {{");
                EmitNode(ifNode.Then, sb, indent + "    ");
                sb.AppendLine($"{indent}}}");
                if (ifNode.Else != null)
                {
                    sb.AppendLine($"{indent}else {{");
                    EmitNode(ifNode.Else, sb, indent + "    ");
                    sb.AppendLine($"{indent}}}");
                }
            }
            else if (node is WhileExpNode whileNode)
            {
                sb.Append($"{indent}while (");
                EmitExprInline(whileNode.Cond, sb);
                sb.AppendLine(" != 0) {{");
                EmitNode(whileNode.Body, sb, indent + "    ");
                sb.AppendLine($"{indent}}}");
            }
            else if (node is ForExpNode forNode)
            {
                sb.Append($"{indent}for (dynamic {forNode.VarName} = ");
                EmitExprInline(forNode.EscapeStart, sb);
                sb.Append($"; {forNode.VarName} <= ");
                EmitExprInline(forNode.EscapeEnd, sb);
                sb.AppendLine($"; {forNode.VarName}++) {{");
                EmitNode(forNode.Body, sb, indent + "    ");
                sb.AppendLine($"{indent}}}");
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
            else if (node is IntLiteralNode intNode) sb.Append(intNode.Value);
            else if (node is VarAccessNode v) sb.Append(v.Name);
            else if (node is AssignNode a)
            {
                sb.Append($"{a.VarName} = ");
                EmitExprInline(a.Value, sb);
            }
            else if (node is BreakExpNode) sb.Append("break");
            else if (node is BinaryExpNode b)
            {
                string op = b.Op switch
                {
                    "=" => "==",
                    "<>" => "!=",
                    _ => b.Op
                };
                sb.Append("(");
                EmitExprInline(b.Left, sb);
                sb.Append($" {op} ");
                EmitExprInline(b.Right, sb);
                sb.Append(")");
            }
            else if (node is CallExpNode c)
            {
                if (c.FuncName == "printline") sb.Append("TigerStdLib.printline(");
                else if (c.FuncName == "print") sb.Append("TigerStdLib.print(");
                else if (c.FuncName == "printint") sb.Append("TigerStdLib.printint(");
                else sb.Append($"TigerStdLib.{c.FuncName}(");

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