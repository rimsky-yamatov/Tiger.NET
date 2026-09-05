using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

            if (ast is LetExpNode rootLet)
            {
                foreach (var dec in rootLet.Decs)
                {
                    if (dec is FunctionDeclNode fn)
                    {
                        EmitUserFunction(fn, sb, "        ");
                    }
                }
            }

            sb.AppendLine("        public static void Main(string[] args) {");
            sb.AppendLine("            TigerStdLib.Init();");

            EmitNode(ast, sb, "            ");

            sb.AppendLine("        }");
            sb.AppendLine("    }");

            sb.AppendLine("    public static class TigerStdLib {");
            sb.AppendLine("        public static dynamic Init() { return 0; }");
            sb.AppendLine("        public static dynamic print(object s) { Console.Write(s); Console.Out.Flush(); return 0; }");
            sb.AppendLine("        public static dynamic printline(object s) { Console.WriteLine(s); return 0; }");
            sb.AppendLine("        public static dynamic printint(object i) { Console.Write(i); Console.Out.Flush(); return 0; }");
            sb.AppendLine("        public static dynamic flush() { Console.Out.Flush(); return 0; }");
            sb.AppendLine("        public static string getchar() => Console.Read() == -1 ? \"\" : ((char)Console.Read()).ToString();");
            sb.AppendLine("        public static int ord(string s) => string.IsNullOrEmpty(s) ? -1 : (int)s[0];");
            sb.AppendLine("        public static string chr(int i) => ((char)i).ToString();");
            sb.AppendLine("        public static int size(string s) => s?.Length ?? 0;");
            sb.AppendLine("        public static string substring(string s, int first, int n) => s.Substring(first, n);");
            sb.AppendLine("        public static string concat(string s1, string s2) => string.Concat(s1, s2);");
            sb.AppendLine("        public static int not(int i) => i == 0 ? 1 : 0;");
            sb.AppendLine("        public static bool IsTruthy(dynamic cond) => cond is bool b ? b : Convert.ToInt32(cond) != 0;");
            sb.AppendLine("        public static dynamic exit(int status) { Environment.Exit(status); return 0; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void EmitUserFunction(
            FunctionDeclNode fn,
            StringBuilder sb,
            string indent)
        {
            sb.Append($"{indent}public static dynamic {fn.Name}(");

            for (int i = 0; i < fn.Params.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append($"dynamic {fn.Params[i].Name}");
            }

            sb.AppendLine(")");
            sb.AppendLine($"{indent}{{");

            EmitFunctionBody(
                fn.Body,
                sb,
                indent + "    ");

            sb.AppendLine($"{indent}}}");
        }

        private static void EmitFunctionBody(
            ExpNode? node,
            StringBuilder sb,
            string indent)
        {
            if (node == null)
            {
                sb.AppendLine($"{indent}return 0;");
                return;
            }

            if (node is IfExpNode ifNode)
            {
                sb.Append($"{indent}if (TigerStdLib.IsTruthy(");
                EmitExprInline(ifNode.Cond, sb);
                sb.AppendLine("))");

                sb.AppendLine($"{indent}{{");

                EmitFunctionBody(
                    ifNode.Then,
                    sb,
                    indent + "    ");

                sb.AppendLine($"{indent}}}");

                if (ifNode.Else != null)
                {
                    sb.AppendLine($"{indent}else");
                    sb.AppendLine($"{indent}{{");

                    EmitFunctionBody(
                        ifNode.Else,
                        sb,
                        indent + "    ");

                    sb.AppendLine($"{indent}}}");
                }
                else
                {
                    sb.AppendLine($"{indent}return 0;");
                }
            }
            else if (node is LetExpNode letNode)
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

                for (int i = 0; i < letNode.Body.Count; i++)
                {
                    if (i == letNode.Body.Count - 1)
                    {
                        EmitFunctionBody(
                            letNode.Body[i],
                            sb,
                            indent);
                    }
                    else
                    {
                        EmitNode(
                            letNode.Body[i],
                            sb,
                            indent);
                    }
                }
            }
            else if (node is WhileExpNode whileNode)
            {
                sb.Append($"{indent}while (TigerStdLib.IsTruthy(");
                EmitExprInline(whileNode.Cond, sb);
                sb.AppendLine("))");

                sb.AppendLine($"{indent}{{");

                foreach (var bodyNode in whileNode.Body)
                {
                    EmitNode(
                        bodyNode,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine($"{indent}}}");

                sb.AppendLine($"{indent}return 0;");
            }
            else if (node is ForExpNode forNode)
            {
                sb.Append(
                    $"{indent}for (int {forNode.VarName} = Convert.ToInt32(");

                EmitExprInline(
                    forNode.EscapeStart,
                    sb);

                sb.Append(
                    $"), __limit_{forNode.VarName} = Convert.ToInt32(");

                EmitExprInline(
                    forNode.EscapeEnd,
                    sb);

                sb.AppendLine(
                    $"); {forNode.VarName} <= __limit_{forNode.VarName}; {forNode.VarName}++)");

                sb.AppendLine($"{indent}{{");

                foreach (var bodyNode in forNode.Body)
                {
                    EmitNode(
                        bodyNode,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine($"{indent}}}");

                sb.AppendLine($"{indent}return 0;");
            }
            else if (node is BreakExpNode)
            {
                sb.AppendLine($"{indent}break;");
                sb.AppendLine($"{indent}return 0;");
            }
            else if (node is CallExpNode ||
                     node is AssignNode ||
                     node is BinaryExpNode ||
                     node is StringLiteralNode ||
                     node is IntLiteralNode ||
                     node is VarAccessNode)
            {
                sb.Append($"{indent}return ");

                EmitExprInline(
                    node,
                    sb);

                sb.AppendLine(";");
            }
            else
            {
                var seqList =
                    GetChildExpressions(node);

                if (seqList != null &&
                    seqList.Count > 0)
                {
                    for (int i = 0; i < seqList.Count; i++)
                    {
                        if (i == seqList.Count - 1)
                        {
                            EmitFunctionBody(
                                seqList[i],
                                sb,
                                indent);
                        }
                        else
                        {
                            EmitNode(
                                seqList[i],
                                sb,
                                indent);
                        }
                    }
                }
                else
                {
                    sb.Append($"{indent}return ");

                    EmitExprInline(
                        node,
                        sb);

                    sb.AppendLine(";");
                }
            }
        }

        private static void EmitNode(
            ExpNode? node,
            StringBuilder sb,
            string indent)
        {
            if (node == null)
                return;

            if (node is LetExpNode letNode)
            {
                foreach (var dec in letNode.Decs)
                {
                    if (dec is VarDeclNode v)
                    {
                        sb.Append($"{indent}dynamic {v.Name} = ");

                        EmitExprInline(
                            v.Init,
                            sb);

                        sb.AppendLine(";");
                    }
                }

                foreach (var b in letNode.Body)
                {
                    EmitNode(
                        b,
                        sb,
                        indent);
                }
            }
            else if (node is IfExpNode ifNode)
            {
                sb.Append(
                    $"{indent}if (TigerStdLib.IsTruthy(");

                EmitExprInline(
                    ifNode.Cond,
                    sb);

                sb.AppendLine("))");
                sb.AppendLine($"{indent}{{");

                EmitNode(
                    ifNode.Then,
                    sb,
                    indent + "    ");

                sb.AppendLine($"{indent}}}");

                if (ifNode.Else != null)
                {
                    sb.AppendLine($"{indent}else");
                    sb.AppendLine($"{indent}{{");

                    EmitNode(
                        ifNode.Else,
                        sb,
                        indent + "    ");

                    sb.AppendLine($"{indent}}}");
                }
            }
            else if (node is WhileExpNode whileNode)
            {
                sb.Append(
                    $"{indent}while (TigerStdLib.IsTruthy(");

                EmitExprInline(
                    whileNode.Cond,
                    sb);

                sb.AppendLine("))");
                sb.AppendLine($"{indent}{{");

                foreach (var bodyNode in whileNode.Body)
                {
                    EmitNode(
                        bodyNode,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine($"{indent}}}");
            }
            else if (node is ForExpNode forNode)
            {
                sb.Append(
                    $"{indent}for (int {forNode.VarName} = Convert.ToInt32(");

                EmitExprInline(
                    forNode.EscapeStart,
                    sb);

                sb.Append(
                    $"), __limit_{forNode.VarName} = Convert.ToInt32(");

                EmitExprInline(
                    forNode.EscapeEnd,
                    sb);

                sb.AppendLine(
                    $"); {forNode.VarName} <= __limit_{forNode.VarName}; {forNode.VarName}++)");

                sb.AppendLine($"{indent}{{");

                foreach (var bodyNode in forNode.Body)
                {
                    EmitNode(
                        bodyNode,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine($"{indent}}}");
            }
            else if (node is BreakExpNode)
            {
                sb.AppendLine($"{indent}break;");
            }
            else if (node is FunctionDeclNode)
            {
            }
            else if (node is CallExpNode ||
                     node is AssignNode ||
                     node is BinaryExpNode ||
                     node is StringLiteralNode ||
                     node is IntLiteralNode ||
                     node is VarAccessNode)
            {
                sb.Append(indent);

                if (!(node is AssignNode ||
                      node is CallExpNode))
                {
                    sb.Append("_ = ");
                }

                EmitExprInline(
                    node,
                    sb);

                sb.AppendLine(";");
            }
            else
            {
                var children =
                    GetChildExpressions(node);

                if (children != null &&
                    children.Count > 0)
                {
                    foreach (var child in children)
                    {
                        EmitNode(
                            child,
                            sb,
                            indent);
                    }
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("_ = ");

                    EmitExprInline(
                        node,
                        sb);

                    sb.AppendLine(";");
                }
            }
        }

        private static void EmitExprInline(
            ExpNode? node,
            StringBuilder sb)
        {
            if (node == null)
            {
                sb.Append("null");
                return;
            }

            if (node is StringLiteralNode s)
            {
                sb.Append(
                    $"\"{s.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            }
            else if (node is IntLiteralNode intNode)
            {
                sb.Append(intNode.Value);
            }
            else if (node is VarAccessNode v)
            {
                sb.Append(v.Name);
            }
            else if (node is AssignNode a)
            {
                sb.Append($"{a.VarName} = ");

                EmitExprInline(
                    a.Value,
                    sb);
            }
            else if (node is BreakExpNode)
            {
                sb.Append("break");
            }
            else if (node is BinaryExpNode b)
            {
                string op = b.Op switch
                {
                    "=" => "==",
                    "<>" => "!=",
                    _ => b.Op
                };

                sb.Append("(");

                EmitExprInline(
                    b.Left,
                    sb);

                sb.Append($" {op} ");

                EmitExprInline(
                    b.Right,
                    sb);

                sb.Append(")");
            }
            else if (node is CallExpNode c)
            {
                if (c.FuncName is
                    "printline" or
                    "print" or
                    "printint" or
                    "flush" or
                    "getchar" or
                    "ord" or
                    "chr" or
                    "size" or
                    "substring" or
                    "concat" or
                    "not" or
                    "exit")
                {
                    sb.Append(
                        $"TigerStdLib.{c.FuncName}(");
                }
                else
                {
                    sb.Append(
                        $"{c.FuncName}(");
                }

                for (int i = 0; i < c.Args.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    EmitExprInline(
                        c.Args[i],
                        sb);
                }

                sb.Append(")");
            }
            else
            {
                var children =
                    GetChildExpressions(node);

                if (children != null &&
                    children.Count > 0)
                {
                    sb.Append(
                        "((Func<dynamic>)(() => { ");

                    for (int i = 0; i < children.Count; i++)
                    {
                        if (i == children.Count - 1)
                        {
                            sb.Append("return ");

                            EmitExprInline(
                                children[i],
                                sb);

                            sb.Append("; ");
                        }
                        else
                        {
                            if (!(children[i] is AssignNode ||
                                  children[i] is CallExpNode))
                            {
                                sb.Append("_ = ");
                            }

                            EmitExprInline(
                                children[i],
                                sb);

                            sb.Append("; ");
                        }
                    }

                    sb.Append("}))()");
                }
                else
                {
                    sb.Append("null");
                }
            }
        }

        private static string? GetStringProp(
            object? obj,
            params string[] names)
        {
            if (obj == null)
                return null;

            var type =
                obj.GetType();

            foreach (var name in names)
            {
                var prop =
                    type.GetProperty(
                        name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                if (prop != null &&
                    prop.PropertyType == typeof(string))
                {
                    var val =
                        prop.GetValue(obj) as string;

                    if (!string.IsNullOrEmpty(val))
                        return val;
                }

                var field =
                    type.GetField(
                        name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                if (field != null &&
                    field.FieldType == typeof(string))
                {
                    var val =
                        field.GetValue(obj) as string;

                    if (!string.IsNullOrEmpty(val))
                        return val;
                }
            }

            return null;
        }

        private static ExpNode? GetExpProp(
            object? obj,
            params string[] names)
        {
            if (obj == null)
                return null;

            var type =
                obj.GetType();

            foreach (var name in names)
            {
                var prop =
                    type.GetProperty(
                        name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                if (prop != null &&
                    typeof(ExpNode).IsAssignableFrom(
                        prop.PropertyType))
                {
                    var val =
                        prop.GetValue(obj) as ExpNode;

                    if (val != null)
                        return val;
                }

                var field =
                    type.GetField(
                        name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                if (field != null &&
                    typeof(ExpNode).IsAssignableFrom(
                        field.FieldType))
                {
                    var val =
                        field.GetValue(obj) as ExpNode;

                    if (val != null)
                        return val;
                }
            }

            return null;
        }

        private static List<ExpNode>? GetChildExpressions(
            object? obj)
        {
            if (obj == null)
                return null;

            var type =
                obj.GetType();

            var props =
                type.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (typeof(IEnumerable).IsAssignableFrom(
                        prop.PropertyType) &&
                    prop.PropertyType != typeof(string))
                {
                    var val =
                        prop.GetValue(obj) as IEnumerable;

                    if (val != null)
                    {
                        var list =
                            new List<ExpNode>();

                        foreach (var item in val)
                        {
                            if (item is ExpNode exp)
                                list.Add(exp);
                        }

                        if (list.Count > 0)
                            return list;
                    }
                }
            }

            var fields =
                type.GetFields(
                    BindingFlags.Public |
                    BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (typeof(IEnumerable).IsAssignableFrom(
                        field.FieldType) &&
                    field.FieldType != typeof(string))
                {
                    var val =
                        field.GetValue(obj) as IEnumerable;

                    if (val != null)
                    {
                        var list =
                            new List<ExpNode>();

                        foreach (var item in val)
                        {
                            if (item is ExpNode exp)
                                list.Add(exp);
                        }

                        if (list.Count > 0)
                            return list;
                    }
                }
            }

            return null;
        }

        public static bool CompileToAssembly(
            string csharpCode,
            CompilerOptions options)
        {
            SyntaxTree syntaxTree =
                CSharpSyntaxTree.ParseText(csharpCode);

            string baseName =
                Path.GetFileNameWithoutExtension(
                    options.OutputFilePath);

            string outputDir =
                Path.GetDirectoryName(
                    Path.GetFullPath(
                        options.OutputFilePath))
                ?? Directory.GetCurrentDirectory();

            string dllPath =
                Path.Combine(
                    outputDir,
                    $"{baseName}.dll");

            string exePath =
                Path.Combine(
                    outputDir,
                    $"{baseName}.exe");

            string rawTfm =
                string.IsNullOrEmpty(
                    options.TargetFramework)
                    ? "net10.0"
                    : options.TargetFramework.ToLower();

            string targetTfm =
                rawTfm.StartsWith("net10")
                    ? "net10.0"
                    : "net9.0";

            string frameworkVersion =
                targetTfm == "net10.0"
                    ? "10.0.11"
                    : "9.0.0";

            IEnumerable<MetadataReference> references =
                targetTfm == "net10.0"
                    ? GetNet10References()
                    : Net90.References.All;

            OutputKind outputKind =
                options.TargetType switch
                {
                    OutputType.Dll =>
                        OutputKind.DynamicallyLinkedLibrary,

                    OutputType.WindowsApplication =>
                        OutputKind.WindowsApplication,

                    _ =>
                        OutputKind.ConsoleApplication
                };

            OptimizationLevel optLevel =
                options.OptimizationLevel ==
                OptimizationLevelKind.Debug
                    ? OptimizationLevel.Debug
                    : OptimizationLevel.Release;

            var compilation =
                CSharpCompilation.Create(
                    baseName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options:
                        new CSharpCompilationOptions(
                            outputKind,
                            optimizationLevel:
                                optLevel));

            using (var stream =
                   File.Create(dllPath))
            {
                var result =
                    compilation.Emit(stream);

                if (!result.Success)
                {
                    Console.WriteLine(
                        "[Error] Compilation Failed:");

                    foreach (var diagnostic
                             in result.Diagnostics)
                    {
                        if (diagnostic.Severity ==
                            DiagnosticSeverity.Error)
                        {
                            Console.WriteLine(
                                $"  {diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                    }

                    return false;
                }
            }

            string configPath =
                Path.Combine(
                    outputDir,
                    $"{baseName}.runtimeconfig.json");

            string runtimeConfigContent =
                "{\n" +
                "  \"runtimeOptions\": {\n" +
                $"    \"tfm\": \"{targetTfm}\",\n" +
                "    \"framework\": {\n" +
                "      \"name\": \"Microsoft.NETCore.App\",\n" +
                $"      \"version\": \"{frameworkVersion}\"\n" +
                "    },\n" +
                "    \"rollForward\": \"LatestMinor\"\n" +
                "  }\n" +
                "}";

            File.WriteAllText(
                configPath,
                runtimeConfigContent);

            bool successHost =
                CreateNativeAppHost(
                    dllPath,
                    exePath);

            if (!successHost)
            {
                Console.WriteLine(
                    "[Warning] Native AppHost generation skipped. Run using 'dotnet " +
                    baseName +
                    ".dll'");
            }

            Console.WriteLine(
                $"[Success] Assembly Generated ({targetTfm}): {dllPath}");

            if (File.Exists(exePath))
            {
                Console.WriteLine(
                    $"[Success] Native Executable Launcher Created: {exePath}");
            }

            return true;
        }

        private static bool CreateNativeAppHost(
            string dllPath,
            string destinationExePath)
        {
            try
            {
                string programFiles =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ProgramFiles);

                string appHostPackDir =
                    Path.Combine(
                        programFiles,
                        "dotnet",
                        "packs",
                        "Microsoft.NETCore.App.Host.win-x64");

                if (!Directory.Exists(appHostPackDir))
                    return false;

                var versionDirs =
                    Directory.GetDirectories(
                        appHostPackDir,
                        "10.0.*");

                if (versionDirs.Length == 0)
                {
                    versionDirs =
                        Directory.GetDirectories(
                            appHostPackDir,
                            "*");
                }

                if (versionDirs.Length == 0)
                    return false;

                Array.Sort(versionDirs);

                string templateAppHostPath =
                    Path.Combine(
                        versionDirs[^1],
                        "runtimes",
                        "win-x64",
                        "native",
                        "apphost.exe");

                if (!File.Exists(templateAppHostPath))
                    return false;

                HostModelUtils.CreateStandaloneHost(
                    templateAppHostPath,
                    destinationExePath,
                    dllPath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[AppHost Warning] {ex.Message}");

                return false;
            }
        }

        private static IEnumerable<MetadataReference>
            GetNet10References()
        {
            var list =
                new List<MetadataReference>();

            string programFiles =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles);

            string refPackDir =
                Path.Combine(
                    programFiles,
                    "dotnet",
                    "packs",
                    "Microsoft.NETCore.App.Ref");

            if (Directory.Exists(refPackDir))
            {
                var versionDirs =
                    Directory.GetDirectories(
                        refPackDir,
                        "10.0.*");

                if (versionDirs.Length > 0)
                {
                    Array.Sort(versionDirs);

                    string latestRef =
                        Path.Combine(
                            versionDirs[^1],
                            "ref",
                            "net10.0");

                    if (Directory.Exists(latestRef))
                    {
                        foreach (var dll
                                 in Directory.GetFiles(
                                     latestRef,
                                     "*.dll"))
                        {
                            list.Add(
                                MetadataReference.CreateFromFile(
                                    dll));
                        }

                        return list;
                    }
                }
            }

            string sharedDir =
                Path.Combine(
                    programFiles,
                    "dotnet",
                    "shared",
                    "Microsoft.NETCore.App");

            if (Directory.Exists(sharedDir))
            {
                var runtimeDirs =
                    Directory.GetDirectories(
                        sharedDir,
                        "10.0.*");

                if (runtimeDirs.Length > 0)
                {
                    Array.Sort(runtimeDirs);

                    string targetDir =
                        runtimeDirs[^1];

                    foreach (var dll
                             in Directory.GetFiles(
                                 targetDir,
                                 "*.dll"))
                    {
                        string name =
                            Path.GetFileName(dll);

                        if (!name.StartsWith(
                                "System.Private.",
                                StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith(
                                "clr",
                                StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith(
                                "mscord",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(
                                MetadataReference.CreateFromFile(
                                    dll));
                        }
                    }

                    return list;
                }
            }

            return list;
        }
    }

    public static class HostModelUtils
    {
        public static void CreateStandaloneHost(
            string appHostSourcePath,
            string appHostDestinationPath,
            string appBinaryFilePath)
        {
            byte[] bytes =
                File.ReadAllBytes(
                    appHostSourcePath);

            byte[] pattern =
                Encoding.ASCII.GetBytes(
                    "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2");

            string fileName =
                Path.GetFileName(
                    appBinaryFilePath);

            byte[] replacement =
                Encoding.UTF8.GetBytes(
                    fileName + "\0");

            int index =
                IndexOfBytes(
                    bytes,
                    pattern);

            if (index != -1)
            {
                Array.Clear(
                    bytes,
                    index,
                    1024);

                Array.Copy(
                    replacement,
                    0,
                    bytes,
                    index,
                    replacement.Length);
            }
            else
            {
                byte[] fallbackPattern =
                    Encoding.UTF8.GetBytes(
                        "apphost.dll\0");

                int fallbackIndex =
                    IndexOfBytes(
                        bytes,
                        fallbackPattern);

                if (fallbackIndex != -1)
                {
                    Array.Clear(
                        bytes,
                        fallbackIndex,
                        fallbackPattern.Length);

                    Array.Copy(
                        replacement,
                        0,
                        bytes,
                        fallbackIndex,
                        replacement.Length);
                }
            }

            File.WriteAllBytes(
                appHostDestinationPath,
                bytes);
        }

        private static int IndexOfBytes(
            byte[] source,
            byte[] pattern)
        {
            for (
                int i = 0;
                i <= source.Length - pattern.Length;
                i++)
            {
                bool match = true;

                for (
                    int j = 0;
                    j < pattern.Length;
                    j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return i;
            }

            return -1;
        }
    }
}