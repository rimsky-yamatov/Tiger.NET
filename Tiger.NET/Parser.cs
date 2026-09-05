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

        private Token Consume(TokenType type)
        {
            if (Current.Type != type)
            {
                throw new Exception(
                    $"Syntax Error: Expected {type}, " +
                    $"got {Current.Type} ('{Current.Value}').");
            }

            var token = Current;
            _idx++;
            return token;
        }

        public ExpNode Parse()
        {
            while (Current.Type == TokenType.Semicolon)
                Consume(TokenType.Semicolon);

            ExpNode result = ParseExp();

            while (Current.Type == TokenType.Semicolon)
                Consume(TokenType.Semicolon);

            if (Current.Type != TokenType.EOF)
            {
                throw new Exception(
                    $"Syntax Error: Unexpected token " +
                    $"'{Current.Value}' after expression.");
            }

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

            return ParseAssignOrBinary();
        }

        private ExpNode ParseLet()
        {
            Consume(TokenType.Let);

            var declarations = new List<ExpNode>();

            while (Current.Type != TokenType.In)
            {
                if (Current.Type == TokenType.EOF)
                    throw new Exception(
                        "Syntax Error: Expected 'in'.");

                if (Current.Type == TokenType.Var)
                {
                    Consume(TokenType.Var);

                    string name =
                        Consume(TokenType.Identifier).Value;

                    string? type = null;

                    if (Current.Type == TokenType.Colon)
                    {
                        Consume(TokenType.Colon);

                        type =
                            Consume(TokenType.Identifier).Value;
                    }

                    Consume(TokenType.Assign);

                    ExpNode init = ParseExp();

                    declarations.Add(
                        new VarDeclNode(
                            name,
                            type,
                            init));

                    if (Current.Type == TokenType.Semicolon)
                        Consume(TokenType.Semicolon);

                    continue;
                }

                if (Current.Type == TokenType.Function)
                {
                    declarations.Add(
                        ParseFunctionDecl());

                    if (Current.Type == TokenType.Semicolon)
                        Consume(TokenType.Semicolon);

                    continue;
                }

                throw new Exception(
                    $"Syntax Error: Unexpected token " +
                    $"'{Current.Value}' in let declarations.");
            }

            Consume(TokenType.In);

            var body = new List<ExpNode>();

            while (Current.Type != TokenType.End)
            {
                if (Current.Type == TokenType.EOF)
                    throw new Exception(
                        "Syntax Error: Expected 'end'.");

                if (Current.Type == TokenType.Semicolon)
                {
                    Consume(TokenType.Semicolon);
                    continue;
                }

                body.Add(ParseExp());

                if (Current.Type == TokenType.Semicolon)
                    Consume(TokenType.Semicolon);
            }

            Consume(TokenType.End);

            return new LetExpNode(
                declarations,
                body);
        }

        private FunctionDeclNode ParseFunctionDecl()
        {
            Consume(TokenType.Function);

            string name =
                Consume(TokenType.Identifier).Value;

            Consume(TokenType.LParen);

            var parameters = new List<FuncParam>();

            if (Current.Type != TokenType.RParen)
            {
                while (true)
                {
                    string parameterName =
                        Consume(TokenType.Identifier).Value;

                    Consume(TokenType.Colon);

                    string parameterType =
                        Consume(TokenType.Identifier).Value;

                    parameters.Add(
                        new FuncParam(
                            parameterName,
                            parameterType));

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

                returnType =
                    Consume(TokenType.Identifier).Value;
            }

            Consume(TokenType.Equal);

            ExpNode body = ParseExp();

            return new FunctionDeclNode(
                name,
                parameters,
                returnType,
                body);
        }

        private ExpNode ParseIf()
        {
            Consume(TokenType.If);

            ExpNode condition = ParseExp();

            Consume(TokenType.Then);

            ExpNode thenExp = ParseExp();

            ExpNode? elseExp = null;

            if (Current.Type == TokenType.Else)
            {
                Consume(TokenType.Else);
                elseExp = ParseExp();
            }

            return new IfExpNode(
                condition,
                thenExp,
                elseExp);
        }

        private ExpNode ParseWhile()
        {
            Consume(TokenType.While);

            ExpNode condition = ParseExp();

            Consume(TokenType.Do);

            var body = new List<ExpNode>();

            while (Current.Type != TokenType.End)
            {
                if (Current.Type == TokenType.EOF)
                    throw new Exception(
                        "Syntax Error: Expected 'end'.");

                if (Current.Type == TokenType.Semicolon)
                {
                    Consume(TokenType.Semicolon);
                    continue;
                }

                body.Add(ParseExp());

                if (Current.Type == TokenType.Semicolon)
                    Consume(TokenType.Semicolon);
            }

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

            ExpNode start = ParseExp();

            Consume(TokenType.To);

            ExpNode end = ParseExp();

            Consume(TokenType.Do);

            var body = new List<ExpNode>();

            while (Current.Type != TokenType.End)
            {
                if (Current.Type == TokenType.EOF)
                    throw new Exception(
                        "Syntax Error: Expected 'end'.");

                if (Current.Type == TokenType.Semicolon)
                {
                    Consume(TokenType.Semicolon);
                    continue;
                }

                body.Add(ParseExp());

                if (Current.Type == TokenType.Semicolon)
                    Consume(TokenType.Semicolon);
            }

            Consume(TokenType.End);

            return new ForExpNode(
                variable,
                start,
                end,
                body);
        }

        private ExpNode ParseAssignOrBinary()
        {
            if (Current.Type == TokenType.Identifier &&
                LookAheadType(1) == TokenType.Assign)
            {
                string name =
                    Consume(TokenType.Identifier).Value;

                Consume(TokenType.Assign);

                ExpNode value = ParseExp();

                return new AssignNode(
                    name,
                    value);
            }

            return ParseOr();
        }

        private ExpNode ParseOr()
        {
            ExpNode left = ParseAnd();

            while (Current.Type == TokenType.Or)
            {
                Consume(TokenType.Or);

                ExpNode right = ParseAnd();

                left =
                    new BinaryExpNode(
                        "or",
                        left,
                        right);
            }

            return left;
        }

        private ExpNode ParseAnd()
        {
            ExpNode left = ParseEquality();

            while (Current.Type == TokenType.And)
            {
                Consume(TokenType.And);

                ExpNode right = ParseEquality();

                left =
                    new BinaryExpNode(
                        "and",
                        left,
                        right);
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

                ExpNode right = ParseComparison();

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        right);
            }

            return left;
        }

        private ExpNode ParseComparison()
        {
            ExpNode left = ParseAdditive();

            while (Current.Type == TokenType.LessThan ||
                   Current.Type == TokenType.LessEqual ||
                   Current.Type == TokenType.GreaterThan ||
                   Current.Type == TokenType.GreaterEqual)
            {
                string op = Current.Value;
                _idx++;

                ExpNode right = ParseAdditive();

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        right);
            }

            return left;
        }

        private ExpNode ParseAdditive()
        {
            ExpNode left = ParseMultiplicative();

            while (Current.Type == TokenType.Plus ||
                   Current.Type == TokenType.Minus)
            {
                string op = Current.Value;
                _idx++;

                ExpNode right = ParseMultiplicative();

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        right);
            }

            return left;
        }

        private ExpNode ParseMultiplicative()
        {
            ExpNode left = ParseUnary();

            while (Current.Type == TokenType.Multiply ||
                   Current.Type == TokenType.Divide)
            {
                string op = Current.Value;
                _idx++;

                ExpNode right = ParseUnary();

                left =
                    new BinaryExpNode(
                        op,
                        left,
                        right);
            }

            return left;
        }

        private ExpNode ParseUnary()
        {
            if (Current.Type == TokenType.Minus)
            {
                Consume(TokenType.Minus);

                return new UnaryExpNode(
                    "-",
                    ParseUnary());
            }

            return ParsePrimary();
        }

        private ExpNode ParsePrimary()
        {
            if (Current.Type == TokenType.Int)
            {
                int value =
                    int.Parse(Current.Value);

                _idx++;

                return new IntLiteralNode(value);
            }

            if (Current.Type == TokenType.String)
            {
                string value = Current.Value;

                _idx++;

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
                string name = Current.Value;
                _idx++;

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

                    return new CallExpNode(
                        name,
                        args);
                }

                return new VarAccessNode(name);
            }

            if (Current.Type == TokenType.LParen)
            {
                Consume(TokenType.LParen);

                ExpNode expression = ParseExp();

                Consume(TokenType.RParen);

                return expression;
            }

            throw new Exception(
                $"Syntax Error: Unexpected token " +
                $"'{Current.Value}'.");
        }

        private TokenType LookAheadType(int offset)
        {
            int index = _idx + offset;

            if (index >= _tokens.Count)
                return TokenType.EOF;

            return _tokens[index].Type;
        }
    }
}