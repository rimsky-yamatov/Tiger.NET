using System;
using System.Collections.Generic;

namespace Tiger.NET
{
    public abstract class ExpNode
    {
        public TigerType? InferredType { get; set; }
    }

    public class StringLiteralNode : ExpNode
    {
        public string Value { get; set; }
        public StringLiteralNode(string value) => Value = value;
    }

    public class IntLiteralNode : ExpNode
    {
        public int Value { get; set; }
        public IntLiteralNode(int value) => Value = value;
    }

    public class BoolLiteralNode : ExpNode
    {
        public bool Value { get; set; }
        public BoolLiteralNode(bool value) => Value = value;
    }

    public class VarDeclNode : ExpNode
    {
        public string Name { get; set; }
        public string? TypeName { get; set; }
        public ExpNode Init { get; set; }

        public VarDeclNode(string name, ExpNode init, string? typeName = null)
        {
            Name = name;
            Init = init;
            TypeName = typeName;
        }
    }

    public class FuncParam
    {
        public string Name { get; set; }
        public string TypeName { get; set; }

        public FuncParam(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    public class FunctionDeclNode : ExpNode
    {
        public string Name { get; set; }
        public List<FuncParam> Params { get; set; }
        public string ReturnType { get; set; }
        public List<ExpNode> Body { get; set; }

        public FunctionDeclNode(
            string name,
            List<FuncParam> parameters,
            string returnType,
            List<ExpNode> body)
        {
            Name = name;
            Params = parameters;
            ReturnType = returnType;
            Body = body;
        }
    }

    public class StructDeclNode : ExpNode
    {
        public string Name { get; set; }
        public List<StructField> Fields { get; set; }

        public StructDeclNode(string name, List<StructField> fields)
        {
            Name = name;
            Fields = fields;
        }
    }

    public class StructField
    {
        public string Name { get; set; }
        public string TypeName { get; set; }

        public StructField(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    public class AssignNode : ExpNode
    {
        public ExpNode Target { get; set; }
        public ExpNode Value { get; set; }

        public AssignNode(ExpNode target, ExpNode value)
        {
            Target = target;
            Value = value;
        }
    }

    public class CallExpNode : ExpNode
    {
        public string FuncName { get; set; }
        public List<ExpNode> Args { get; set; }

        public CallExpNode(string funcName, List<ExpNode> args)
        {
            FuncName = funcName;
            Args = args;
        }
    }

    public class LetExpNode : ExpNode
    {
        public List<ExpNode> Decs { get; set; }
        public List<ExpNode> Body { get; set; }

        public LetExpNode(List<ExpNode> decs, List<ExpNode> body)
        {
            Decs = decs;
            Body = body;
        }
    }

    public class BlockNode : ExpNode
    {
        public List<ExpNode> Expressions { get; set; }

        public BlockNode(List<ExpNode> expressions)
        {
            Expressions = expressions;
        }
    }

    public class BinaryExpNode : ExpNode
    {
        public string Op { get; set; }
        public ExpNode Left { get; set; }
        public ExpNode Right { get; set; }

        public BinaryExpNode(string op, ExpNode left, ExpNode right)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }

    public class UnaryExpNode : ExpNode
    {
        public string Op { get; set; }
        public ExpNode Operand { get; set; }

        public UnaryExpNode(string op, ExpNode operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    public class IfExpNode : ExpNode
    {
        public ExpNode Cond { get; set; }
        public List<ExpNode> Then { get; set; }
        public List<ExpNode>? Else { get; set; }

        public IfExpNode(
            ExpNode cond,
            List<ExpNode> thenBody,
            List<ExpNode>? elseBody = null)
        {
            Cond = cond;
            Then = thenBody;
            Else = elseBody;
        }
    }

    public class WhileExpNode : ExpNode
    {
        public ExpNode Cond { get; set; }
        public List<ExpNode> Body { get; set; }

        public WhileExpNode(ExpNode cond, List<ExpNode> body)
        {
            Cond = cond;
            Body = body;
        }
    }

    public class ForExpNode : ExpNode
    {
        public string VarName { get; set; }
        public ExpNode EscapeStart { get; set; }
        public ExpNode EscapeEnd { get; set; }
        public List<ExpNode> Body { get; set; }

        public ForExpNode(
            string varName,
            ExpNode start,
            ExpNode end,
            List<ExpNode> body)
        {
            VarName = varName;
            EscapeStart = start;
            EscapeEnd = end;
            Body = body;
        }
    }

    public class BreakExpNode : ExpNode
    {
    }

    public class ContinueExpNode : ExpNode
    {
    }

    public class VarAccessNode : ExpNode
    {
        public string Name { get; set; }

        public VarAccessNode(string name)
        {
            Name = name;
        }
    }

    public class ArrayLiteralNode : ExpNode
    {
        public List<ExpNode> Elements { get; set; }

        public ArrayLiteralNode(List<ExpNode> elements)
        {
            Elements = elements;
        }
    }

    public class ArrayAccessNode : ExpNode
    {
        public ExpNode Array { get; set; }
        public ExpNode Index { get; set; }

        public ArrayAccessNode(ExpNode array, ExpNode index)
        {
            Array = array;
            Index = index;
        }
    }

    public class FieldAccessNode : ExpNode
    {
        public ExpNode Target { get; set; }
        public string FieldName { get; set; }

        public FieldAccessNode(ExpNode target, string fieldName)
        {
            Target = target;
            FieldName = fieldName;
        }
    }

    public class StructInitNode : ExpNode
    {
        public string StructName { get; set; }
        public List<ExpNode> Args { get; set; }

        public StructInitNode(string structName, List<ExpNode> args)
        {
            StructName = structName;
            Args = args;
        }
    }

    public class TigerType
    {
        public string Name { get; }
        public TigerType? ElementType { get; }

        public TigerType(string name, TigerType? elementType = null)
        {
            Name = name;
            ElementType = elementType;
        }

        public bool IsArray => Name == "array";

        public override string ToString()
        {
            return IsArray && ElementType != null
                ? ElementType + "[]"
                : Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TigerType other)
                return false;

            if (IsArray != other.IsArray)
                return false;

            if (IsArray)
                return ElementType != null &&
                       other.ElementType != null &&
                       ElementType.Equals(other.ElementType);

            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, ElementType);
        }

        public static readonly TigerType Int = new("int");
        public static readonly TigerType String = new("string");
        public static readonly TigerType Bool = new("bool");
        public static readonly TigerType Void = new("void");

        public static TigerType ArrayOf(TigerType type)
        {
            return new TigerType("array", type);
        }
    }

    public class TigerFunction
    {
        public string Name { get; }
        public List<TigerType> Parameters { get; }
        public TigerType ReturnType { get; }

        public TigerFunction(
            string name,
            List<TigerType> parameters,
            TigerType returnType)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }

    public class TigerStruct
    {
        public string Name { get; }
        public Dictionary<string, TigerType> Fields { get; }

        public TigerStruct(
            string name,
            Dictionary<string, TigerType> fields)
        {
            Name = name;
            Fields = fields;
        }
    }
}