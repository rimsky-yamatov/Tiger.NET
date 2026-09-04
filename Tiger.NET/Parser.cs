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
            if (Current.Type == TokenType.If) return ParseIf();
            if (Current.Type == TokenType.While) return ParseWhile();
            if (Current.Type == TokenType.For) return ParseFor();
            if (Current.Type == TokenType.Break) { Consume(TokenType.Break); return new BreakExpNode(); }

            return ParseAssignOrBinary();
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
                    if (Current.Type == TokenType.Colon)
                    {
                        Consume(TokenType.Colon);
                        Consume(TokenType.Identifier);
                    }
                    Consume(TokenType.Assign);
                    ExpNode init = ParseExp();
                    decs.Add(new VarDeclNode(name, init));
                }
                else if (Current.Type == TokenType.Function)
                {
                    decs.Add(ParseFunctionDecl());
                }
                else { _idx++; }
            }
            Consume(TokenType.In);
            var body = new List<ExpNode>();
            while (Current.Type != TokenType.End && Current.Type != TokenType.EOF)
            {
                body.Add(ParseExp());
                if (Current.Type == TokenType.Semicolon) Consume(TokenType.Semicolon);
            }
            Consume(TokenType.End);
            return new LetExpNode(decs, body);
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
                    string pName = Consume(TokenType.Identifier).Value;
                    Consume(TokenType.Colon);
                    string pType = Consume(TokenType.Identifier).Value;
                    parameters.Add(new FuncParam(pName, pType));
                    if (Current.Type == TokenType.Comma) Consume(TokenType.Comma);
                    else break;
                }
            }
            Consume(TokenType.RParen);

            string retType = "void";
            if (Current.Type == TokenType.Colon)
            {
                Consume(TokenType.Colon);
                retType = Consume(TokenType.Identifier).Value;
            }

            Consume(TokenType.Equal);
            ExpNode body = ParseExp();
            return new FunctionDeclNode(name, parameters, retType, body);
        }

        private ExpNode ParseIf()
        {
            Consume(TokenType.If);
            var cond = ParseExp();
            Consume(TokenType.Then);
            var thenExp = ParseExp();
            ExpNode? elseExp = null;
            if (Current.Type == TokenType.Else)
            {
                Consume(TokenType.Else);
                elseExp = ParseExp();
            }
            return new IfExpNode(cond, thenExp, elseExp);
        }

        private ExpNode ParseWhile()
        {
            Consume(TokenType.While);
            var cond = ParseExp();
            Consume(TokenType.Do);
            var body = ParseExp();
            return new WhileExpNode(cond, body);
        }

        private ExpNode ParseFor()
        {
            Consume(TokenType.For);
            string varName = Consume(TokenType.Identifier).Value;
            Consume(TokenType.Assign);
            var start = ParseExp();
            Consume(TokenType.To);
            var end = ParseExp();
            Consume(TokenType.Do);
            var body = ParseExp();
            return new ForExpNode(varName, start, end, body);
        }

        private ExpNode ParseAssignOrBinary()
        {
            if (Current.Type == TokenType.Identifier && LookAheadType(1) == TokenType.Assign)
            {
                string name = Consume(TokenType.Identifier).Value;
                Consume(TokenType.Assign);
                var val = ParseExp();
                return new AssignNode(name, val);
            }
            return ParseBinaryExp();
        }

        private ExpNode ParseBinaryExp()
        {
            var left = ParsePrimary();
            while (IsOp(Current.Type))
            {
                string op = Current.Value;
                _idx++;
                var right = ParsePrimary();
                left = new BinaryExpNode(op, left, right);
            }
            return left;
        }

        private bool IsOp(TokenType t) =>
            t == TokenType.Plus || t == TokenType.Minus || t == TokenType.Multiply || t == TokenType.Divide ||
            t == TokenType.Equal || t == TokenType.NotEqual || t == TokenType.LessThan ||
            t == TokenType.LessEqual || t == TokenType.GreaterThan || t == TokenType.GreaterEqual;

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
            if (Current.Type == TokenType.LParen)
            {
                Consume(TokenType.LParen);
                var exp = ParseExp();
                Consume(TokenType.RParen);
                return exp;
            }
            throw new Exception($"Unexpected token: {Current.Value}");
        }

        private TokenType LookAheadType(int offset) => (_idx + offset < _tokens.Count) ? _tokens[_idx + offset].Type : TokenType.EOF;
    }
}