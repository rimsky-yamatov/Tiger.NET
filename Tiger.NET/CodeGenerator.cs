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
                sb.AppendLine(" != 0)");
                sb.AppendLine($"{indent}{{");
                EmitNode(ifNode.Then, sb, indent + "    ");
                sb.AppendLine($"{indent}}}");
                if (ifNode.Else != null)
                {
                    sb.AppendLine($"{indent}else");
                    sb.AppendLine($"{indent}{{");
                    EmitNode(ifNode.Else, sb, indent + "    ");
                    sb.AppendLine($"{indent}}}");
                }
            }
            else if (node is WhileExpNode whileNode)
            {
                sb.Append($"{indent}while (");
                EmitExprInline(whileNode.Cond, sb);
                sb.AppendLine(" != 0)");
                sb.AppendLine($"{indent}{{");
                EmitNode(whileNode.Body, sb, indent + "    ");
                sb.AppendLine($"{indent}}}");
            }
            else if (node is ForExpNode forNode)
            {
                sb.Append($"{indent}for (dynamic {forNode.VarName} = ");
                EmitExprInline(forNode.EscapeStart, sb);
                sb.Append($"; {forNode.VarName} <= ");
                EmitExprInline(forNode.EscapeEnd, sb);
                sb.AppendLine($"; {forNode.VarName}++)");
                sb.AppendLine($"{indent}{{");
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

            OutputKind outputKind = OutputKind.ConsoleApplication;
            string targetTypeName = options.TargetType.ToString().ToLower();
            if (targetTypeName.Contains("dll")) outputKind = OutputKind.DynamicallyLinkedLibrary;
            else if (targetTypeName.Contains("win")) outputKind = OutputKind.WindowsApplication;

            string rawTfw = string.IsNullOrEmpty(options.TargetFramework) ? "net10.0" : options.TargetFramework.ToLower();
            string targetTfm;
            string frameworkVersion;
            IEnumerable<MetadataReference> references;

            if (rawTfw.Contains("net10"))
            {
                targetTfm = "net10.0";
                frameworkVersion = "10.0.11"; // 実効ランタイムバージョンに一致
                references = GetNet10References();
            }
            else
            {
                targetTfm = "net9.0";
                frameworkVersion = "9.0.0";
                references = Net90.References.All;
            }

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(outputKind, optimizationLevel: OptimizationLevel.Release)
            );

            using (var stream = File.Create(options.OutputFilePath))
            {
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
            }

            if (outputKind != OutputKind.DynamicallyLinkedLibrary)
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(options.OutputFilePath)) ?? "";
                string configPath = Path.Combine(dir, $"{assemblyName}.runtimeconfig.json");

                // 最新バージョンへのロールフォワードを有効化
                string runtimeConfigContent = "{\n" +
                    "  \"runtimeOptions\": {\n" +
                    $"    \"tfm\": \"{targetTfm}\",\n" +
                    "    \"framework\": {\n" +
                    "      \"name\": \"Microsoft.NETCore.App\",\n" +
                    $"      \"version\": \"{frameworkVersion}\"\n" +
                    "    },\n" +
                    "    \"rollForward\": \"LatestMinor\"\n" +
                    "  }\n" +
                    "}";

                File.WriteAllText(configPath, runtimeConfigContent);
            }

            Console.WriteLine($"[Success] Assembly Generated ({targetTfm}): {options.OutputFilePath}");
            return true;
        }

        private static IEnumerable<MetadataReference> GetNet10References()
        {
            var list = new List<MetadataReference>();

            // 1. インストール済み SDK Ref パックから参照アセンブリを取得
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string refPackDir = Path.Combine(programFiles, "dotnet", "packs", "Microsoft.NETCore.App.Ref");

            if (Directory.Exists(refPackDir))
            {
                var versionDirs = Directory.GetDirectories(refPackDir, "10.0.*");
                if (versionDirs.Length > 0)
                {
                    // 最新の10.0.xバージョンを選択
                    Array.Sort(versionDirs);
                    string latestVersionDir = versionDirs[^1];

                    string refDir = Path.Combine(latestVersionDir, "ref", "net10.0");
                    if (Directory.Exists(refDir))
                    {
                        foreach (var dll in Directory.GetFiles(refDir, "*.dll"))
                        {
                            list.Add(MetadataReference.CreateFromFile(dll));
                        }
                        return list;
                    }
                }
            }

            // 2. 実行環境の共有ランタイムディレクトリからアセンブリを取得
            string sharedFrameworkDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.NETCore.App", "10.0.11");
            if (Directory.Exists(sharedFrameworkDir))
            {
                foreach (var dll in Directory.GetFiles(sharedFrameworkDir, "*.dll"))
                {
                    string fileName = Path.GetFileName(dll);
                    if (!fileName.StartsWith("System.Private.", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.StartsWith("clr", StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(MetadataReference.CreateFromFile(dll));
                    }
                }
                return list;
            }

            // 3. 最終フォールバック
            var trustedPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
            foreach (var path in trustedPaths)
            {
                string fileName = Path.GetFileName(path);
                if (!fileName.StartsWith("System.Private.", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("clr", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(path)) list.Add(MetadataReference.CreateFromFile(path));
                }
            }

            return list;
        }
    }
}