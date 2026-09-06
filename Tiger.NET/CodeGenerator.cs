using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tiger.NET
{
    public class CodeGenerator
    {
        private readonly StringBuilder _sb = new();
        private int _tempCounter;

        public static string EmitCSharp(ExpNode ast)
        {
            var generator = new CodeGenerator();
            return generator.Generate(ast);
        }

        private string Generate(ExpNode ast)
        {
            _sb.AppendLine("using System;");
            _sb.AppendLine("using System.Collections.Generic;");
            _sb.AppendLine("namespace Tiger.NET.Runtime");
            _sb.AppendLine("{");

            if (ast is LetExpNode rootLet)
            {
                foreach (var declaration in rootLet.Decs)
                {
                    if (declaration is StructDeclNode structure)
                        EmitStruct(structure);
                }

                foreach (var declaration in rootLet.Decs)
                {
                    if (declaration is FunctionDeclNode function)
                        EmitFunction(function);
                }
            }

            _sb.AppendLine("public static class ExecutableProgram");
            _sb.AppendLine("{");
            _sb.AppendLine("public static void Main(string[] args)");
            _sb.AppendLine("{");

            if (ast is LetExpNode root)
            {
                foreach (var declaration in root.Decs)
                {
                    if (declaration is VarDeclNode variable)
                        EmitStatement(variable);

                    if (declaration is FunctionDeclNode)
                    {
                    }

                    if (declaration is StructDeclNode)
                    {
                    }
                }

                foreach (var expression in root.Body)
                    EmitStatement(expression);
            }
            else
            {
                EmitStatement(ast);
            }

            _sb.AppendLine("}");
            _sb.AppendLine("}");

            EmitRuntime();

            _sb.AppendLine("}");

            return _sb.ToString();
        }

        private void EmitStruct(StructDeclNode structure)
        {
            _sb.AppendLine(
                $"public struct {SafeName(structure.Name)}");
            _sb.AppendLine("{");

            foreach (var field in structure.Fields)
            {
                _sb.AppendLine(
                    $"public {MapType(field.TypeName)} {SafeName(field.Name)};");
            }

            _sb.AppendLine("}");
        }

        private void EmitFunction(FunctionDeclNode function)
        {
            _sb.Append(
                $"public static {MapType(function.ReturnType)} {SafeName(function.Name)}(");

            for (int i = 0; i < function.Params.Count; i++)
            {
                if (i > 0)
                    _sb.Append(", ");

                _sb.Append(
                    $"{MapType(function.Params[i].TypeName)} {SafeName(function.Params[i].Name)}");
            }

            _sb.AppendLine(")");
            _sb.AppendLine("{");

            if (function.Body.Count == 0)
            {
                if (function.ReturnType == "void")
                    _sb.AppendLine("return;");
                else
                    _sb.AppendLine($"return {DefaultValue(function.ReturnType)};");
            }
            else
            {
                for (int i = 0; i < function.Body.Count - 1; i++)
                    EmitStatement(function.Body[i]);

                ExpNode last = function.Body[^1];

                if (function.ReturnType == "void")
                {
                    EmitStatement(last);
                }
                else
                {
                    _sb.Append("return ");
                    EmitExpression(last);
                    _sb.AppendLine(";");
                }
            }

            _sb.AppendLine("}");
        }

        private void EmitStatement(ExpNode node)
        {
            switch (node)
            {
                case VarDeclNode variable:
                    _sb.Append(
                        $"{MapType(variable.TypeName ?? variable.Init.InferredType?.ToString() ?? "int")} {SafeName(variable.Name)} = ");
                    EmitExpression(variable.Init);
                    _sb.AppendLine(";");
                    break;

                case AssignNode assignment:
                    EmitExpression(assignment.Target);
                    _sb.Append(" = ");
                    EmitExpression(assignment.Value);
                    _sb.AppendLine(";");
                    break;

                case IfExpNode conditional:
                    EmitIf(conditional);
                    break;

                case WhileExpNode whileNode:
                    EmitWhile(whileNode);
                    break;

                case ForExpNode forNode:
                    EmitFor(forNode);
                    break;

                case BreakExpNode:
                    _sb.AppendLine("break;");
                    break;

                case ContinueExpNode:
                    _sb.AppendLine("continue;");
                    break;

                case LetExpNode let:
                    foreach (var declaration in let.Decs)
                    {
                        if (declaration is VarDeclNode variable)
                            EmitStatement(variable);
                    }

                    foreach (var expression in let.Body)
                        EmitStatement(expression);
                    break;

                case FunctionDeclNode:
                case StructDeclNode:
                    break;

                case BlockNode block:
                    foreach (var expression in block.Expressions)
                        EmitStatement(expression);
                    break;

                default:
                    EmitExpression(node);
                    _sb.AppendLine(";");
                    break;
            }
        }

        private void EmitIf(IfExpNode node)
        {
            _sb.Append("if (");
            EmitExpression(node.Cond);
            _sb.AppendLine(")");
            _sb.AppendLine("{");

            foreach (var expression in node.Then)
                EmitStatement(expression);

            _sb.AppendLine("}");

            if (node.Else != null)
            {
                _sb.AppendLine("else");
                _sb.AppendLine("{");

                foreach (var expression in node.Else)
                    EmitStatement(expression);

                _sb.AppendLine("}");
            }
        }

        private void EmitWhile(WhileExpNode node)
        {
            _sb.Append("while (");
            EmitExpression(node.Cond);
            _sb.AppendLine(")");
            _sb.AppendLine("{");

            foreach (var expression in node.Body)
                EmitStatement(expression);

            _sb.AppendLine("}");
        }

        private void EmitFor(ForExpNode node)
        {
            string variable = SafeName(node.VarName);
            string limit = $"__limit_{_tempCounter++}";

            _sb.Append(
                $"for (int {variable} = ");

            EmitExpression(node.EscapeStart);

            _sb.Append(
                $", {limit} = ");

            EmitExpression(node.EscapeEnd);

            _sb.AppendLine(
                $"; {variable} <= {limit}; {variable}++)");

            _sb.AppendLine("{");

            foreach (var expression in node.Body)
                EmitStatement(expression);

            _sb.AppendLine("}");
        }

        private void EmitExpression(ExpNode node)
        {
            switch (node)
            {
                case IntLiteralNode integer:
                    _sb.Append(integer.Value);
                    break;

                case StringLiteralNode text:
                    _sb.Append(
                        "\"" +
                        text.Value
                            .Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\n", "\\n")
                            .Replace("\r", "\\r")
                            .Replace("\t", "\\t") +
                        "\"");
                    break;

                case BoolLiteralNode boolean:
                    _sb.Append(
                        boolean.Value ? "true" : "false");
                    break;

                case VarAccessNode variable:
                    _sb.Append(SafeName(variable.Name));
                    break;

                case AssignNode assignment:
                    EmitExpression(assignment.Target);
                    _sb.Append(" = ");
                    EmitExpression(assignment.Value);
                    break;

                case BinaryExpNode binary:
                    _sb.Append("(");
                    EmitExpression(binary.Left);

                    _sb.Append(
                        binary.Op switch
                        {
                            "=" => " == ",
                            "<>" => " != ",
                            "and" => " && ",
                            "or" => " || ",
                            _ => $" {binary.Op} "
                        });

                    EmitExpression(binary.Right);
                    _sb.Append(")");
                    break;

                case UnaryExpNode unary:
                    _sb.Append(unary.Op);
                    _sb.Append("(");
                    EmitExpression(unary.Operand);
                    _sb.Append(")");
                    break;

                case CallExpNode call:
                    EmitCall(call);
                    break;

                case ArrayLiteralNode array:
                    _sb.Append("new ");
                    _sb.Append(
                        MapTigerType(
                            array.InferredType ??
                            TigerType.ArrayOf(TigerType.Int)));

                    _sb.Append(" { ");

                    for (int i = 0; i < array.Elements.Count; i++)
                    {
                        if (i > 0)
                            _sb.Append(", ");

                        EmitExpression(array.Elements[i]);
                    }

                    _sb.Append(" }");
                    break;

                case ArrayAccessNode access:
                    EmitExpression(access.Array);
                    _sb.Append("[");
                    EmitExpression(access.Index);
                    _sb.Append("]");
                    break;

                case FieldAccessNode field:
                    EmitExpression(field.Target);
                    _sb.Append(".");
                    _sb.Append(SafeName(field.FieldName));
                    break;

                case StructInitNode structure:
                    EmitStructInit(structure);
                    break;

                case IfExpNode conditional:
                    EmitExpressionIf(conditional);
                    break;

                default:
                    _sb.Append("default");
                    break;
            }
        }

        private void EmitCall(CallExpNode call)
        {
            string name = call.FuncName switch
            {
                "print" => "TigerStdLib.print",
                "printline" => "TigerStdLib.printline",
                "printint" => "TigerStdLib.printint",
                "printbool" => "TigerStdLib.printbool",
                "flush" => "TigerStdLib.flush",
                "getchar" => "TigerStdLib.getchar",
                "ord" => "TigerStdLib.ord",
                "chr" => "TigerStdLib.chr",
                "size" => "TigerStdLib.size",
                "substring" => "TigerStdLib.substring",
                "concat" => "TigerStdLib.concat",
                "not" => "TigerStdLib.not",
                "exit" => "TigerStdLib.exit",
                _ => SafeName(call.FuncName)
            };

            _sb.Append(name);
            _sb.Append("(");

            for (int i = 0; i < call.Args.Count; i++)
            {
                if (i > 0)
                    _sb.Append(", ");

                EmitExpression(call.Args[i]);
            }

            _sb.Append(")");
        }

        private void EmitStructInit(
            StructInitNode structure)
        {
            _sb.Append(
                $"new {SafeName(structure.StructName)}");

            _sb.Append(" { ");

            TigerStruct? definition =
                FindStruct(structure.StructName);

            for (int i = 0; i < structure.Args.Count; i++)
            {
                if (i > 0)
                    _sb.Append(", ");

                if (definition != null &&
                    i < definition.Fields.Count)
                {
                    var field =
                        new List<KeyValuePair<string, TigerType>>(
                            definition.Fields)[i];

                    _sb.Append(
                        $"{SafeName(field.Key)} = ");

                    EmitExpression(structure.Args[i]);
                }
                else
                {
                    EmitExpression(structure.Args[i]);
                }
            }

            _sb.Append(" }");
        }

        private void EmitExpressionIf(
            IfExpNode node)
        {
            _sb.Append("(");
            EmitExpression(node.Cond);
            _sb.Append(" ? ");

            if (node.Then.Count == 1)
                EmitExpression(node.Then[0]);
            else
                _sb.Append("default");

            _sb.Append(" : ");

            if (node.Else != null &&
                node.Else.Count == 1)
            {
                EmitExpression(node.Else[0]);
            }
            else
            {
                _sb.Append("default");
            }

            _sb.Append(")");
        }

        private void EmitRuntime()
        {
            _sb.AppendLine(
                "public static class TigerStdLib");
            _sb.AppendLine("{");

            _sb.AppendLine(
                "public static void print(string s) { Console.Write(s); Console.Out.Flush(); }");

            _sb.AppendLine(
                "public static void printline(string s) { Console.WriteLine(s); }");

            _sb.AppendLine(
                "public static void printint(int i) { Console.Write(i); Console.Out.Flush(); }");

            _sb.AppendLine(
                "public static void printbool(bool b) { Console.Write(b); Console.Out.Flush(); }");

            _sb.AppendLine(
                "public static void flush() { Console.Out.Flush(); }");

            _sb.AppendLine(
                "public static string getchar() { int c = Console.Read(); return c < 0 ? \"\" : ((char)c).ToString(); }");

            _sb.AppendLine(
                "public static int ord(string s) { return string.IsNullOrEmpty(s) ? -1 : s[0]; }");

            _sb.AppendLine(
                "public static string chr(int i) { return ((char)i).ToString(); }");

            _sb.AppendLine(
                "public static int size(string s) { return s.Length; }");

            _sb.AppendLine(
                "public static string substring(string s, int first, int n) { return s.Substring(first, n); }");

            _sb.AppendLine(
                "public static string concat(string a, string b) { return string.Concat(a, b); }");

            _sb.AppendLine(
                "public static bool not(bool value) { return !value; }");

            _sb.AppendLine(
                "public static void exit(int status) { Environment.Exit(status); }");

            _sb.AppendLine("}");
        }

        private string MapType(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return "void";

            if (name.EndsWith("[]"))
            {
                string element =
                    name[..^2];

                return $"{MapType(element)}[]";
            }

            return name switch
            {
                "int" => "int",
                "string" => "string",
                "bool" => "bool",
                "void" => "void",
                _ => SafeName(name)
            };
        }

        private string MapTigerType(TigerType type)
        {
            if (type.IsArray &&
                type.ElementType != null)
            {
                return $"{MapTigerType(type.ElementType)}[]";
            }

            return type.Name switch
            {
                "int" => "int[]",
                "string" => "string[]",
                "bool" => "bool[]",
                _ => $"{SafeName(type.Name)}[]"
            };
        }

        private string DefaultValue(string type)
        {
            return type switch
            {
                "int" => "0",
                "string" => "\"\"",
                "bool" => "false",
                _ => "default"
            };
        }

        private string SafeName(string name)
        {
            return name switch
            {
                "class" => "@class",
                "struct" => "@struct",
                "string" => "@string",
                "int" => "@int",
                "bool" => "@bool",
                "void" => "@void",
                "namespace" => "@namespace",
                "object" => "@object",
                _ => name
            };
        }

        private TigerStruct? FindStruct(string name)
        {
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

            Directory.CreateDirectory(outputDir);

            string dllPath =
                Path.Combine(
                    outputDir,
                    $"{baseName}.dll");

            IEnumerable<MetadataReference> references =
                GetReferences(options.TargetFramework);

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

            EmitResult result =
                compilation.Emit(stream);

            if (!result.Success)
            {
                Console.WriteLine(
                    "[Error] C# compilation failed:");

                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.Severity ==
                        DiagnosticSeverity.Error)
                    {
                        Console.WriteLine(
                            $"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }
                }

                return false;
            }

            return true;
        }

        private static IEnumerable<MetadataReference> GetReferences(
            string framework)
        {
            if (!string.IsNullOrEmpty(framework) &&
                framework.StartsWith("net9"))
            {
                return Net90.References.All;
            }

            return GetNet10References();
        }

        private static IEnumerable<MetadataReference> GetNet10References()
        {
            var list =
                new List<MetadataReference>();

            string programFiles =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles);

            string refPack =
                Path.Combine(
                    programFiles,
                    "dotnet",
                    "packs",
                    "Microsoft.NETCore.App.Ref");

            if (Directory.Exists(refPack))
            {
                var versions =
                    Directory.GetDirectories(
                        refPack,
                        "10.0.*");

                if (versions.Length > 0)
                {
                    Array.Sort(versions);

                    string latest =
                        Path.Combine(
                            versions[^1],
                            "ref",
                            "net10.0");

                    if (Directory.Exists(latest))
                    {
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

            string shared =
                Path.Combine(
                    programFiles,
                    "dotnet",
                    "shared",
                    "Microsoft.NETCore.App");

            if (Directory.Exists(shared))
            {
                var versions =
                    Directory.GetDirectories(
                        shared,
                        "10.0.*");

                if (versions.Length > 0)
                {
                    Array.Sort(versions);

                    string latest =
                        versions[^1];

                    foreach (var dll in
                             Directory.GetFiles(
                                 latest,
                                 "*.dll"))
                    {
                        list.Add(
                            MetadataReference.CreateFromFile(
                                dll));
                    }
                }
            }

            return list;
        }
    }
}
