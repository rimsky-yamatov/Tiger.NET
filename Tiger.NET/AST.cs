using System.Collections.Generic;

namespace Tiger.NET
{
    public abstract class ExpNode { }

    public class StringLiteralNode : ExpNode
    {
        public string Value {; }
        public StringLiteralNode(string val) => Value = val;
    }

    public class IntLiteralNode : ExpNode
    {
        public int Value {; }
        public IntLiteralNode(int val) => Value = val;
    }

    public class VarDeclNode : ExpNode
    {
        public string Name {; }
        public ExpNode Init {; }
        public VarDeclNode(string name, ExpNode init) { Name = name; Init = init; }
    }

    public class CallExpNode : ExpNode
    {
        public string FuncName {; }
        public List<ExpNode> Args {; }
        public CallExpNode(string funcName, List<ExpNode> args) { FuncName = funcName; Args = args; }
    }

    public class LetExpNode : ExpNode
    {
        public List<ExpNode> Decs {; }
        public List<ExpNode> Body {; }
        public LetExpNode(List<ExpNode> decs, List<ExpNode> body) { Decs = decs; Body = body; }
    }

    public class BinaryExpNode : ExpNode
    {
        public string Op {; }
        public ExpNode Left {; }
        public ExpNode Right {; }
        public BinaryExpNode(string op, ExpNode left, ExpNode right) { Op = op; Left = left; Right = right; }
    }

    public class VarAccessNode : ExpNode
    {
        public string Name {; }
        public VarAccessNode(string name) => Name = name;
    }
}