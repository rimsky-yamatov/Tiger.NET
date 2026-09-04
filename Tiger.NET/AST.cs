using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.NET
{
    public enum TokenType
    {
        Let, In, End, Var, If, Then, Else, While, Do, For, To, Break,
        Assign, Plus, Minus, Multiply, Divide, Equal, NotEqual, LessThan,
        LessEqual, GreaterThan, GreaterEqual, LParen, RParen, Comma, Colon,
        Semicolon, String, Int, Identifier, EOF
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
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

                // コメント処理 /* ... */
                if (c == '/' && LookAhead(1) == '*')
                {
                    _pos += 2;
                    while (_pos < _src.Length - 1 && !(_src[_pos] == '*' && _src[_pos + 1] == '/')) _pos++;
                    _pos += 2;
                    continue;
                }

                if (c == '"')
                {
                    _pos++;
                    var sb = new StringBuilder();
                    while (_pos < _src.Length && _src[_pos] != '"')
                    {
                        if (_src[_pos] == '\\' && _pos + 1 < _src.Length)
                        {
                            _pos++;
                            if (_src[_pos] == 'n') sb.Append('\n');
                            else if (_src[_pos] == 't') sb.Append('\t');
                            else sb.Append(_src[_pos]);
                        }
                        else { sb.Append(_src[_pos]); }
                        _pos++;
                    }
                    _pos++;
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
                        "if" => TokenType.If,
                        "then" => TokenType.Then,
                        "else" => TokenType.Else,
                        "while" => TokenType.While,
                        "do" => TokenType.Do,
                        "for" => TokenType.For,
                        "to" => TokenType.To,
                        "break" => TokenType.Break,
                        _ => TokenType.Identifier
                    };
                    tokens.Add(new Token(t, val));
                    continue;
                }

                if (c == ':' && LookAhead(1) == '=') { _pos += 2; tokens.Add(new Token(TokenType.Assign, ":=")); continue; }
                if (c == '<' && LookAhead(1) == '>') { _pos += 2; tokens.Add(new Token(TokenType.NotEqual, "<>")); continue; }
                if (c == '<' && LookAhead(1) == '=') { _pos += 2; tokens.Add(new Token(TokenType.LessEqual, "<=")); continue; }
                if (c == '>' && LookAhead(1) == '=') { _pos += 2; tokens.Add(new Token(TokenType.GreaterEqual, ">=")); continue; }
                if (c == '<') { _pos++; tokens.Add(new Token(TokenType.LessThan, "<")); continue; }
                if (c == '>') { _pos++; tokens.Add(new Token(TokenType.GreaterThan, ">")); continue; }
                if (c == '=') { _pos++; tokens.Add(new Token(TokenType.Equal, "=")); continue; }
                if (c == '+') { _pos++; tokens.Add(new Token(TokenType.Plus, "+")); continue; }
                if (c == '-') { _pos++; tokens.Add(new Token(TokenType.Minus, "-")); continue; }
                if (c == '*') { _pos++; tokens.Add(new Token(TokenType.Multiply, "*")); continue; }
                if (c == '/') { _pos++; tokens.Add(new Token(TokenType.Divide, "/")); continue; }
                if (c == '(') { _pos++; tokens.Add(new Token(TokenType.LParen, "(")); continue; }
                if (c == ')') { _pos++; tokens.Add(new Token(TokenType.RParen, ")")); continue; }
                if (c == ',') { _pos++; tokens.Add(new Token(TokenType.Comma, ",")); continue; }
                if (c == ';') { _pos++; tokens.Add(new Token(TokenType.Semicolon, ";")); continue; }

                _pos++;
            }
            tokens.Add(new Token(TokenType.EOF));
            return tokens;
        }

        private char LookAhead(int offset) => (_pos + offset < _src.Length) ? _src[_pos + offset] : '\0';
    }
}