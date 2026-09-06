using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Basic.Reference.Assemblies;

namespace Tiger.NET
{
    public static class CodeGenerator
    {
        public static string EmitCSharp(ExpNode ast)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("namespace Tiger.NET.Runtime");
            sb.AppendLine("{");
            sb.AppendLine("    public static class ExecutableProgram");
            sb.AppendLine("    {");

            if (ast is LetExpNode rootLet)
            {
                foreach (var dec in rootLet.Decs)
                {
                    if (dec is StructDeclNode st)
                        EmitStruct(st, sb, "        ");
                }

                foreach (var dec in rootLet.Decs)
                {
                    if (dec is FunctionDeclNode fn)
                        EmitFunction(fn, sb, "        ");
                }
            }

            sb.AppendLine("        public static void Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            TigerStdLib.Init();");

            EmitMainNode(ast, sb, "            ");

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            EmitStdLib(sb);

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void EmitStruct(
            StructDeclNode st,
            StringBuilder sb,
            string indent)
        {
            sb.AppendLine($"{indent}public struct {st.Name}");
            sb.AppendLine($"{indent}{{");

            foreach (var field in st.Fields)
            {
                sb.AppendLine(
                    $"{indent}    public dynamic {field.Name};");
            }

            sb.AppendLine();

            sb.AppendLine(
                $"{indent}    public {st.Name}(" +
                string.Join(
                    ", ",
                    st.Fields.ConvertAll(
                        f => $"dynamic {f.Name}")) +
                ")");

            sb.AppendLine($"{indent}    {{");

            foreach (var field in st.Fields)
            {
                sb.AppendLine(
                    $"{indent}        this.{field.Name} = {field.Name};");
            }

            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        private static void EmitFunction(
            FunctionDeclNode fn,
            StringBuilder sb,
            string indent)
        {
            sb.Append(
                $"{indent}public static dynamic {fn.Name}(");

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
            sb.AppendLine();
        }

        private static void EmitFunctionBody(
            ExpNode node,
            StringBuilder sb,
            string indent)
        {
            if (node is LetExpNode let)
            {
                foreach (var dec in let.Decs)
                {
                    if (dec is VarDeclNode v)
                    {
                        sb.Append(
                            $"{indent}dynamic {v.Name} = ");

                        EmitExpression(v.Init, sb);

                        sb.AppendLine(";");
                    }
                }

                if (let.Body.Count == 0)
                {
                    sb.AppendLine(
                        $"{indent}return null;");
                    return;
                }

                for (int i = 0; i < let.Body.Count - 1; i++)
                {
                    EmitStatement(
                        let.Body[i],
                        sb,
                        indent);
                }

                EmitReturn(
                    let.Body[^1],
                    sb,
                    indent);

                return;
            }

            if (node is IfExpNode ifNode)
            {
                sb.Append(
                    $"{indent}if (TigerStdLib.IsTruthy(");

                EmitExpression(
                    ifNode.Cond,
                    sb);

                sb.AppendLine("))");
                sb.AppendLine($"{indent}{{");

                EmitReturnBlock(
                    ifNode.ThenBody,
                    sb,
                    indent + "    ");

                sb.AppendLine($"{indent}}}");

                if (ifNode.HasElse)
                {
                    sb.AppendLine($"{indent}else");
                    sb.AppendLine($"{indent}{{");

                    EmitReturnBlock(
                        ifNode.ElseBody,
                        sb,
                        indent + "    ");

                    sb.AppendLine($"{indent}}}");
                }
                else
                {
                    sb.AppendLine(
                        $"{indent}return null;");
                }

                return;
            }

            if (node is WhileExpNode whileNode)
            {
                EmitWhile(
                    whileNode,
                    sb,
                    indent);

                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            if (node is ForExpNode forNode)
            {
                EmitFor(
                    forNode,
                    sb,
                    indent);

                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            if (node is BreakExpNode)
            {
                sb.AppendLine(
                    $"{indent}break;");

                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            if (node is ContinueExpNode)
            {
                sb.AppendLine(
                    $"{indent}continue;");

                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            EmitReturn(
                node,
                sb,
                indent);
        }

        private static void EmitReturnBlock(
            List<ExpNode> body,
            StringBuilder sb,
            string indent)
        {
            if (body.Count == 0)
            {
                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            for (int i = 0; i < body.Count - 1; i++)
            {
                EmitStatement(
                    body[i],
                    sb,
                    indent);
            }

            EmitReturn(
                body[^1],
                sb,
                indent);
        }

        private static void EmitReturn(
            ExpNode node,
            StringBuilder sb,
            string indent)
        {
            if (node is IfExpNode ifNode)
            {
                EmitFunctionBody(
                    ifNode,
                    sb,
                    indent);

                return;
            }

            if (node is LetExpNode letNode)
            {
                EmitFunctionBody(
                    letNode,
                    sb,
                    indent);

                return;
            }

            if (node is WhileExpNode whileNode)
            {
                EmitWhile(
                    whileNode,
                    sb,
                    indent);

                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            if (node is ForExpNode forNode)
            {
                EmitFor(
                    forNode,
                    sb,
                    indent);

                sb.AppendLine(
                    $"{indent}return null;");

                return;
            }

            sb.Append(
                $"{indent}return ");

            EmitExpression(
                node,
                sb);

            sb.AppendLine(";");
        }

        private static void EmitMainNode(
            ExpNode node,
            StringBuilder sb,
            string indent)
        {
            if (node is LetExpNode let)
            {
                foreach (var dec in let.Decs)
                {
                    if (dec is VarDeclNode v)
                    {
                        sb.Append(
                            $"{indent}dynamic {v.Name} = ");

                        EmitExpression(
                            v.Init,
                            sb);

                        sb.AppendLine(";");
                    }
                }

                foreach (var body in let.Body)
                {
                    EmitStatement(
                        body,
                        sb,
                        indent);
                }

                return;
            }

            EmitStatement(
                node,
                sb,
                indent);
        }

        private static void EmitStatement(
            ExpNode node,
            StringBuilder sb,
            string indent)
        {
            if (node is LetExpNode let)
            {
                sb.AppendLine(
                    $"{indent}{{");

                foreach (var dec in let.Decs)
                {
                    if (dec is VarDeclNode v)
                    {
                        sb.Append(
                            $"{indent}    dynamic {v.Name} = ");

                        EmitExpression(
                            v.Init,
                            sb);

                        sb.AppendLine(";");
                    }
                }

                foreach (var body in let.Body)
                {
                    EmitStatement(
                        body,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine(
                    $"{indent}}}");

                return;
            }

            if (node is IfExpNode ifNode)
            {
                sb.Append(
                    $"{indent}if (TigerStdLib.IsTruthy(");

                EmitExpression(
                    ifNode.Cond,
                    sb);

                sb.AppendLine("))");
                sb.AppendLine($"{indent}{{");

                foreach (var body in ifNode.ThenBody)
                {
                    EmitStatement(
                        body,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine($"{indent}}}");

                if (ifNode.HasElse)
                {
                    sb.AppendLine($"{indent}else");
                    sb.AppendLine($"{indent}{{");

                    foreach (var body in ifNode.ElseBody)
                    {
                        EmitStatement(
                            body,
                            sb,
                            indent + "    ");
                    }

                    sb.AppendLine($"{indent}}}");
                }

                return;
            }

            if (node is WhileExpNode whileNode)
            {
                EmitWhile(
                    whileNode,
                    sb,
                    indent);

                return;
            }

            if (node is ForExpNode forNode)
            {
                EmitFor(
                    forNode,
                    sb,
                    indent);

                return;
            }

            if (node is BreakExpNode)
            {
                sb.AppendLine(
                    $"{indent}break;");

                return;
            }

            if (node is ContinueExpNode)
            {
                sb.AppendLine(
                    $"{indent}continue;");

                return;
            }

            if (node is FunctionDeclNode)
                return;

            if (node is StructDeclNode)
                return;

            sb.Append(
                indent);

            EmitExpression(
                node,
                sb);

            sb.AppendLine(";");
        }

        private static void EmitWhile(
            WhileExpNode node,
            StringBuilder sb,
            string indent)
        {
            sb.Append(
                $"{indent}while (TigerStdLib.IsTruthy(");

            EmitExpression(
                node.Cond,
                sb);

            sb.AppendLine("))");
            sb.AppendLine($"{indent}{{");

            foreach (var body in node.Body)
            {
                EmitStatement(
                    body,
                    sb,
                    indent + "    ");
            }

            sb.AppendLine(
                $"{indent}}}");
        }

        private static void EmitFor(
            ForExpNode node,
            StringBuilder sb,
            string indent)
        {
            string limit =
                $"__limit_{Sanitize(node.VarName)}";

            sb.Append(
                $"{indent}for (int {node.VarName} = Convert.ToInt32(");

            EmitExpression(
                node.EscapeStart,
                sb);

            sb.Append(
                $"), int {limit} = Convert.ToInt32(");

            EmitExpression(
                node.EscapeEnd,
                sb);

            sb.AppendLine(
                $"); {node.VarName} <= {limit}; {node.VarName}++)");

            sb.AppendLine(
                $"{indent}{{");

            foreach (var body in node.Body)
            {
                EmitStatement(
                    body,
                    sb,
                    indent + "    ");
            }

            sb.AppendLine(
                $"{indent}}}");
        }

        private static void EmitExpression(
            ExpNode node,
            StringBuilder sb)
        {
            switch (node)
            {
                case StringLiteralNode s:
                    sb.Append('"');
                    sb.Append(
                        s.Value
                            .Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\r", "\\r")
                            .Replace("\n", "\\n")
                            .Replace("\t", "\\t"));
                    sb.Append('"');
                    break;

                case IntLiteralNode i:
                    sb.Append(i.Value);
                    break;

                case BoolLiteralNode b:
                    sb.Append(
                        b.Value ? "true" : "false");
                    break;

                case VarAccessNode v:
                    sb.Append(v.Name);
                    break;

                case AssignNode a:
                    sb.Append(a.VarName);
                    sb.Append(" = ");
                    EmitExpression(
                        a.Value,
                        sb);
                    break;

                case ArrayAssignNode aa:
                    EmitExpression(
                        aa.Array,
                        sb);

                    sb.Append("[");
                    EmitExpression(
                        aa.Index,
                        sb);
                    sb.Append("] = ");

                    EmitExpression(
                        aa.Value,
                        sb);

                    break;

                case FieldAssignNode fa:
                    EmitExpression(
                        fa.Target,
                        sb);

                    sb.Append(".");
                    sb.Append(fa.FieldName);
                    sb.Append(" = ");

                    EmitExpression(
                        fa.Value,
                        sb);

                    break;

                case ArrayAccessNode aa:
                    EmitExpression(
                        aa.Array,
                        sb);

                    sb.Append("[");
                    EmitExpression(
                        aa.Index,
                        sb);
                    sb.Append("]");

                    break;

                case FieldAccessNode fa:
                    EmitExpression(
                        fa.Target,
                        sb);

                    sb.Append(".");
                    sb.Append(fa.FieldName);

                    break;

                case ArrayLiteralNode array:
                    sb.Append(
                        "new dynamic[] { ");

                    for (int i = 0;
                         i < array.Elements.Count;
                         i++)
                    {
                        if (i > 0)
                            sb.Append(", ");

                        EmitExpression(
                            array.Elements[i],
                            sb);
                    }

                    sb.Append(" }");
                    break;

                case StructInitNode st:
                    sb.Append(
                        $"new {st.TypeName}(");

                    for (int i = 0;
                         i < st.Args.Count;
                         i++)
                    {
                        if (i > 0)
                            sb.Append(", ");

                        EmitExpression(
                            st.Args[i],
                            sb);
                    }

                    sb.Append(")");
                    break;

                case BinaryExpNode binary:
                    sb.Append("(");

                    EmitExpression(
                        binary.Left,
                        sb);

                    sb.Append(" ");

                    sb.Append(
                        binary.Op switch
                        {
                            "=" => "==",
                            "<>" => "!=",
                            "and" => "&&",
                            "or" => "||",
                            _ => binary.Op
                        });

                    sb.Append(" ");

                    EmitExpression(
                        binary.Right,
                        sb);

                    sb.Append(")");
                    break;

                case UnaryExpNode unary:
                    sb.Append("(");
                    sb.Append(unary.Op);

                    EmitExpression(
                        unary.Operand,
                        sb);

                    sb.Append(")");
                    break;

                case CallExpNode call:
                    if (IsBuiltin(call.FuncName))
                        sb.Append("TigerStdLib.");

                    sb.Append(call.FuncName);
                    sb.Append("(");

                    for (int i = 0;
                         i < call.Args.Count;
                         i++)
                    {
                        if (i > 0)
                            sb.Append(", ");

                        EmitExpression(
                            call.Args[i],
                            sb);
                    }

                    sb.Append(")");
                    break;

                case IfExpNode ifNode:
                    EmitIfExpression(
                        ifNode,
                        sb);
                    break;

                case LetExpNode letNode:
                    EmitLetExpression(
                        letNode,
                        sb);
                    break;

                default:
                    sb.Append("null");
                    break;
            }
        }

        private static void EmitIfExpression(
            IfExpNode node,
            StringBuilder sb)
        {
            sb.Append(
                "((Func<dynamic>)(() => { ");

            sb.Append(
                "if (TigerStdLib.IsTruthy(");

            EmitExpression(
                node.Cond,
                sb);

            sb.Append(")) { ");

            EmitLambdaBody(
                node.ThenBody,
                sb);

            sb.Append(" } ");

            if (node.HasElse)
            {
                sb.Append("else { ");

                EmitLambdaBody(
                    node.ElseBody,
                    sb);

                sb.Append(" } ");
            }
            else
            {
                sb.Append(
                    "else { return null; } ");
            }

            sb.Append(
                "}))()");
        }

        private static void EmitLambdaBody(
            List<ExpNode> body,
            StringBuilder sb)
        {
            if (body.Count == 0)
            {
                sb.Append(
                    "return null;");

                return;
            }

            for (int i = 0;
                 i < body.Count - 1;
                 i++)
            {
                EmitExpression(
                    body[i],
                    sb);

                sb.Append(";");
            }

            sb.Append("return ");

            EmitExpression(
                body[^1],
                sb);

            sb.Append(";");
        }

        private static void EmitLetExpression(
            LetExpNode let,
            StringBuilder sb)
        {
            sb.Append(
                "((Func<dynamic>)(() => { ");

            foreach (var dec in let.Decs)
            {
                if (dec is VarDeclNode v)
                {
                    sb.Append(
                        $"dynamic {v.Name} = ");

                    EmitExpression(
                        v.Init,
                        sb);

                    sb.Append("; ");
                }
            }

            if (let.Body.Count == 0)
            {
                sb.Append(
                    "return null; ");
            }
            else
            {
                for (int i = 0;
                     i < let.Body.Count - 1;
                     i++)
                {
                    EmitExpression(
                        let.Body[i],
                        sb);

                    sb.Append("; ");
                }

                sb.Append(
                    "return ");

                EmitExpression(
                    let.Body[^1],
                    sb);

                sb.Append("; ");
            }

            sb.Append(
                "}))()");
        }

        private static bool IsBuiltin(
            string name)
        {
            return name is
                "print" or
                "printline" or
                "printint" or
                "printbool" or
                "flush" or
                "getchar" or
                "ord" or
                "chr" or
                "size" or
                "substring" or
                "concat" or
                "not" or
                "exit";
        }

        private static string Sanitize(
            string name)
        {
            var sb = new StringBuilder();

            foreach (char c in name)
            {
                sb.Append(
                    char.IsLetterOrDigit(c) || c == '_'
                        ? c
                        : '_');
            }

            return sb.ToString();
        }

        private static void EmitStdLib(
            StringBuilder sb)
        {
            sb.AppendLine(
                "    public static class TigerStdLib");

            sb.AppendLine(
                "    {");

            sb.AppendLine(
                "        public static void Init() { }");

            sb.AppendLine(
                "        public static void print(string s) { Console.Write(s); Console.Out.Flush(); }");

            sb.AppendLine(
                "        public static void printline(string s) { Console.WriteLine(s); }");

            sb.AppendLine(
                "        public static void printint(int i) { Console.Write(i); Console.Out.Flush(); }");

            sb.AppendLine(
                "        public static void printbool(bool b) { Console.Write(b ? 1 : 0); Console.Out.Flush(); }");

            sb.AppendLine(
                "        public static void flush() { Console.Out.Flush(); }");

            sb.AppendLine(
                "        public static string getchar() { int c = Console.Read(); return c < 0 ? \"\" : ((char)c).ToString(); }");

            sb.AppendLine(
                "        public static int ord(string s) => string.IsNullOrEmpty(s) ? -1 : s[0];");

            sb.AppendLine(
                "        public static string chr(int i) => ((char)i).ToString();");

            sb.AppendLine(
                "        public static int size(string s) => s?.Length ?? 0;");

            sb.AppendLine(
                "        public static string substring(string s, int first, int n) => s.Substring(first, n);");

            sb.AppendLine(
                "        public static string concat(string s1, string s2) => string.Concat(s1, s2);");

            sb.AppendLine(
                "        public static bool not(bool b) => !b;");

            sb.AppendLine(
                "        public static void exit(int status) => Environment.Exit(status);");

            sb.AppendLine(
                "        public static bool IsTruthy(bool value) => value;");

            sb.AppendLine(
                "    }");
        }

        public static bool CompileToAssembly(
            string csharpCode,
            CompilerOptions options)
        {
            var syntaxTree =
                CSharpSyntaxTree.ParseText(
                    csharpCode);

            string baseName =
                Path.GetFileNameWithoutExtension(
                    options.OutputFilePath);

            string outputDir =
                Path.GetDirectoryName(
                    Path.GetFullPath(
                        options.OutputFilePath))
                ?? Directory.GetCurrentDirectory();

            Directory.CreateDirectory(
                outputDir);

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
                    : options.TargetFramework
                        .ToLowerInvariant();

            string targetTfm =
                rawTfm.StartsWith("net10")
                    ? "net10.0"
                    : "net9.0";

            string frameworkVersion =
                targetTfm == "net10.0"
                    ? "10.0.11"
                    : "9.0.0";

            IEnumerable<Microsoft.CodeAnalysis.MetadataReference> references =
                targetTfm == "net10.0"
                    ? GetNet10References()
                    : Net90.References.All;

            var outputKind =
                options.TargetType switch
                {
                    OutputType.Dll =>
                        Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,

                    OutputType.WindowsApplication =>
                        Microsoft.CodeAnalysis.OutputKind.WindowsApplication,

                    _ =>
                        Microsoft.CodeAnalysis.OutputKind.ConsoleApplication
                };

            var optimization =
                options.OptimizationLevel ==
                OptimizationLevelKind.Debug
                    ? Microsoft.CodeAnalysis.OptimizationLevel.Debug
                    : Microsoft.CodeAnalysis.OptimizationLevel.Release;

            var compilation =
                Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                    baseName,
                    new[] { syntaxTree },
                    references,
                    new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                        outputKind,
                        optimizationLevel: optimization));

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
                            Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
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

            File.WriteAllText(
                configPath,
                "{\n" +
                "  \"runtimeOptions\": {\n" +
                $"    \"tfm\": \"{targetTfm}\",\n" +
                "    \"framework\": {\n" +
                "      \"name\": \"Microsoft.NETCore.App\",\n" +
                $"      \"version\": \"{frameworkVersion}\"\n" +
                "    },\n" +
                "    \"rollForward\": \"LatestMinor\"\n" +
                "  }\n" +
                "}");

            if (OperatingSystem.IsWindows())
            {
                CreateNativeAppHost(
                    dllPath,
                    exePath);
            }

            Console.WriteLine(
                $"[Success] Assembly Generated ({targetTfm}): {dllPath}");

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

                if (!Directory.Exists(
                    appHostPackDir))
                {
                    return false;
                }

                var versionDirs =
                    Directory.GetDirectories(
                        appHostPackDir,
                        "10.0.*");

                if (versionDirs.Length == 0)
                    return false;

                Array.Sort(
                    versionDirs);

                string template =
                    Path.Combine(
                        versionDirs[^1],
                        "runtimes",
                        "win-x64",
                        "native",
                        "apphost.exe");

                if (!File.Exists(template))
                    return false;

                HostModelUtils.CreateStandaloneHost(
                    template,
                    destinationExePath,
                    dllPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<Microsoft.CodeAnalysis.MetadataReference>
            GetNet10References()
        {
            var list =
                new List<Microsoft.CodeAnalysis.MetadataReference>();

            string programFiles =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles);

            string refPackDir =
                Path.Combine(
                    programFiles,
                    "dotnet",
                    "packs",
                    "Microsoft.NETCore.App.Ref");

            if (Directory.Exists(
                refPackDir))
            {
                var versionDirs =
                    Directory.GetDirectories(
                        refPackDir,
                        "10.0.*");

                if (versionDirs.Length > 0)
                {
                    Array.Sort(
                        versionDirs);

                    string latestRef =
                        Path.Combine(
                            versionDirs[^1],
                            "ref",
                            "net10.0");

                    if (Directory.Exists(
                        latestRef))
                    {
                        foreach (var dll in
                                 Directory.GetFiles(
                                     latestRef,
                                     "*.dll"))
                        {
                            list.Add(
                                Microsoft.CodeAnalysis.MetadataReference
                                    .CreateFromFile(dll));
                        }

                        return list;
                    }
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

            byte[] replacement =
                Encoding.UTF8.GetBytes(
                    Path.GetFileName(
                        appBinaryFilePath) + "\0");

            int index =
                IndexOfBytes(
                    bytes,
                    pattern);

            if (index >= 0 &&
                index + 1024 <= bytes.Length)
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
                byte[] fallback =
                    Encoding.UTF8.GetBytes(
                        "apphost.dll\0");

                int fallbackIndex =
                    IndexOfBytes(
                        bytes,
                        fallback);

                if (fallbackIndex < 0)
                    return;

                Array.Clear(
                    bytes,
                    fallbackIndex,
                    fallback.Length);

                Array.Copy(
                    replacement,
                    0,
                    bytes,
                    fallbackIndex,
                    replacement.Length);
            }

            File.WriteAllBytes(
                appHostDestinationPath,
                bytes);
        }

        private static int IndexOfBytes(
            byte[] source,
            byte[] pattern)
        {
            for (int i = 0;
                 i <= source.Length - pattern.Length;
                 i++)
            {
                bool match = true;

                for (int j = 0;
                     j < pattern.Length;
                     j++)
                {
                    if (source[i + j] !=
                        pattern[j])
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
