using System.Collections.Generic;

namespace Tiger.NET
{
    public abstract class ExpNode
    {
        public TigerType? InferredType { get; set; }
    }

    public sealed class StringLiteralNode : ExpNode
    {
        public string Value { get; }
        public StringLiteralNode(string value) => Value = value;
    }

    public sealed class IntLiteralNode : ExpNode
    {
        public int Value { get; }
        public IntLiteralNode(int value) => Value = value;
    }

    public sealed class BoolLiteralNode : ExpNode
    {
        public bool Value { get; }
        public BoolLiteralNode(bool value) => Value = value;
    }

    public sealed class VarDeclNode : ExpNode
    {
        public string Name { get; }
        public string? TypeName { get; }
        public ExpNode Init { get; }

        public VarDeclNode(
            string name,
            ExpNode init,
            string? typeName = null)
        {
            Name = name;
            Init = init;
            TypeName = typeName;
        }
    }

    public sealed class FuncParam
    {
        public string Name { get; }
        public string TypeName { get; }

        public FuncParam(
            string name,
            string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    public sealed class FunctionDeclNode : ExpNode
    {
        public string Name { get; }
        public List<FuncParam> Params { get; }
        public string ReturnType { get; }
        public ExpNode Body { get; }

        public FunctionDeclNode(
            string name,
            List<FuncParam> parameters,
            string returnType,
            ExpNode body)
        {
            Name = name;
            Params = parameters;
            ReturnType = returnType;
            Body = body;
        }
    }

    public sealed class StructDeclNode : ExpNode
    {
        public string Name { get; }
        public List<StructField> Fields { get; }

        public StructDeclNode(
            string name,
            List<StructField> fields)
        {
            Name = name;
            Fields = fields;
        }
    }

    public sealed class StructField
    {
        public string Name { get; }
        public string TypeName { get; }

        public StructField(
            string name,
            string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    public sealed class StructInitNode : ExpNode
    {
        public string TypeName { get; }
        public List<ExpNode> Args { get; }

        public StructInitNode(
            string typeName,
            List<ExpNode> args)
        {
            TypeName = typeName;
            Args = args;
        }
    }

    public sealed class FieldAccessNode : ExpNode
    {
        public ExpNode Target { get; }
        public string FieldName { get; }

        public FieldAccessNode(
            ExpNode target,
            string fieldName)
        {
            Target = target;
            FieldName = fieldName;
        }
    }

    public sealed class ArrayLiteralNode : ExpNode
    {
        public List<ExpNode> Elements { get; }

        public ArrayLiteralNode(
            List<ExpNode> elements)
        {
            Elements = elements;
        }
    }

    public sealed class ArrayAccessNode : ExpNode
    {
        public ExpNode Array { get; }
        public ExpNode Index { get; }

        public ArrayAccessNode(
            ExpNode array,
            ExpNode index)
        {
            Array = array;
            Index = index;
        }
    }

    public sealed class AssignNode : ExpNode
    {
        public string Name { get; }
        public ExpNode Value { get; }

        public AssignNode(
            string name,
            ExpNode value)
        {
            Name = name;
            Value = value;
        }
    }

    public sealed class ArrayAssignNode : ExpNode
    {
        public ExpNode Array { get; }
        public ExpNode Index { get; }
        public ExpNode Value { get; }

        public ArrayAssignNode(
            ExpNode array,
            ExpNode index,
            ExpNode value)
        {
            Array = array;
            Index = index;
            Value = value;
        }
    }

    public sealed class FieldAssignNode : ExpNode
    {
        public ExpNode Target { get; }
        public string FieldName { get; }
        public ExpNode Value { get; }

        public FieldAssignNode(
            ExpNode target,
            string fieldName,
            ExpNode value)
        {
            Target = target;
            FieldName = fieldName;
            Value = value;
        }
    }

    public sealed class CallExpNode : ExpNode
    {
        public string FuncName { get; }
        public List<ExpNode> Args { get; }

        public CallExpNode(
            string funcName,
            List<ExpNode> args)
        {
            FuncName = funcName;
            Args = args;
        }
    }

    public sealed class LetExpNode : ExpNode
    {
        public List<ExpNode> Decs { get; }
        public List<ExpNode> Body { get; }

        public LetExpNode(
            List<ExpNode> decs,
            List<ExpNode> body)
        {
            Decs = decs;
            Body = body;
        }
    }

    public sealed class BinaryExpNode : ExpNode
    {
        public string Op { get; }
        public ExpNode Left { get; }
        public ExpNode Right { get; }

        public BinaryExpNode(
            string op,
            ExpNode left,
            ExpNode right)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }

    public sealed class UnaryExpNode : ExpNode
    {
        public string Op { get; }
        public ExpNode Operand { get; }

        public UnaryExpNode(
            string op,
            ExpNode operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    public sealed class IfExpNode : ExpNode
    {
        public ExpNode Cond { get; }
        public List<ExpNode> ThenBody { get; }
        public List<ExpNode> ElseBody { get; }

        public bool HasElse =>
            ElseBody.Count > 0;

        public IfExpNode(
            ExpNode cond,
            List<ExpNode> thenBody,
            List<ExpNode>? elseBody = null)
        {
            Cond = cond;
            ThenBody = thenBody;
            ElseBody =
                elseBody ??
                new List<ExpNode>();
        }
    }

    public sealed class WhileExpNode : ExpNode
    {
        public ExpNode Cond { get; }
        public List<ExpNode> Body { get; }

        public WhileExpNode(
            ExpNode cond,
            List<ExpNode> body)
        {
            Cond = cond;
            Body = body;
        }
    }

    public sealed class ForExpNode : ExpNode
    {
        public string VarName { get; }
        public ExpNode EscapeStart { get; }
        public ExpNode EscapeEnd { get; }
        public List<ExpNode> Body { get; }

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

    public sealed class BreakExpNode : ExpNode
    {
    }

    public sealed class ContinueExpNode : ExpNode
    {
    }

    public sealed class VarAccessNode : ExpNode
    {
        public string Name { get; }

        public VarAccessNode(
            string name)
        {
            Name = name;
        }
    }

    public sealed class SequenceExpNode : ExpNode
    {
        public List<ExpNode> Expressions { get; }

        public SequenceExpNode(
            List<ExpNode> expressions)
        {
            Expressions = expressions;
        }
    }
}
