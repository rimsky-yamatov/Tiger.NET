using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Basic.Reference.Assemblies;
using Microsoft.NETCore.HostModel.AppHost; // Microsoft.NETCore.HostModel が必要です

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
            string baseName = Path.GetFileNameWithoutExtension(options.OutputFilePath);
            string outputDir = Path.GetDirectoryName(Path.GetFullPath(options.OutputFilePath)) ?? Directory.GetCurrentDirectory();

            // 出力する主要なアセンブリは常に DLL としてコンパイルする
            string dllPath = Path.Combine(outputDir, $"{baseName}.dll");
            string exePath = Path.Combine(outputDir, $"{baseName}.exe");

            string rawTfw = string.IsNullOrEmpty(options.TargetFramework) ? "net10.0" : options.TargetFramework.ToLower();
            string targetTfm = rawTfw.StartsWith("net10") ? "net10.0" : "net9.0";
            string frameworkVersion = targetTfm == "net10.0" ? "10.0.11" : "9.0.0";

            IEnumerable<MetadataReference> references = targetTfm == "net10.0"
                ? GetNet10References()
                : Net90.References.All;

            // Roslyn には ConsoleApplication を指定して DLL を生成
            var compilation = CSharpCompilation.Create(
                baseName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Release)
            );

            // 1. まず hello.dll を生成
            using (var stream = File.Create(dllPath))
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

            // 2. hello.runtimeconfig.json を生成
            string configPath = Path.Combine(outputDir, $"{baseName}.runtimeconfig.json");
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

            // 3. EXE ランチャーの生成
            bool successHost = CreateNativeAppHost(dllPath, exePath, targetTfm);
            if (!successHost)
            {
                Console.WriteLine("[Warning] Native AppHost generation skipped or failed. Use 'dotnet " + baseName + ".dll' to run.");
            }

            Console.WriteLine($"[Success] Assembly Generated ({targetTfm}): {dllPath}");
            if (File.Exists(exePath))
            {
                Console.WriteLine($"[Success] Native Executable Launcher Created: {exePath}");
            }

            return true;
        }

        private static bool CreateNativeAppHost(string dllPath, string destinationExePath, string targetTfm)
        {
            try
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string appHostPackDir = Path.Combine(programFiles, "dotnet", "packs", "Microsoft.NETCore.App.Host.win-x64");

                if (!Directory.Exists(appHostPackDir)) return false;

                var versionDirs = Directory.GetDirectories(appHostPackDir, "10.0.*");
                if (versionDirs.Length == 0) versionDirs = Directory.GetDirectories(appHostPackDir, "*");
                if (versionDirs.Length == 0) return false;

                Array.Sort(versionDirs);
                string templateAppHostPath = Path.Combine(versionDirs[^1], "runtimes", "win-x64", "native", "apphost.exe");

                if (!File.Exists(templateAppHostPath)) return false;

                string appDllName = Path.GetFileName(dllPath);

                // AppHostBinaryModifier を用いて apphost.exe 内のバイナリのプレースホルダーを dll 名に修正して出力
                HostModelUtils.CreateStandaloneHost(templateAppHostPath, destinationExePath, appDllName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppHost Error] {ex.Message}");
                return false;
            }
        }

        private static IEnumerable<MetadataReference> GetNet10References()
        {
            var list = new List<MetadataReference>();
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string refPackDir = Path.Combine(programFiles, "dotnet", "packs", "Microsoft.NETCore.App.Ref");
            if (Directory.Exists(refPackDir))
            {
                var versionDirs = Directory.GetDirectories(refPackDir, "10.0.*");
                if (versionDirs.Length > 0)
                {
                    Array.Sort(versionDirs);
                    string latestRef = Path.Combine(versionDirs[^1], "ref", "net10.0");
                    if (Directory.Exists(latestRef))
                    {
                        foreach (var dll in Directory.GetFiles(latestRef, "*.dll"))
                        {
                            list.Add(MetadataReference.CreateFromFile(dll));
                        }
                        return list;
                    }
                }
            }

            string sharedDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.NETCore.App");
            if (Directory.Exists(sharedDir))
            {
                var runtimeDirs = Directory.GetDirectories(sharedDir, "10.0.*");
                if (runtimeDirs.Length > 0)
                {
                    Array.Sort(runtimeDirs);
                    string targetDir = runtimeDirs[^1];
                    foreach (var dll in Directory.GetFiles(targetDir, "*.dll"))
                    {
                        string name = Path.GetFileName(dll);
                        if (!name.StartsWith("System.Private.", StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith("clr", StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith("mscord", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(MetadataReference.CreateFromFile(dll));
                        }
                    }
                    return list;
                }
            }

            return list;
        }
    }

    // AppHost の書き換え用ヘルパー
    public static class HostModelUtils
    {
        public static void CreateStandaloneHost(string appHostSourcePath, string appHostDestinationPath, string appBinaryFilePath)
        {
            // Microsoft.NETCore.HostModel パッケージの AppHost.Create を使用
            HostModel.AppHost.HostWriter.CreateAppHost(
                appHostSourceFilePath: appHostSourcePath,
                appHostDestinationFilePath: appHostDestinationPath,
                appBinaryFilePath: appBinaryFilePath
            );
        }
    }
}