using System;
using System.Collections.Generic;

namespace Tiger.NET
{
    public class Parser
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
            return index < _tokens.Count
                ? _tokens[index]
                : _tokens[^1];
        }

        private Token Consume(TokenType type)
        {
            if (Current.Type != type)
            {
                throw new Exception(
                    $"Syntax Error at {Current.Line}:{Current.Column}: expected {type}, got {Current.Type} ('{Current.Value}')");
            }

            return _tokens[_idx++];
        }

        public ExpNode Parse()
        {
            ExpNode result = ParseExp();

            while (Current.Type == TokenType.Semicolon)
                Consume(TokenType.Semicolon);

            Consume(TokenType.EOF);

            return result;
        }

        private ExpNode ParseExp()
        {
            if (Current.Type == TokenType.Let)
                return ParseLet();

            if (Current.Type == TokenType.If)
                return ParseIf();

            if (Current.Type == TokenType.While)
                return ParseWhile();

            if (Current.Type == TokenType.For)
                return ParseFor();

            if (Current.Type == TokenType.Break)
            {
                Consume(TokenType.Break);
                return new BreakExpNode();
            }

            if (Current.Type == TokenType.Continue)
            {
                Consume(TokenType.Continue);
                return new ContinueExpNode();
            }

            return ParseAssignment();
        }

        private ExpNode ParseLet()
        {
            Consume(TokenType.Let);

            var declarations = new List<ExpNode>();

            while (Current.Type != TokenType.In)
            {
                if (Current.Type == TokenType.Var)
                {
                    declarations.Add(ParseVarDecl());
                    ConsumeOptionalSemicolon();
                }
                else if (Current.Type == TokenType.Function)
                {
                    declarations.Add(ParseFunctionDecl());
                    ConsumeOptionalSemicolon();
                }
                else if (Current.Type == TokenType.Struct)
                {
                    declarations.Add(ParseStructDecl());
                    ConsumeOptionalSemicolon();
                }
                else
                {
                    throw Error("expected declaration");
                }
            }

            Consume(TokenType.In);

            var body = ParseBlockUntil(TokenType.End);
            Consume(TokenType.End);

            return new LetExpNode(declarations, body);
        }

        private VarDeclNode ParseVarDecl()
        {
            Consume(TokenType.Var);

            string name = Consume(TokenType.Identifier).Value;

            string? typeName = null;

            if (Current.Type == TokenType.Colon)
            {
                Consume(TokenType.Colon);
                typeName = ParseTypeName();
            }

            Consume(TokenType.Assign);

            ExpNode init = ParseExp();

            return new VarDeclNode(name, init, typeName);
        }

        private FunctionDeclNode ParseFunctionDecl()
        {
            Consume(TokenType.Function);

            string name = Consume(TokenType.Identifier).Value;

            Consume(TokenType.LParen);

            var parameters = new List<FuncParam>();

            if (Current.Type != TokenType.RParen)
            {
                while (true)
                {
                    string parameterName =
                        Consume(TokenType.Identifier).Value;

                    Consume(TokenType.Colon);

                    string parameterType = ParseTypeName();

                    parameters.Add(
                        new FuncParam(parameterName, parameterType));

                    if (Current.Type != TokenType.Comma)
                        break;

                    Consume(TokenType.Comma);
                }
            }

            Consume(TokenType.RParen);

            string returnType = "void";

            if (Current.Type == TokenType.Colon)
            {
                Consume(TokenType.Colon);
                returnType = ParseTypeName();
            }

            Consume(TokenType.Equal);

            var body = new List<ExpNode>();

            if (Current.Type == TokenType.Let)
            {
                body.Add(ParseLet());
            }
            else
            {
                body.Add(ParseExp());
            }

            return new FunctionDeclNode(
                name,
                parameters,
                returnType,
                body);
        }

        private StructDeclNode ParseStructDecl()
        {
            Consume(TokenType.Struct);

            string name =
                Consume(TokenType.Identifier).Value;

            Consume(TokenType.LBrace);

            var fields = new List<StructField>();

            while (Current.Type != TokenType.RBrace)
            {
                string fieldName =
                    Consume(TokenType.Identifier).Value;

                Consume(TokenType.Colon);

                string fieldType = ParseTypeName();

                fields.Add(
                    new StructField(fieldName, fieldType));

                if (Current.Type == TokenType.Comma ||
                    Current.Type == TokenType.Semicolon)
                {
                    _idx++;
                }
                else if (Current.Type != TokenType.RBrace)
                {
                    throw Error("expected ',' or '}'");
                }
            }

            Consume(TokenType.RBrace);

            return new StructDeclNode(name, fields);
        }

        private ExpNode ParseIf()
        {
            Consume(TokenType.If);

            ExpNode cond = ParseExp();

            Consume(TokenType.Then);

            var thenBody =
                ParseBlockUntil(TokenType.Else, TokenType.End);

            List<ExpNode>? elseBody = null;

            if (Current.Type == TokenType.Else)
            {
                Consume(TokenType.Else);

                elseBody =
                    ParseBlockUntil(TokenType.End);
            }

            Consume(TokenType.End);

            return new IfExpNode(
                cond,
                thenBody,
                elseBody);
        }

        private ExpNode ParseWhile()
        {
            Consume(TokenType.While);

            ExpNode cond = ParseExp();

            Consume(TokenType.Do);

            var body =
                ParseBlockUntil(TokenType.End);

            Consume(TokenType.End);

            return new WhileExpNode(cond, body);
        }

        private ExpNode ParseFor()
        {
            Consume(TokenType.For);

            string variable =
                Consume(TokenType.Identifier).Value;

            Consume(TokenType.Assign);

            ExpNode start = ParseExp();

            Consume(TokenType.To);

            ExpNode end = ParseExp();

            Consume(TokenType.Do);

            var body =
                ParseBlockUntil(TokenType.End);

            Consume(TokenType.End);

            return new ForExpNode(
                variable,
                start,
                end,
                body);
        }

        private List<ExpNode> ParseBlockUntil(params TokenType[] terminators)
        {
            var result = new List<ExpNode>();

            while (Array.IndexOf(terminators, Current.Type) < 0)
            {
                if (Current.Type == TokenType.EOF)
                    throw Error("unexpected end of file");

                result.Add(ParseExp());

                ConsumeOptionalSemicolon();
            }

            return result;
        }

        private ExpNode ParseAssignment()
        {
            ExpNode left = ParseBinaryExp();

            if (Current.Type == TokenType.Assign)
            {
                Consume(TokenType.Assign);

                ExpNode value = ParseExp();

                return new AssignNode(left, value);
            }

            return left;
        }

        private ExpNode ParseBinaryExp()
        {
            return ParseOr();
        }

        private ExpNode ParseOr()
        {
            ExpNode left = ParseAnd();

            while (Current.Type == TokenType.Or)
            {
                Consume(TokenType.Or);
                left = new BinaryExpNode("or", left, ParseAnd());
            }

            return left;
        }

        private ExpNode ParseAnd()
        {
            ExpNode left = ParseEquality();

            while (Current.Type == TokenType.And)
            {
                Consume(TokenType.And);
                left = new BinaryExpNode("and", left, ParseEquality());
            }

            return left;
        }

        private ExpNode ParseEquality()
        {
            ExpNode left = ParseComparison();

            while (Current.Type == TokenType.Equal ||
                   Current.Type == TokenType.NotEqual)
            {
                string op = Current.Value;
                _idx++;

                left = new BinaryExpNode(
                    op,
                    left,
                    ParseComparison());
            }

            return left;
        }

        private ExpNode ParseComparison()
        {
            ExpNode left = ParseTerm();

            while (Current.Type == TokenType.LessThan ||
                   Current.Type == TokenType.LessEqual ||
                   Current.Type == TokenType.GreaterThan ||
                   Current.Type == TokenType.GreaterEqual)
            {
                string op = Current.Value;
                _idx++;

                left = new BinaryExpNode(
                    op,
                    left,
                    ParseTerm());
            }

            return left;
        }

        private ExpNode ParseTerm()
        {
            ExpNode left = ParseFactor();

            while (Current.Type == TokenType.Plus ||
                   Current.Type == TokenType.Minus)
            {
                string op = Current.Value;
                _idx++;

                left = new BinaryExpNode(
                    op,
                    left,
                    ParseFactor());
            }

            return left;
        }

        private ExpNode ParseFactor()
        {
            ExpNode left = ParseUnary();

            while (Current.Type == TokenType.Multiply ||
                   Current.Type == TokenType.Divide ||
                   Current.Type == TokenType.Modulo)
            {
                string op = Current.Value;
                _idx++;

                left = new BinaryExpNode(
                    op,
                    left,
                    ParseUnary());
            }

            return left;
        }

        private ExpNode ParseUnary()
        {
            if (Current.Type == TokenType.Minus)
            {
                Consume(TokenType.Minus);
                return new UnaryExpNode("-", ParseUnary());
            }

            return ParsePostfix();
        }

        private ExpNode ParsePostfix()
        {
            ExpNode expression = ParsePrimary();

            while (true)
            {
                if (Current.Type == TokenType.LBracket)
                {
                    Consume(TokenType.LBracket);

                    ExpNode index = ParseExp();

                    Consume(TokenType.RBracket);

                    expression =
                        new ArrayAccessNode(
                            expression,
                            index);

                    continue;
                }

                if (Current.Type == TokenType.Dot)
                {
                    Consume(TokenType.Dot);

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
            if (Current.Type == TokenType.Int)
            {
                int value =
                    int.Parse(Current.Value);

                Consume(TokenType.Int);

                return new IntLiteralNode(value);
            }

            if (Current.Type == TokenType.String)
            {
                string value = Current.Value;

                Consume(TokenType.String);

                return new StringLiteralNode(value);
            }

            if (Current.Type == TokenType.True)
            {
                Consume(TokenType.True);
                return new BoolLiteralNode(true);
            }

            if (Current.Type == TokenType.False)
            {
                Consume(TokenType.False);
                return new BoolLiteralNode(false);
            }

            if (Current.Type == TokenType.Identifier)
            {
                string name =
                    Consume(TokenType.Identifier).Value;

                if (Current.Type == TokenType.LParen)
                {
                    Consume(TokenType.LParen);

                    var args = new List<ExpNode>();

                    if (Current.Type != TokenType.RParen)
                    {
                        args.Add(ParseExp());

                        while (Current.Type == TokenType.Comma)
                        {
                            Consume(TokenType.Comma);
                            args.Add(ParseExp());
                        }
                    }

                    Consume(TokenType.RParen);

                    return new CallExpNode(name, args);
                }

                return new VarAccessNode(name);
            }

            if (Current.Type == TokenType.LBracket)
            {
                Consume(TokenType.LBracket);

                var elements = new List<ExpNode>();

                if (Current.Type != TokenType.RBracket)
                {
                    elements.Add(ParseExp());

                    while (Current.Type == TokenType.Comma)
                    {
                        Consume(TokenType.Comma);
                        elements.Add(ParseExp());
                    }
                }

                Consume(TokenType.RBracket);

                return new ArrayLiteralNode(elements);
            }

            if (Current.Type == TokenType.LParen)
            {
                Consume(TokenType.LParen);

                ExpNode expression = ParseExp();

                Consume(TokenType.RParen);

                return expression;
            }

            throw Error(
                $"unexpected token '{Current.Value}'");
        }

        private string ParseTypeName()
        {
            string type =
                Consume(TokenType.Identifier).Value;

            if (Current.Type == TokenType.LBracket)
            {
                Consume(TokenType.LBracket);
                Consume(TokenType.RBracket);
                type += "[]";
            }

            return type;
        }

        private void ConsumeOptionalSemicolon()
        {
            if (Current.Type == TokenType.Semicolon)
                Consume(TokenType.Semicolon);
        }

        private Exception Error(string message)
        {
            return new Exception(
                $"Syntax Error at {Current.Line}:{Current.Column}: {message}");
        }
    }
}
