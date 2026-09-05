using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
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

            if (ast is LetExpNode root)
            {
                foreach (var dec in root.Decs)
                {
                    if (dec is FunctionDeclNode function)
                        EmitFunction(function, sb);
                }
            }

            sb.AppendLine(
                "        public static void Main(string[] args)");

            sb.AppendLine("        {");
            sb.AppendLine(
                "            TigerStdLib.Init();");

            EmitStatements(
                ast,
                sb,
                "            ");

            sb.AppendLine("        }");
            sb.AppendLine("    }");

            EmitStdLib(sb);

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void EmitFunction(
            FunctionDeclNode function,
            StringBuilder sb)
        {
            string returnType =
                ToCSharpType(
                    TigerType.Parse(function.ReturnType));

            sb.Append(
                $"        public static {returnType} {function.Name}(");

            for (int i = 0; i < function.Params.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                string type =
                    ToCSharpType(
                        TigerType.Parse(
                            function.Params[i].TypeName));

                sb.Append(
                    $"{type} {function.Params[i].Name}");
            }

            sb.AppendLine(")");
            sb.AppendLine("        {");

            EmitFunctionBody(
                function.Body,
                sb,
                "            ",
                TigerType.Parse(function.ReturnType));

            sb.AppendLine("        }");
        }

        private static void EmitFunctionBody(
            ExpNode node,
            StringBuilder sb,
            string indent,
            TigerType returnType)
        {
            if (node is IfExpNode conditional)
            {
                EmitIf(
                    conditional,
                    sb,
                    indent,
                    true,
                    returnType);

                return;
            }

            if (node is LetExpNode let)
            {
                foreach (var dec in let.Decs)
                {
                    if (dec is VarDeclNode variable)
                        EmitVariable(variable, sb, indent);
                }

                for (int i = 0; i < let.Body.Count; i++)
                {
                    bool last =
                        i == let.Body.Count - 1;

                    if (last)
                    {
                        sb.Append(indent);
                        sb.Append("return ");
                        EmitExpr(
                            let.Body[i],
                            sb);
                        sb.AppendLine(";");
                    }
                    else
                    {
                        EmitStatement(
                            let.Body[i],
                            sb,
                            indent);
                    }
                }

                return;
            }

            sb.Append(indent);
            sb.Append("return ");
            EmitExpr(node, sb);
            sb.AppendLine(";");
        }

        private static void EmitStatements(
            ExpNode node,
            StringBuilder sb,
            string indent)
        {
            if (node is LetExpNode let)
            {
                foreach (var dec in let.Decs)
                {
                    if (dec is VarDeclNode variable)
                        EmitVariable(
                            variable,
                            sb,
                            indent);
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
            switch (node)
            {
                case VarDeclNode variable:
                    EmitVariable(
                        variable,
                        sb,
                        indent);
                    break;

                case AssignNode:
                    sb.Append(indent);
                    EmitExpr(node, sb);
                    sb.AppendLine(";");
                    break;

                case CallExpNode:
                    sb.Append(indent);
                    EmitExpr(node, sb);
                    sb.AppendLine(";");
                    break;

                case IfExpNode conditional:
                    EmitIf(
                        conditional,
                        sb,
                        indent,
                        false,
                        TigerType.Void);
                    break;

                case WhileExpNode loop:
                    EmitWhile(
                        loop,
                        sb,
                        indent);
                    break;

                case ForExpNode loop:
                    EmitFor(
                        loop,
                        sb,
                        indent);
                    break;

                case BreakExpNode:
                    sb.AppendLine(
                        $"{indent}break;");
                    break;

                case LetExpNode let:
                    EmitStatements(
                        let,
                        sb,
                        indent);
                    break;

                case FunctionDeclNode:
                    break;

                default:
                    sb.Append(indent);
                    EmitExpr(node, sb);
                    sb.AppendLine(";");
                    break;
            }
        }

        private static void EmitVariable(
            VarDeclNode node,
            StringBuilder sb,
            string indent)
        {
            TigerType type =
                node.InferredType ?? TigerType.Int;

            sb.Append(
                $"{indent}{ToCSharpType(type)} {node.Name} = ");

            EmitExpr(
                node.Init,
                sb);

            sb.AppendLine(";");
        }

        private static void EmitIf(
            IfExpNode node,
            StringBuilder sb,
            string indent,
            bool returning,
            TigerType returnType)
        {
            sb.Append(
                $"{indent}if (");

            EmitExpr(
                node.Cond,
                sb);

            sb.AppendLine(")");
            sb.AppendLine(
                $"{indent}{{");

            if (returning)
            {
                sb.Append(
                    $"{indent}    return ");

                EmitExpr(
                    node.Then,
                    sb);

                sb.AppendLine(";");
            }
            else
            {
                EmitStatement(
                    node.Then,
                    sb,
                    indent + "    ");
            }

            sb.AppendLine(
                $"{indent}}}");

            if (node.Else != null)
            {
                sb.AppendLine(
                    $"{indent}else");
                sb.AppendLine(
                    $"{indent}{{");

                if (returning)
                {
                    sb.Append(
                        $"{indent}    return ");

                    EmitExpr(
                        node.Else,
                        sb);

                    sb.AppendLine(";");
                }
                else
                {
                    EmitStatement(
                        node.Else,
                        sb,
                        indent + "    ");
                }

                sb.AppendLine(
                    $"{indent}}}");
            }
            else if (returning &&
                     returnType.Equals(TigerType.Void))
            {
                sb.AppendLine(
                    $"{indent}return;");
            }
        }

        private static void EmitWhile(
            WhileExpNode node,
            StringBuilder sb,
            string indent)
        {
            sb.Append(
                $"{indent}while (");

            EmitExpr(
                node.Cond,
                sb);

            sb.AppendLine(")");
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

        private static void EmitFor(
            ForExpNode node,
            StringBuilder sb,
            string indent)
        {
            string limit =
                $"__limit_{Sanitize(node.VarName)}";

            sb.Append(
                $"{indent}for (int {node.VarName} = ");

            EmitExpr(
                node.EscapeStart,
                sb);

            sb.Append(
                $"; {node.VarName} <= {limit}; ");

            sb.Append(
                $"{node.VarName}++)");

            sb.AppendLine();
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

        private static void EmitExpr(
            ExpNode node,
            StringBuilder sb)
        {
            switch (node)
            {
                case IntLiteralNode integer:
                    sb.Append(integer.Value);
                    return;

                case StringLiteralNode str:
                    sb.Append(
                        "\"" +
                        EscapeString(str.Value) +
                        "\"");
                    return;

                case BoolLiteralNode boolean:
                    sb.Append(
                        boolean.Value
                            ? "true"
                            : "false");
                    return;

                case VarAccessNode variable:
                    sb.Append(variable.Name);
                    return;

                case AssignNode assignment:
                    sb.Append(
                        $"{assignment.VarName} = ");
                    EmitExpr(
                        assignment.Value,
                        sb);
                    return;

                case UnaryExpNode unary:
                    sb.Append("(");
                    sb.Append(unary.Op);
                    EmitExpr(
                        unary.Operand,
                        sb);
                    sb.Append(")");
                    return;

                case BinaryExpNode binary:
                    sb.Append("(");

                    EmitExpr(
                        binary.Left,
                        sb);

                    sb.Append(
                        $" {MapOperator(binary.Op)} ");

                    EmitExpr(
                        binary.Right,
                        sb);

                    sb.Append(")");
                    return;

                case CallExpNode call:
                    EmitCall(
                        call,
                        sb);
                    return;

                case IfExpNode conditional:
                    EmitConditionalExpression(
                        conditional,
                        sb);
                    return;

                default:
                    sb.Append("0");
                    return;
            }
        }

        private static void EmitCall(
            CallExpNode node,
            StringBuilder sb)
        {
            if (IsBuiltin(node.FuncName))
            {
                sb.Append(
                    $"TigerStdLib.{node.FuncName}(");
            }
            else
            {
                sb.Append(
                    $"{node.FuncName}(");
            }

            for (int i = 0; i < node.Args.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                EmitExpr(
                    node.Args[i],
                    sb);
            }

            sb.Append(")");
        }

        private static void EmitConditionalExpression(
            IfExpNode node,
            StringBuilder sb)
        {
            sb.Append("(");

            EmitExpr(
                node.Cond,
                sb);

            sb.Append(" ? ");

            EmitExpr(
                node.Then,
                sb);

            sb.Append(" : ");

            if (node.Else != null)
            {
                EmitExpr(
                    node.Else,
                    sb);
            }
            else
            {
                sb.Append("0");
            }

            sb.Append(")");
        }

        private static string MapOperator(
            string op)
        {
            return op switch
            {
                "=" => "==",
                "<>" => "!=",
                "and" => "&&",
                "or" => "||",
                _ => op
            };
        }

        private static bool IsBuiltin(
            string name)
        {
            return name switch
            {
                "print" => true,
                "printline" => true,
                "printint" => true,
                "flush" => true,
                "getchar" => true,
                "ord" => true,
                "chr" => true,
                "size" => true,
                "substring" => true,
                "concat" => true,
                "not" => true,
                "exit" => true,
                _ => false
            };
        }

        private static string ToCSharpType(
            TigerType type)
        {
            return type.Kind switch
            {
                TigerTypeKind.Int => "int",
                TigerTypeKind.String => "string",
                TigerTypeKind.Bool => "bool",
                TigerTypeKind.Void => "void",
                _ => "object"
            };
        }

        private static string EscapeString(
            string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string Sanitize(
            string value)
        {
            var sb = new StringBuilder();

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) ||
                    c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            return sb.ToString();
        }

        private static void EmitStdLib(
            StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine(
                "    public static class TigerStdLib");
            sb.AppendLine("    {");

            sb.AppendLine(
                "        public static void Init() { }");

            sb.AppendLine(
                "        public static void print(string s)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            Console.Write(s);");
            sb.AppendLine(
                "            Console.Out.Flush();");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static void printline(string s)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            Console.WriteLine(s);");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static void printint(int i)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            Console.Write(i);");
            sb.AppendLine(
                "            Console.Out.Flush();");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static void flush()");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            Console.Out.Flush();");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static string getchar()");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            int c = Console.Read();");
            sb.AppendLine(
                "            return c == -1 ? \"\" : ((char)c).ToString();");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static int ord(string s)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            return string.IsNullOrEmpty(s) ? -1 : s[0];");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static string chr(int i)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            return ((char)i).ToString();");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static int size(string s)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            return s.Length;");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static string substring(string s, int first, int n)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            return s.Substring(first, n);");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static string concat(string s1, string s2)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            return string.Concat(s1, s2);");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static int not(int i)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            return i == 0 ? 1 : 0;");
            sb.AppendLine("        }");

            sb.AppendLine(
                "        public static void exit(int status)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            Environment.Exit(status);");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
        }

        public static bool CompileToAssembly(
            string csharpCode,
            CompilerOptions options)
        {
            SyntaxTree syntaxTree =
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

            Directory.CreateDirectory(outputDir);

            string dllPath =
                Path.Combine(
                    outputDir,
                    $"{baseName}.dll");

            string rawTfm =
                string.IsNullOrEmpty(
                    options.TargetFramework)
                    ? "net10.0"
                    : options.TargetFramework.ToLowerInvariant();

            string targetTfm =
                rawTfm.StartsWith("net10")
                    ? "net10.0"
                    : "net9.0";

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

            OptimizationLevel optimization =
                options.OptimizationLevel ==
                OptimizationLevelKind.Debug
                    ? OptimizationLevel.Debug
                    : OptimizationLevel.Release;

            var compilation =
                CSharpCompilation.Create(
                    baseName,
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(
                        outputKind,
                        optimizationLevel:
                            optimization));

            using var stream =
                File.Create(dllPath);

            var result =
                compilation.Emit(stream);

            if (!result.Success)
            {
                Console.WriteLine(
                    "[Error] Generated C# compilation failed:");

                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.Severity ==
                        DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(
                            $"  {diagnostic.Id}: " +
                            diagnostic.GetMessage());
                    }
                }

                return false;
            }

            Console.WriteLine(
                $"[Success] Assembly Generated: {dllPath}");

            return true;
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

            if (!Directory.Exists(refPackDir))
                return list;

            var versions =
                Directory.GetDirectories(
                    refPackDir,
                    "10.0.*");

            if (versions.Length == 0)
                return list;

            Array.Sort(versions);

            string latest =
                Path.Combine(
                    versions[^1],
                    "ref",
                    "net10.0");

            if (!Directory.Exists(latest))
                return list;

            foreach (var dll in
                     Directory.GetFiles(
                         latest,
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