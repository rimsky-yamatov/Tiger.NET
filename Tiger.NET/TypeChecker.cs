using System;
using System.Collections.Generic;

namespace Tiger.NET
{
    public sealed class TypeChecker
    {
        private readonly Dictionary<string, TigerType> _variables = new();
        private readonly Dictionary<string, FunctionType> _functions = new();
        private int _loopDepth;

        public void Check(ExpNode root)
        {
            RegisterFunctions(root);
            CheckNode(root);
        }

        private void RegisterFunctions(ExpNode node)
        {
            if (node is not LetExpNode let)
                return;

            foreach (var dec in let.Decs)
            {
                if (dec is not FunctionDeclNode fn)
                    continue;

                if (_functions.ContainsKey(fn.Name))
                    throw new TypeCheckException(
                        $"Function '{fn.Name}' is already defined.");

                var parameters = new TigerType[fn.Params.Count];

                for (int i = 0; i < fn.Params.Count; i++)
                {
                    parameters[i] = TigerType.Parse(fn.Params[i].TypeName);
                }

                var returnType = TigerType.Parse(fn.ReturnType);

                _functions.Add(
                    fn.Name,
                    new FunctionType(
                        fn.Name,
                        returnType,
                        parameters));
            }
        }

        private TigerType CheckNode(ExpNode node)
        {
            switch (node)
            {
                case IntLiteralNode:
                    return SetType(node, TigerType.Int);

                case StringLiteralNode:
                    return SetType(node, TigerType.String);

                case BoolLiteralNode:
                    return SetType(node, TigerType.Bool);

                case VarAccessNode variable:
                    return SetType(node, GetVariable(variable.Name));

                case VarDeclNode declaration:
                    return CheckVariableDeclaration(declaration);

                case AssignNode assignment:
                    return CheckAssignment(assignment);

                case UnaryExpNode unary:
                    return CheckUnary(unary);

                case BinaryExpNode binary:
                    return CheckBinary(binary);

                case CallExpNode call:
                    return CheckCall(call);

                case IfExpNode conditional:
                    return CheckIf(conditional);

                case WhileExpNode loop:
                    return CheckWhile(loop);

                case ForExpNode loop:
                    return CheckFor(loop);

                case BreakExpNode:
                    if (_loopDepth == 0)
                        throw new TypeCheckException(
                            "'break' can only be used inside a loop.");

                    return SetType(node, TigerType.Void);

                case LetExpNode let:
                    return CheckLet(let);

                case FunctionDeclNode:
                    return SetType(node, TigerType.Void);

                default:
                    throw new TypeCheckException(
                        $"Unsupported AST node '{node.GetType().Name}'.");
            }
        }

        private TigerType CheckVariableDeclaration(VarDeclNode node)
        {
            if (_variables.ContainsKey(node.Name))
                throw new TypeCheckException(
                    $"Variable '{node.Name}' is already defined.");

            var initType = CheckNode(node.Init);

            TigerType finalType;

            if (node.DeclaredType != null)
            {
                finalType = TigerType.Parse(node.DeclaredType);

                RequireSameType(
                    finalType,
                    initType,
                    $"Variable '{node.Name}' initializer");
            }
            else
            {
                finalType = initType;
            }

            _variables[node.Name] = finalType;

            return SetType(node, TigerType.Void);
        }

        private TigerType CheckAssignment(AssignNode node)
        {
            var variableType = GetVariable(node.VarName);
            var valueType = CheckNode(node.Value);

            RequireSameType(
                variableType,
                valueType,
                $"Assignment to '{node.VarName}'");

            return SetType(node, TigerType.Void);
        }

        private TigerType CheckUnary(UnaryExpNode node)
        {
            var operandType = CheckNode(node.Operand);

            if (node.Op == "-")
            {
                RequireType(TigerType.Int, operandType, "Unary '-'");
                return SetType(node, TigerType.Int);
            }

            throw new TypeCheckException(
                $"Unknown unary operator '{node.Op}'.");
        }

        private TigerType CheckBinary(BinaryExpNode node)
        {
            var left = CheckNode(node.Left);
            var right = CheckNode(node.Right);

            switch (node.Op)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    RequireType(TigerType.Int, left, $"Left operand of '{node.Op}'");
                    RequireType(TigerType.Int, right, $"Right operand of '{node.Op}'");
                    return SetType(node, TigerType.Int);

                case "=":
                case "<>":
                    RequireSameType(left, right, $"Operator '{node.Op}'");
                    return SetType(node, TigerType.Bool);

                case "<":
                case "<=":
                case ">":
                case ">=":
                    RequireType(TigerType.Int, left, $"Left operand of '{node.Op}'");
                    RequireType(TigerType.Int, right, $"Right operand of '{node.Op}'");
                    return SetType(node, TigerType.Bool);

                case "and":
                case "or":
                    RequireType(TigerType.Bool, left, $"Left operand of '{node.Op}'");
                    RequireType(TigerType.Bool, right, $"Right operand of '{node.Op}'");
                    return SetType(node, TigerType.Bool);

                default:
                    throw new TypeCheckException(
                        $"Unknown binary operator '{node.Op}'.");
            }
        }

        private TigerType CheckCall(CallExpNode node)
        {
            var function = GetFunction(node.FuncName);

            if (node.Args.Count != function.ParameterTypes.Length)
            {
                throw new TypeCheckException(
                    $"Function '{node.FuncName}' expects " +
                    $"{function.ParameterTypes.Length} arguments, " +
                    $"but got {node.Args.Count}.");
            }

            for (int i = 0; i < node.Args.Count; i++)
            {
                var actual = CheckNode(node.Args[i]);
                var expected = function.ParameterTypes[i];

                RequireSameType(
                    expected,
                    actual,
                    $"Argument {i + 1} of '{node.FuncName}'");
            }

            return SetType(node, function.ReturnType);
        }

        private TigerType CheckIf(IfExpNode node)
        {
            var condition = CheckNode(node.Cond);

            RequireType(
                TigerType.Bool,
                condition,
                "If condition");

            var thenType = CheckNode(node.Then);

            if (node.Else == null)
            {
                return SetType(node, TigerType.Void);
            }

            var elseType = CheckNode(node.Else);

            RequireSameType(
                thenType,
                elseType,
                "If branches");

            return SetType(node, thenType);
        }

        private TigerType CheckWhile(WhileExpNode node)
        {
            var condition = CheckNode(node.Cond);

            RequireType(
                TigerType.Bool,
                condition,
                "While condition");

            _loopDepth++;

            try
            {
                foreach (var body in node.Body)
                    CheckNode(body);
            }
            finally
            {
                _loopDepth--;
            }

            return SetType(node, TigerType.Void);
        }

        private TigerType CheckFor(ForExpNode node)
        {
            var startType = CheckNode(node.EscapeStart);
            var endType = CheckNode(node.EscapeEnd);

            RequireType(
                TigerType.Int,
                startType,
                "For start");

            RequireType(
                TigerType.Int,
                endType,
                "For end");

            if (_variables.ContainsKey(node.VarName))
                throw new TypeCheckException(
                    $"For variable '{node.VarName}' conflicts with an existing variable.");

            _variables[node.VarName] = TigerType.Int;

            _loopDepth++;

            try
            {
                foreach (var body in node.Body)
                    CheckNode(body);
            }
            finally
            {
                _loopDepth--;
                _variables.Remove(node.VarName);
            }

            return SetType(node, TigerType.Void);
        }

        private TigerType CheckLet(LetExpNode node)
        {
            var oldVariables =
                new Dictionary<string, TigerType>(_variables);

            try
            {
                foreach (var dec in node.Decs)
                {
                    if (dec is VarDeclNode variable)
                        CheckVariableDeclaration(variable);
                }

                foreach (var dec in node.Decs)
                {
                    if (dec is FunctionDeclNode function)
                        CheckFunction(function);
                }

                TigerType result = TigerType.Void;

                foreach (var body in node.Body)
                    result = CheckNode(body);

                return SetType(node, result);
            }
            finally
            {
                _variables.Clear();

                foreach (var pair in oldVariables)
                    _variables.Add(pair.Key, pair.Value);
            }
        }

        private void CheckFunction(FunctionDeclNode node)
        {
            var function = GetFunction(node.Name);

            var oldVariables =
                new Dictionary<string, TigerType>(_variables);

            try
            {
                _variables.Clear();

                foreach (var pair in oldVariables)
                    _variables.Add(pair.Key, pair.Value);

                for (int i = 0; i < node.Params.Count; i++)
                {
                    var parameter = node.Params[i];

                    if (_variables.ContainsKey(parameter.Name))
                    {
                        throw new TypeCheckException(
                            $"Parameter '{parameter.Name}' is already defined.");
                    }

                    _variables[parameter.Name] =
                        function.ParameterTypes[i];
                }

                var bodyType = CheckNode(node.Body);

                RequireSameType(
                    function.ReturnType,
                    bodyType,
                    $"Return value of function '{node.Name}'");
            }
            finally
            {
                _variables.Clear();

                foreach (var pair in oldVariables)
                    _variables.Add(pair.Key, pair.Value);
            }
        }

        private TigerType GetVariable(string name)
        {
            if (!_variables.TryGetValue(name, out var type))
            {
                throw new TypeCheckException(
                    $"Undefined variable '{name}'.");
            }

            return type;
        }

        private FunctionType GetFunction(string name)
        {
            if (!_functions.TryGetValue(name, out var function))
            {
                if (name == "print" ||
                    name == "printline" ||
                    name == "printint" ||
                    name == "flush" ||
                    name == "getchar" ||
                    name == "ord" ||
                    name == "chr" ||
                    name == "size" ||
                    name == "substring" ||
                    name == "concat" ||
                    name == "not" ||
                    name == "exit")
                {
                    return GetBuiltin(name);
                }

                throw new TypeCheckException(
                    $"Undefined function '{name}'.");
            }

            return function;
        }

        private static FunctionType GetBuiltin(string name)
        {
            return name switch
            {
                "print" =>
                    new FunctionType(
                        name,
                        TigerType.Void,
                        new[] { TigerType.String }),

                "printline" =>
                    new FunctionType(
                        name,
                        TigerType.Void,
                        new[] { TigerType.String }),

                "printint" =>
                    new FunctionType(
                        name,
                        TigerType.Void,
                        new[] { TigerType.Int }),

                "flush" =>
                    new FunctionType(
                        name,
                        TigerType.Void,
                        Array.Empty<TigerType>()),

                "getchar" =>
                    new FunctionType(
                        name,
                        TigerType.String,
                        Array.Empty<TigerType>()),

                "ord" =>
                    new FunctionType(
                        name,
                        TigerType.Int,
                        new[] { TigerType.String }),

                "chr" =>
                    new FunctionType(
                        name,
                        TigerType.String,
                        new[] { TigerType.Int }),

                "size" =>
                    new FunctionType(
                        name,
                        TigerType.Int,
                        new[] { TigerType.String }),

                "substring" =>
                    new FunctionType(
                        name,
                        TigerType.String,
                        new[]
                        {
                            TigerType.String,
                            TigerType.Int,
                            TigerType.Int
                        }),

                "concat" =>
                    new FunctionType(
                        name,
                        TigerType.String,
                        new[]
                        {
                            TigerType.String,
                            TigerType.String
                        }),

                "not" =>
                    new FunctionType(
                        name,
                        TigerType.Int,
                        new[] { TigerType.Int }),

                "exit" =>
                    new FunctionType(
                        name,
                        TigerType.Void,
                        new[] { TigerType.Int }),

                _ => throw new TypeCheckException(
                    $"Unknown builtin function '{name}'.")
            };
        }

        private static void RequireType(
            TigerType expected,
            TigerType actual,
            string context)
        {
            if (!expected.Equals(actual))
            {
                throw new TypeCheckException(
                    $"{context}: expected '{expected}', got '{actual}'.");
            }
        }

        private static void RequireSameType(
            TigerType expected,
            TigerType actual,
            string context)
        {
            if (!expected.Equals(actual))
            {
                throw new TypeCheckException(
                    $"{context}: type mismatch, expected '{expected}', got '{actual}'.");
            }
        }

        private static TigerType SetType(
            ExpNode node,
            TigerType type)
        {
            node.InferredType = type;
            return type;
        }
    }
}