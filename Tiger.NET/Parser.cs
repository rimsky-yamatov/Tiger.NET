using System;
using System.Collections.Generic;

namespace Tiger.NET
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _idx = 0;

        public Parser(List<Token> tokens) => _tokens = tokens;

        private Token Current => _idx < _tokens.Count ? _tokens[_idx] : _tokens[^1];

        private Token Consume(TokenType type)
        {
            if (Current.Type == type) { var t = Current; _idx++; return t; }
            throw new Exception($"Syntax Error: Expected {type}, got {Current.Type} ('{Current.Value}')");
        }

        public ExpNode Parse() => ParseExp();

        private ExpNode ParseExp()
        {
            if (Current.Type == TokenType.Let) return ParseLet();
            return ParseBinaryExp();
        }

        private ExpNode ParseLet()
        {
            Consume(TokenType.Let);
            var decs = new List<ExpNode>();
            while (Current.Type != TokenType.In && Current.Type != TokenType.EOF)
            {
                if (Current.Type == TokenType.Var)
                {
                    Consume(TokenType.Var);
                    string name = Consume(TokenType.Identifier).Value;
                    Consume(TokenType.Assign);
                    ExpNode init = ParseExp();
                    decs.Add(new VarDeclNode(name, init));
                }
                else _idx++;
            }
            Consume(TokenType.In);
            var body = new List<ExpNode>();
            while (Current.Type != TokenType.End && Current.Type != TokenType.EOF)
            {
                body.Add(ParseExp());
            }
            Consume(TokenType.End);
            return new LetExpNode(decs, body);
        }

        private ExpNode ParseBinaryExp()
        {
            var left = ParsePrimary();
            if (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus ||
                Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide)
            {
                string op = Current.Value;
                _idx++;
                var right = ParseExp();
                return new BinaryExpNode(op, left, right);
            }
            return left;
        }

        private ExpNode ParsePrimary()
        {
            if (Current.Type == TokenType.Int)
            {
                int val = int.Parse(Current.Value);
                _idx++;
                return new IntLiteralNode(val);
            }
            if (Current.Type == TokenType.String)
            {
                string val = Current.Value;
                _idx++;
                return new StringLiteralNode(val);
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
                    return new CallExpNode(name, args);
                }
                return new VarAccessNode(name);
            }
            throw new Exception($"Unexpected token: {Current.Value}");
        }
    }
}