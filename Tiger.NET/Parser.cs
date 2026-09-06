using System;
using System.Collections.Generic;

namespace Tiger.NET
{
    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _idx;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _idx = 0;
        }

        private Token Current =>
            _idx < _tokens.Count
                ? _tokens[_idx]
                : _tokens[^1];

        private Token Peek(int offset)
        {
            int index = _idx + offset;

            if (index < 0 ||
                index >= _tokens.Count)
            {
                return _tokens[^1];
            }

            return _tokens[index];
        }

        private bool Check(TokenType type)
        {
            return Current.Type == type;
        }

        private bool Match(TokenType type)
        {
            if (!Check(type))
                return false;

            _idx++;
            return true;
        }

        private Token Consume(TokenType type)
        {
            if (!Check(type))
            {
                throw new Exception(
                    $"Syntax Error: Expected {type}, got {Current.Type} ('{Current.Value}') at {Current.Line}:{Current.Column}");
            }

            return _tokens[_idx++];
        }

        public ExpNode Parse()
        {
            ExpNode result = ParseExp();

            while (Match(TokenType.Semicolon))
            {
            }

            if (!Check(TokenType.EOF))
            {
                throw new Exception(
                    $"Syntax Error: unexpected token '{Current.Value}' at {Current.Line}:{Current.Column}");
            }

            return result;
        }

        private ExpNode ParseExp()
        {
            switch (Current.Type)
            {
                case TokenType.Let:
                    return ParseLet();

                case TokenType.If:
                    return ParseIf();

                case TokenType.While:
                    return ParseWhile();

                case TokenType.For:
                    return ParseFor();

                case TokenType.Break:
                    Consume(TokenType.Break);
                    return new BreakExpNode();

                case TokenType.Continue:
                    Consume(TokenType.Continue);
                    return new ContinueExpNode();

                default:
                    return ParseAssignment();
            }
        }

        private ExpNode ParseLet()
        {
            Consume(TokenType.Let);

            var declarations =
                new List<ExpNode>();

            while (!Check(TokenType.In))
            {
                if (Check(TokenType.EOF))
                {
                    throw new Exception(
                        "Syntax Error: expected 'in'");
                }

                if (Match(TokenType.Semicolon))
                    continue;

                switch (Current.Type)
                {
                    case TokenType.Var:
                        declarations.Add(
                            ParseVarDeclaration());
                        break;

                    case TokenType.Function:
                        declarations.Add(
                            ParseFunctionDeclaration());
                        break;

                    case TokenType.Struct:
                        declarations.Add(
                            ParseStructDeclaration());
                        break;

                    default:
                        throw new Exception(
                            $"Syntax Error: unexpected declaration token '{Current.Value}'");
                }

                Match(TokenType.Semicolon);
            }

            Consume(TokenType.In);

            var body =
                ParseStatementList(
                    TokenType.End);

            Consume(TokenType.End);

            return new LetExpNode(
                declarations,
                body);
        }

        private VarDeclNode ParseVarDeclaration()
        {
            Consume(TokenType.Var);

            string name =
                Consume(TokenType.Identifier).Value;

            string? typeName = null;

            if (Match(TokenType.Colon))
            {
                typeName =
                    ParseTypeName();
            }

            Consume(TokenType.Assign);

            ExpNode init =
                ParseExp();

            return new VarDeclNode(
                name,
                init,
                typeName);
        }

        private FunctionDeclNode ParseFunctionDeclaration()
        {
            Consume(TokenType.Function);

            string name =
                Consume(TokenType.Identifier).Value;

            Consume(TokenType.LParen);

            var parameters =
                new List<FuncParam>();

            if (!Check(TokenType.RParen))
            {
                while (true)
                {
                    string parameterName =
                        Consume(TokenType.Identifier).Value;

                    Consume(TokenType.Colon);

                    string parameterType =
                        ParseTypeName();

                    parameters.Add(
                        new FuncParam(
                            parameterName,
                            parameterType));

                    if (!Match(TokenType.Comma))
                        break;
                }
            }

            Consume(TokenType.RParen);

            string returnType = "void";

            if (Match(TokenType.Colon))
            {
                returnType =
                    ParseTypeName();
            }

            Consume(TokenType.Equal);

            ExpNode body;

            if (Check(TokenType.If) ||
                Check(TokenType.Let))
            {
                body = ParseExp();
            }
            else
            {
                var expressions =
                    new List<ExpNode>();

                expressions.Add(
                    ParseExp());

                while (Match(TokenType.Semicolon))
                {
                    if (Check(TokenType.End) ||
                        Check(TokenType.Else) ||
                        Check(TokenType.EOF))
                    {
                        break;
                    }

                    expressions.Add(
                        ParseExp());
                }

                body =
                    expressions.Count == 1
                        ? expressions[0]
                        : new SequenceExpNode(
                            expressions);
            }

            return new FunctionDeclNode(
                name,
                parameters,
                returnType,
                body);
        }

        private StructDeclNode ParseStructDeclaration()
        {
            Consume(TokenType.Struct);

            string name =
                Consume(TokenType.Identifier).Value;

            Consume(TokenType.LBrace);

            var fields =
                new List<StructField>();

            while (!Check(TokenType.RBrace))
            {
                string fieldName =
                    Consume(TokenType.Identifier).Value;

                Consume(TokenType.Colon);

                string fieldType =
                    ParseTypeName();

                fields.Add(
                    new StructField(
                        fieldName,
                        fieldType));

                Match(TokenType.Semicolon);

                if (Check(TokenType.RBrace))
                    break;
            }

            Consume(TokenType.RBrace);

            return new StructDeclNode(
                name,
                fields);
        }

        private string ParseTypeName()
        {
            string name =
                Consume(TokenType.Identifier).Value;

            while (Match(TokenType.LBracket))
            {
                Consume(TokenType.RBracket);
                name += "[]";
            }

            return name;
        }

        private ExpNode ParseIf()
        {
            Consume(TokenType.If);

            ExpNode condition =
                ParseExp();

            Consume(TokenType.Then);

            var thenBody =
                new List<ExpNode>();

            if (Match(TokenType.Semicolon))
            {
            }

            if (Check(TokenType.End) ||
                Check(TokenType.Else))
            {
                throw new Exception(
                    "Syntax Error: empty 'then' body");
            }

            thenBody.Add(
                ParseExp());

            while (Match(TokenType.Semicolon))
            {
                if (Check(TokenType.End) ||
                    Check(TokenType.Else) ||
                    Check(TokenType.EOF))
                {
                    break;
                }

                thenBody.Add(
                    ParseExp());
            }

            var elseBody =
                new List<ExpNode>();

            if (Match(TokenType.Else))
            {
                if (Match(TokenType.Semicolon))
                {
                }

                while (!Check(TokenType.End) &&
                       !Check(TokenType.EOF))
                {
                    elseBody.Add(
                        ParseExp());

                    if (!Match(TokenType.Semicolon))
                        break;
                }
            }

            if (Check(TokenType.End))
                Consume(TokenType.End);

            return new IfExpNode(
                condition,
                thenBody,
                elseBody);
        }

        private ExpNode ParseWhile()
        {
            Consume(TokenType.While);

            ExpNode condition =
                ParseExp();

            Consume(TokenType.Do);

            var body =
                new List<ExpNode>();

            if (Match(TokenType.Semicolon))
            {
            }

            body.Add(
                ParseExp());

            while (Match(TokenType.Semicolon))
            {
                if (Check(TokenType.End) ||
                    Check(TokenType.EOF))
                {
                    break;
                }

                body.Add(
                    ParseExp());
            }

            if (Check(TokenType.End))
                Consume(TokenType.End);

            return new WhileExpNode(
                condition,
                body);
        }

        private ExpNode ParseFor()
        {
            Consume(TokenType.For);

            string variable =
                Consume(TokenType.Identifier).Value;

            Consume(TokenType.Assign);

            ExpNode start =
                ParseExp();

            Consume(TokenType.To);

            ExpNode end =
                ParseExp();

            Consume(TokenType.Do);

            var body =
                new List<ExpNode>();

            if (Match(TokenType.Semicolon))
            {
            }

            body.Add(
                ParseExp());

            while (Match(TokenType.Semicolon))
            {
                if (Check(TokenType.End) ||
                    Check(TokenType.EOF))
                {
                    break;
                }

                body.Add(
                    ParseExp());
            }

            if (Check(TokenType.End))
                Consume(TokenType.End);

            return new ForExpNode(
                variable,
                start,
                end,
                body);
        }

        private List<ExpNode> ParseStatementList(
            TokenType terminator)
        {
            var result =
                new List<ExpNode>();

            while (!Check(terminator) &&
                   !Check(TokenType.EOF))
            {
                if (Match(TokenType.Semicolon))
                    continue;

                result.Add(
                    ParseExp());

                Match(TokenType.Semicolon);
            }

            return result;
        }

        private ExpNode ParseAssignment()
        {
            ExpNode left =
                ParseBinaryOr();

            if (Match(TokenType.Assign))
            {
                ExpNode value =
                    ParseExp();

                if (left is VarAccessNode)
                {
                    return new AssignNode(
                        ((VarAccessNode)left).Name,
                        value);
                }

                if (left is ArrayAccessNode array)
                {
                    return new ArrayAssignNode(
                        array.Array,
                        array.Index,
                        value);
                }

                if (left is FieldAccessNode field)
                {
                    return new FieldAssignNode(
                        field.Target,
                        field.FieldName,
                        value);
                }

                throw new Exception(
                    "Syntax Error: left side of assignment is not assignable");
            }

            return left;
        }

        private ExpNode ParseBinaryOr()
        {
            ExpNode left =
                ParseBinaryAnd();

            while (Match(TokenType.Or))
            {
                left =
                    new BinaryExpNode(
                        "or",
                        left,
                        ParseBinaryAnd());
            }

            return left;
        }

        private ExpNode ParseBinaryAnd()
        {
            ExpNode left =
                ParseEquality();

            while (Match(TokenType.And))
            {
                left =
                    new BinaryExpNode(
                        "and",
                        left,
                        ParseEquality());
            }

            return left;
        }

        private ExpNode ParseEquality()
        {
            ExpNode left =
                ParseComparison();

            while (Check(TokenType.Equal) ||
                   Check(TokenType.NotEqual))
            {
                string op =
                    Current.Value;

                _idx++;

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        ParseComparison());
            }

            return left;
        }

        private ExpNode ParseComparison()
        {
            ExpNode left =
                ParseTerm();

            while (Check(TokenType.LessThan) ||
                   Check(TokenType.LessEqual) ||
                   Check(TokenType.GreaterThan) ||
                   Check(TokenType.GreaterEqual))
            {
                string op =
                    Current.Value;

                _idx++;

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        ParseTerm());
            }

            return left;
        }

        private ExpNode ParseTerm()
        {
            ExpNode left =
                ParseFactor();

            while (Check(TokenType.Plus) ||
                   Check(TokenType.Minus))
            {
                string op =
                    Current.Value;

                _idx++;

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        ParseFactor());
            }

            return left;
        }

        private ExpNode ParseFactor()
        {
            ExpNode left =
                ParseUnary();

            while (Check(TokenType.Multiply) ||
                   Check(TokenType.Divide) ||
                   Check(TokenType.Modulo))
            {
                string op =
                    Current.Value;

                _idx++;

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        ParseUnary());
            }

            return left;
        }

        private ExpNode ParseUnary()
        {
            if (Match(TokenType.Minus))
            {
                return new UnaryExpNode(
                    "-",
                    ParseUnary());
            }

            return ParsePostfix();
        }

        private ExpNode ParsePostfix()
        {
            ExpNode expression =
                ParsePrimary();

            while (true)
            {
                if (Match(TokenType.LBracket))
                {
                    ExpNode index =
                        ParseExp();

                    Consume(TokenType.RBracket);

                    expression =
                        new ArrayAccessNode(
                            expression,
                            index);

                    continue;
                }

                if (Match(TokenType.Dot))
                {
                    string field =
                        Consume(TokenType.Identifier).Value;

                    expression =
                        new FieldAccessNode(
                            expression,
                            field);

                    continue;
                }

                break;
            }

            return expression;
        }

        private ExpNode ParsePrimary()
        {
            if (Check(TokenType.Int))
            {
                int value =
                    int.Parse(
                        Consume(TokenType.Int).Value);

                return new IntLiteralNode(
                    value);
            }

            if (Check(TokenType.String))
            {
                return new StringLiteralNode(
                    Consume(TokenType.String).Value);
            }

            if (Match(TokenType.True))
                return new BoolLiteralNode(true);

            if (Match(TokenType.False))
                return new BoolLiteralNode(false);

            if (Check(TokenType.Identifier))
            {
                string name =
                    Consume(TokenType.Identifier).Value;

                if (Match(TokenType.LParen))
                {
                    var args =
                        new List<ExpNode>();

                    if (!Check(TokenType.RParen))
                    {
                        while (true)
                        {
                            args.Add(
                                ParseExp());

                            if (!Match(TokenType.Comma))
                                break;
                        }
                    }

                    Consume(TokenType.RParen);

                    return new CallExpNode(
                        name,
                        args);
                }

                return new VarAccessNode(
                    name);
            }

            if (Match(TokenType.LBracket))
            {
                var elements =
                    new List<ExpNode>();

                if (!Check(TokenType.RBracket))
                {
                    while (true)
                    {
                        elements.Add(
                            ParseExp());

                        if (!Match(TokenType.Comma))
                            break;
                    }
                }

                Consume(TokenType.RBracket);

                return new ArrayLiteralNode(
                    elements);
            }

            if (Match(TokenType.LParen))
            {
                ExpNode expression =
                    ParseExp();

                Consume(TokenType.RParen);

                return expression;
            }

            throw new Exception(
                $"Syntax Error: unexpected token '{Current.Value}' at {Current.Line}:{Current.Column}");
        }
    }
}