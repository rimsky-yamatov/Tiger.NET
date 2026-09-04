using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.NET
{
    public enum TokenType { Let, In, End, Var, Assign, Plus, Minus, Multiply, Divide, String, Int, Identifier, LParen, RParen, Comma, Colon, EOF }

    public class Token
    {
        public TokenType Type {; }
        public string Value {; }
        public Token(TokenType type, string value = "") { Type = type; Value = value; }
    }

    public class Lexer
    {
        private readonly string _src;
        private int _pos;

        public Lexer(string src) => _src = src;

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (_pos < _src.Length)
            {
                char c = _src[_pos];
                if (char.IsWhiteSpace(c)) { _pos++; continue; }

                if (c == '"')
                {
                    _pos++;
                    var sb = new StringBuilder();
                    while (_pos < _src.Length && _src[_pos] != '"')
                    {
                        sb.Append(_src[_pos++]);
                    }
                    _pos++; // skip closing quote
                    tokens.Add(new Token(TokenType.String, sb.ToString()));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    var sb = new StringBuilder();
                    while (_pos < _src.Length && char.IsDigit(_src[_pos])) sb.Append(_src[_pos++]);
                    tokens.Add(new Token(TokenType.Int, sb.ToString()));
                    continue;
                }

                if (char.IsLetter(c))
                {
                    var sb = new StringBuilder();
                    while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_')) sb.Append(_src[_pos++]);
                    string val = sb.ToString();
                    TokenType t = val switch
                    {
                        "let" => TokenType.Let,
                        "in" => TokenType.In,
                        "end" => TokenType.End,
                        "var" => TokenType.Var,
                        _ => TokenType.Identifier
                    };
                    tokens.Add(new Token(t, val));
                    continue;
                }

                if (c == ':' && MatchNext('=')) { tokens.Add(new Token(TokenType.Assign, ":=")); continue; }
                if (c == '+') { tokens.Add(new Token(TokenType.Plus, "+")); _pos++; continue; }
                if (c == '-') { tokens.Add(new Token(TokenType.Minus, "-")); _pos++; continue; }
                if (c == '*') { tokens.Add(new Token(TokenType.Multiply, "*")); _pos++; continue; }
                if (c == '/') { tokens.Add(new Token(TokenType.Divide, "/")); _pos++; continue; }
                if (c == '(') { tokens.Add(new Token(TokenType.LParen, "(")); _pos++; continue; }
                if (c == ')') { tokens.Add(new Token(TokenType.RParen, ")")); _pos++; continue; }
                if (c == ',') { tokens.Add(new Token(TokenType.Comma, ",")); _pos++; continue; }

                _pos++;
            }
            tokens.Add(new Token(TokenType.EOF));
            return tokens;
        }

        private bool MatchNext(char expected)
        {
            if (_pos + 1 < _src.Length && _src[_pos + 1] == expected)
            {
                _pos += 2;
                return true;
            }
            return false;
        }
    }
}