using System;
using System.Collections.Generic;

namespace Tiger.NET
{
    public class Scope
    {
        public Dictionary<string, TigerType> Variables { get; } = new();
        public Dictionary<string, TigerFunction> Functions { get; } = new();
        public Dictionary<string, TigerStruct> Structs { get; } = new();
    }

    public class ScopeStack
    {
        private readonly Stack<Scope> _scopes = new();

        public ScopeStack()
        {
            Push();
        }

        public void Push()
        {
            _scopes.Push(new Scope());
        }

        public void Pop()
        {
            if (_scopes.Count <= 1)
                throw new InvalidOperationException("cannot pop global scope");

            _scopes.Pop();
        }

        public void DeclareVariable(string name, TigerType type)
        {
            Current.Variables[name] = type;
        }

        public void DeclareFunction(
            string name,
            TigerFunction function)
        {
            Current.Functions[name] = function;
        }

        public void DeclareStruct(
            string name,
            TigerStruct structure)
        {
            Current.Structs[name] = structure;
        }

        public TigerType? LookupVariable(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Variables.TryGetValue(name, out var type))
                    return type;
            }

            return null;
        }

        public TigerFunction? LookupFunction(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Functions.TryGetValue(name, out var function))
                    return function;
            }

            return null;
        }

        public TigerStruct? LookupStruct(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Structs.TryGetValue(name, out var structure))
                    return structure;
            }

            return null;
        }

        public Scope Current => _scopes.Peek();
    }

    public class TypeChecker
    {
        private readonly ScopeStack _scopes = new();
        private int _loopDepth;

        public void Check(ExpNode ast)
        {
            InstallStandardLibrary();

            if (ast is LetExpNode root)
                CheckLet(root);
            else
                CheckExpression(ast);
        }

        private void InstallStandardLibrary()
        {
            foreach (var function in StandardLibrary.Functions)
            {
                _scopes.DeclareFunction(
                    function.Name,
                    function);
            }
        }

        private TigerType CheckExpression(ExpNode node)
        {
            TigerType type;

            switch (node)
            {
                case IntLiteralNode:
                    type = TigerType.Int;
                    break;

                case StringLiteralNode:
                    type = TigerType.String;
                    break;

                case BoolLiteralNode:
                    type = TigerType.Bool;
                    break;

                case VarAccessNode variable:
                    type = RequireVariable(variable.Name);
                    break;

                case VarDeclNode declaration:
                    type = CheckVarDecl(declaration);
                    break;

                case AssignNode assignment:
                    type = CheckAssignment(assignment);
                    break;

                case BinaryExpNode binary:
                    type = CheckBinary(binary);
                    break;

                case UnaryExpNode unary:
                    type = CheckUnary(unary);
                    break;

                case CallExpNode call:
                    type = CheckCall(call);
                    break;

                case LetExpNode let:
                    type = CheckLet(let);
                    break;

                case BlockNode block:
                    type = CheckBlock(block.Expressions);
                    break;

                case IfExpNode conditional:
                    type = CheckIf(conditional);
                    break;

                case WhileExpNode whileNode:
                    type = CheckWhile(whileNode);
                    break;

                case ForExpNode forNode:
                    type = CheckFor(forNode);
                    break;

                case BreakExpNode:
                    if (_loopDepth == 0)
                        throw Error("break is only valid inside a loop");

                    type = TigerType.Void;
                    break;

                case ContinueExpNode:
                    if (_loopDepth == 0)
                        throw Error("continue is only valid inside a loop");

                    type = TigerType.Void;
                    break;

                case ArrayLiteralNode array:
                    type = CheckArrayLiteral(array);
                    break;

                case ArrayAccessNode access:
                    type = CheckArrayAccess(access);
                    break;

                case FieldAccessNode field:
                    type = CheckFieldAccess(field);
                    break;

                case StructInitNode init:
                    type = CheckStructInit(init);
                    break;

                case FunctionDeclNode:
                case StructDeclNode:
                    type = TigerType.Void;
                    break;

                default:
                    throw Error(
                        $"unsupported AST node {node.GetType().Name}");
            }

            node.InferredType = type;
            return type;
        }

        private TigerType CheckVarDecl(VarDeclNode declaration)
        {
            TigerType actual =
                CheckExpression(declaration.Init);

            TigerType finalType = actual;

            if (!string.IsNullOrEmpty(declaration.TypeName))
            {
                finalType =
                    ResolveType(declaration.TypeName);

                RequireSameType(
                    finalType,
                    actual,
                    $"variable '{declaration.Name}'");
            }

            if (_scopes.Current.Variables.ContainsKey(declaration.Name))
                throw Error(
                    $"variable '{declaration.Name}' is already declared in this scope");

            _scopes.DeclareVariable(
                declaration.Name,
                finalType);

            return TigerType.Void;
        }

        private TigerType CheckAssignment(
            AssignNode assignment)
        {
            TigerType target =
                CheckAssignableTarget(assignment.Target);

            TigerType value =
                CheckExpression(assignment.Value);

            RequireSameType(
                target,
                value,
                "assignment");

            return TigerType.Void;
        }

        private TigerType CheckAssignableTarget(ExpNode target)
        {
            switch (target)
            {
                case VarAccessNode variable:
                    return RequireVariable(variable.Name);

                case ArrayAccessNode array:
                    return CheckArrayAccess(array);

                case FieldAccessNode field:
                    return CheckFieldAccess(field);

                default:
                    throw Error(
                        "left side of assignment is not assignable");
            }
        }

        private TigerType CheckBinary(
            BinaryExpNode binary)
        {
            TigerType left =
                CheckExpression(binary.Left);

            TigerType right =
                CheckExpression(binary.Right);

            switch (binary.Op)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":
                    RequireSameType(
                        TigerType.Int,
                        left,
                        $"left operand of '{binary.Op}'");

                    RequireSameType(
                        TigerType.Int,
                        right,
                        $"right operand of '{binary.Op}'");

                    return TigerType.Int;

                case "<":
                case "<=":
                case ">":
                case ">=":
                    RequireSameType(
                        TigerType.Int,
                        left,
                        $"left operand of '{binary.Op}'");

                    RequireSameType(
                        TigerType.Int,
                        right,
                        $"right operand of '{binary.Op}'");

                    return TigerType.Bool;

                case "=":
                case "<>":
                    RequireSameType(
                        left,
                        right,
                        $"operands of '{binary.Op}'");

                    return TigerType.Bool;

                case "and":
                case "or":
                    RequireSameType(
                        TigerType.Bool,
                        left,
                        $"left operand of '{binary.Op}'");

                    RequireSameType(
                        TigerType.Bool,
                        right,
                        $"right operand of '{binary.Op}'");

                    return TigerType.Bool;

                default:
                    throw Error(
                        $"unknown binary operator '{binary.Op}'");
            }
        }

        private TigerType CheckUnary(
            UnaryExpNode unary)
        {
            TigerType operand =
                CheckExpression(unary.Operand);

            if (unary.Op == "-")
            {
                RequireSameType(
                    TigerType.Int,
                    operand,
                    "unary '-'");

                return TigerType.Int;
            }

            throw Error(
                $"unknown unary operator '{unary.Op}'");
        }

        private TigerType CheckCall(
            CallExpNode call)
        {
            TigerFunction? function =
                _scopes.LookupFunction(call.FuncName);

            if (function == null)
            {
                TigerStruct? structure =
                    _scopes.LookupStruct(call.FuncName);

                if (structure != null)
                {
                    var init =
                        new StructInitNode(
                            call.FuncName,
                            call.Args);

                    return CheckStructInit(init);
                }

                throw Error(
                    $"unknown function '{call.FuncName}'");
            }

            if (call.Args.Count != function.Parameters.Count)
            {
                throw Error(
                    $"function '{call.FuncName}' expects {function.Parameters.Count} arguments but got {call.Args.Count}");
            }

            for (int i = 0; i < call.Args.Count; i++)
            {
                TigerType actual =
                    CheckExpression(call.Args[i]);

                RequireSameType(
                    function.Parameters[i],
                    actual,
                    $"argument {i + 1} of '{call.FuncName}'");
            }

            return function.ReturnType;
        }

        private TigerType CheckLet(
            LetExpNode let)
        {
            _scopes.Push();

            foreach (var declaration in let.Decs)
            {
                if (declaration is StructDeclNode structure)
                {
                    DeclareStruct(structure);
                }
            }

            foreach (var declaration in let.Decs)
            {
                if (declaration is FunctionDeclNode function)
                {
                    DeclareFunction(function);
                }
            }

            foreach (var declaration in let.Decs)
            {
                if (declaration is VarDeclNode variable)
                    CheckVarDecl(variable);
            }

            foreach (var declaration in let.Decs)
            {
                if (declaration is FunctionDeclNode function)
                    CheckFunctionBody(function);
            }

            TigerType result =
                CheckBlock(let.Body);

            _scopes.Pop();

            return result;
        }

        private TigerType CheckBlock(
            List<ExpNode> expressions)
        {
            if (expressions.Count == 0)
                return TigerType.Void;

            TigerType result = TigerType.Void;

            foreach (var expression in expressions)
                result = CheckExpression(expression);

            return result;
        }

        private TigerType CheckIf(
            IfExpNode conditional)
        {
            TigerType condition =
                CheckExpression(conditional.Cond);

            RequireSameType(
                TigerType.Bool,
                condition,
                "if condition");

            _scopes.Push();

            TigerType thenType =
                CheckBlock(conditional.Then);

            _scopes.Pop();

            if (conditional.Else == null)
                return TigerType.Void;

            _scopes.Push();

            TigerType elseType =
                CheckBlock(conditional.Else);

            _scopes.Pop();

            RequireSameType(
                thenType,
                elseType,
                "if branches");

            return thenType;
        }

        private TigerType CheckWhile(
            WhileExpNode whileNode)
        {
            TigerType condition =
                CheckExpression(whileNode.Cond);

            RequireSameType(
                TigerType.Bool,
                condition,
                "while condition");

            _loopDepth++;

            _scopes.Push();
            CheckBlock(whileNode.Body);
            _scopes.Pop();

            _loopDepth--;

            return TigerType.Void;
        }

        private TigerType CheckFor(
            ForExpNode forNode)
        {
            TigerType start =
                CheckExpression(forNode.EscapeStart);

            TigerType end =
                CheckExpression(forNode.EscapeEnd);

            RequireSameType(
                TigerType.Int,
                start,
                "for start");

            RequireSameType(
                TigerType.Int,
                end,
                "for end");

            _loopDepth++;

            _scopes.Push();

            _scopes.DeclareVariable(
                forNode.VarName,
                TigerType.Int);

            CheckBlock(forNode.Body);

            _scopes.Pop();

            _loopDepth--;

            return TigerType.Void;
        }

        private TigerType CheckArrayLiteral(
            ArrayLiteralNode array)
        {
            if (array.Elements.Count == 0)
                throw Error(
                    "cannot infer type of empty array");

            TigerType elementType =
                CheckExpression(array.Elements[0]);

            for (int i = 1; i < array.Elements.Count; i++)
            {
                TigerType type =
                    CheckExpression(array.Elements[i]);

                RequireSameType(
                    elementType,
                    type,
                    $"array element {i + 1}");
            }

            return TigerType.ArrayOf(elementType);
        }

        private TigerType CheckArrayAccess(
            ArrayAccessNode access)
        {
            TigerType array =
                CheckExpression(access.Array);

            TigerType index =
                CheckExpression(access.Index);

            RequireSameType(
                TigerType.Int,
                index,
                "array index");

            if (!array.IsArray ||
                array.ElementType == null)
            {
                throw Error(
                    "array access requires an array");
            }

            return array.ElementType;
        }

        private TigerType CheckFieldAccess(
            FieldAccessNode field)
        {
            TigerType target =
                CheckExpression(field.Target);

            TigerStruct? structure =
                _scopes.LookupStruct(target.Name);

            if (structure == null)
            {
                throw Error(
                    $"type '{target}' is not a struct");
            }

            if (!structure.Fields.TryGetValue(
                    field.FieldName,
                    out var fieldType))
            {
                throw Error(
                    $"struct '{structure.Name}' has no field '{field.FieldName}'");
            }

            return fieldType;
        }

        private TigerType CheckStructInit(
            StructInitNode init)
        {
            TigerStruct? structure =
                _scopes.LookupStruct(init.StructName);

            if (structure == null)
                throw Error(
                    $"unknown struct '{init.StructName}'");

            if (init.Args.Count != structure.Fields.Count)
                throw Error(
                    $"struct '{init.StructName}' expects {structure.Fields.Count} fields but got {init.Args.Count}");

            int index = 0;

            foreach (var field in structure.Fields)
            {
                TigerType actual =
                    CheckExpression(init.Args[index]);

                RequireSameType(
                    field.Value,
                    actual,
                    $"field '{field.Key}'");

                index++;
            }

            return new TigerType(structure.Name);
        }

        private void DeclareStruct(
            StructDeclNode declaration)
        {
            if (_scopes.LookupStruct(declaration.Name) != null)
                throw Error(
                    $"struct '{declaration.Name}' is already declared");

            var fields =
                new Dictionary<string, TigerType>();

            foreach (var field in declaration.Fields)
            {
                if (fields.ContainsKey(field.Name))
                    throw Error(
                        $"duplicate field '{field.Name}'");

                fields[field.Name] =
                    ResolveType(field.TypeName);
            }

            _scopes.DeclareStruct(
                declaration.Name,
                new TigerStruct(
                    declaration.Name,
                    fields));
        }

        private void DeclareFunction(
            FunctionDeclNode declaration)
        {
            if (_scopes.LookupFunction(declaration.Name) != null)
                throw Error(
                    $"function '{declaration.Name}' is already declared");

            var parameters =
                new List<TigerType>();

            foreach (var parameter in declaration.Params)
            {
                parameters.Add(
                    ResolveType(parameter.TypeName));
            }

            TigerType returnType =
                ResolveType(declaration.ReturnType);

            _scopes.DeclareFunction(
                declaration.Name,
                new TigerFunction(
                    declaration.Name,
                    parameters,
                    returnType));
        }

        private void CheckFunctionBody(
            FunctionDeclNode function)
        {
            TigerFunction? signature =
                _scopes.LookupFunction(function.Name);

            if (signature == null)
                throw Error(
                    $"function '{function.Name}' was not registered");

            _scopes.Push();

            for (int i = 0; i < function.Params.Count; i++)
            {
                _scopes.DeclareVariable(
                    function.Params[i].Name,
                    signature.Parameters[i]);
            }

            TigerType actual =
                CheckBlock(function.Body);

            RequireSameType(
                signature.ReturnType,
                actual,
                $"return type of function '{function.Name}'");

            _scopes.Pop();
        }

        private TigerType ResolveType(string name)
        {
            if (name.EndsWith("[]"))
            {
                string elementName =
                    name[..^2];

                return TigerType.ArrayOf(
                    ResolveType(elementName));
            }

            return name switch
            {
                "int" => TigerType.Int,
                "string" => TigerType.String,
                "bool" => TigerType.Bool,
                "void" => TigerType.Void,
                _ => ResolveStructType(name)
            };
        }

        private TigerType ResolveStructType(
            string name)
        {
            if (_scopes.LookupStruct(name) == null)
                throw Error(
                    $"unknown type '{name}'");

            return new TigerType(name);
        }

        private TigerType RequireVariable(
            string name)
        {
            TigerType? type =
                _scopes.LookupVariable(name);

            if (type == null)
                throw Error(
                    $"unknown variable '{name}'");

            return type;
        }

        private void RequireSameType(
            TigerType expected,
            TigerType actual,
            string context)
        {
            if (!expected.Equals(actual))
            {
                throw Error(
                    $"type mismatch in {context}: expected {expected}, got {actual}");
            }
        }

        private Exception Error(string message)
        {
            return new Exception(
                $"Type Error: {message}");
        }
    }
}
