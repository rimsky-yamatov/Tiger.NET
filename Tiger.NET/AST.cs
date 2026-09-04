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

    public class VarAccessNode : ExpNode
    {
        public string Name { get; set; }
        public VarAccessNode(string name) => Name = name;
    }
}