using System.Collections.Generic;

namespace Tiger.NET
{
    public abstract class ExpNode { }

    public class StringLiteralNode : ExpNode
    {
        public string Value { get; set; }
        public StringLiteralNode(string val) => Value = val;
    }

    public class IntLiteralNode : ExpNode
    {
        public int Value { get; set; }
        public IntLiteralNode(int val) => Value = val;
    }

    public class VarDeclNode : ExpNode
    {
        public string Name { get; set; }
        public ExpNode Init { get; set; }
        public VarDeclNode(string name, ExpNode init) { Name = name; Init = init; }
    }

    public class FuncParam
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public FuncParam(string name, string typeName) { Name = name; TypeName = typeName; }
    }

    public class FunctionDeclNode : ExpNode
    {
        public string Name { get; set; }
        public List<FuncParam> Params { get; set; }
        public string ReturnType { get; set; }
        public ExpNode Body { get; set; }
        public FunctionDeclNode(string name, List<FuncParam> @params, string returnType, ExpNode body)
        {
            Name = name; Params = @params; ReturnType = returnType; Body = body;
        }
    }

    public class AssignNode : ExpNode
    {
        public string VarName { get; set; }
        public ExpNode Value { get; set; }
        public AssignNode(string varName, ExpNode val) { VarName = varName; Value = val; }
    }

    public class CallExpNode : ExpNode
    {
        public string FuncName { get; set; }
        public List<ExpNode> Args { get; set; }
        public CallExpNode(string funcName, List<ExpNode> args) { FuncName = funcName; Args = args; }
    }

    public class LetExpNode : ExpNode
    {
        public List<ExpNode> Decs { get; set; }
        public List<ExpNode> Body { get; set; }
        public LetExpNode(List<ExpNode> decs, List<ExpNode> body) { Decs = decs; Body = body; }
    }

    public class BinaryExpNode : ExpNode
    {
        public string Op { get; set; }
        public ExpNode Left { get; set; }
        public ExpNode Right { get; set; }
        public BinaryExpNode(string op, ExpNode left, ExpNode right) { Op = op; Left = left; Right = right; }
    }

    public class IfExpNode : ExpNode
    {
        public ExpNode Cond { get; set; }
        public ExpNode Then { get; set; }
        public ExpNode? Else { get; set; }
        public IfExpNode(ExpNode cond, ExpNode thenExp, ExpNode? elseExp = null)
        {
            Cond = cond; Then = thenExp; Else = elseExp;
        }
    }

    public class WhileExpNode : ExpNode
    {
        public ExpNode Cond { get; set; }
        public ExpNode Body { get; set; }
        public WhileExpNode(ExpNode cond, ExpNode body) { Cond = cond; Body = body; }
    }

    public class ForExpNode : ExpNode
    {
        public string VarName { get; set; }
        public ExpNode EscapeStart { get; set; }
        public ExpNode EscapeEnd { get; set; }
        public ExpNode Body { get; set; }
        public ForExpNode(string varName, ExpNode start, ExpNode end, ExpNode body)
        {
            VarName = varName; EscapeStart = start; EscapeEnd = end; Body = body;
        }
    }

    public class BreakExpNode : ExpNode { }

    public class VarAccessNode : ExpNode
    {
        public string Name { get; set; }
        public VarAccessNode(string name) => Name = name;
    }
}