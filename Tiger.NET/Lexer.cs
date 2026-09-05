using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.NET
{
    public enum TokenType
    {
        Let,
        In,
        End,
        Var,
        Function,
        If,
        Then,
        Else,
        While,
        Do,
        For,
        To,
        Break,
        True,
        False,
        And,
        Or,
        Assign,
        Plus,
        Minus,
        Multiply,
        Divide,
        Equal,
        NotEqual,
        LessThan,
        LessEqual,
        GreaterThan,
        GreaterEqual,
        LParen,
        RParen,
        Comma,
        Colon,
        Semicolon,
        String,
        Int,
        Identifier,
        EOF
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public Token(TokenType type, string value = "")
        {
            Type = type;
            Value = value;
        }
    }

    public class Lexer
    {
        private readonly string _src;
        private int _pos;

        public Lexer(string src)
        {
            _src = src;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (_pos < _src.Length)
            {
                char c = _src[_pos];

                if (char.IsWhiteSpace(c))
                {
                    _pos++;
                    continue;
                }

                if (c == '/' && LookAhead(1) == '*')
                {
                    _pos += 2;

                    bool closed = false;

                    while (_pos < _src.Length - 1)
                    {
                        if (_src[_pos] == '*' &&
                            _src[_pos + 1] == '/')
                        {
                            _pos += 2;
                            closed = true;
                            break;
                        }

                        _pos++;
                    }

                    if (!closed)
                        throw new Exception(
                            "Lexer Error: Unterminated comment.");

                    continue;
                }

                if (c == '"')
                {
                    _pos++;

                    var sb = new StringBuilder();
                    bool closed = false;

                    while (_pos < _src.Length)
                    {
                        if (_src[_pos] == '"')
                        {
                            _pos++;
                            closed = true;
                            break;
                        }

                        if (_src[_pos] == '\\')
                        {
                            _pos++;

                            if (_pos >= _src.Length)
                                throw new Exception(
                                    "Lexer Error: Unterminated string.");

                            char escaped = _src[_pos];

                            switch (escaped)
                            {
                                case 'n':
                                    sb.Append('\n');
                                    break;

                                case 't':
                                    sb.Append('\t');
                                    break;

                                case 'r':
                                    sb.Append('\r');
                                    break;

                                case '\\':
                                    sb.Append('\\');
                                    break;

                                case '"':
                                    sb.Append('"');
                                    break;

                                default:
                                    throw new Exception(
                                        $"Lexer Error: Unknown escape '\\{escaped}'.");
                            }

                            _pos++;
                            continue;
                        }

                        sb.Append(_src[_pos]);
                        _pos++;
                    }

                    if (!closed)
                        throw new Exception(
                            "Lexer Error: Unterminated string.");

                    tokens.Add(
                        new Token(TokenType.String, sb.ToString()));

                    continue;
                }

                if (char.IsDigit(c))
                {
                    var sb = new StringBuilder();

                    while (_pos < _src.Length &&
                           char.IsDigit(_src[_pos]))
                    {
                        sb.Append(_src[_pos]);
                        _pos++;
                    }

                    tokens.Add(
                        new Token(TokenType.Int, sb.ToString()));

                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    var sb = new StringBuilder();

                    while (_pos < _src.Length &&
                           (char.IsLetterOrDigit(_src[_pos]) ||
                            _src[_pos] == '_'))
                    {
                        sb.Append(_src[_pos]);
                        _pos++;
                    }

                    string value = sb.ToString();

                    TokenType type = value switch
                    {
                        "let" => TokenType.Let,
                        "in" => TokenType.In,
                        "end" => TokenType.End,
                        "var" => TokenType.Var,
                        "function" => TokenType.Function,
                        "if" => TokenType.If,
                        "then" => TokenType.Then,
                        "else" => TokenType.Else,
                        "while" => TokenType.While,
                        "do" => TokenType.Do,
                        "for" => TokenType.For,
                        "to" => TokenType.To,
                        "break" => TokenType.Break,
                        "true" => TokenType.True,
                        "false" => TokenType.False,
                        "and" => TokenType.And,
                        "or" => TokenType.Or,
                        _ => TokenType.Identifier
                    };

                    tokens.Add(new Token(type, value));
                    continue;
                }

                if (c == ':' && LookAhead(1) == '=')
                {
                    _pos += 2;
                    tokens.Add(
                        new Token(TokenType.Assign, ":="));
                    continue;
                }

                if (c == '<' && LookAhead(1) == '>')
                {
                    _pos += 2;
                    tokens.Add(
                        new Token(TokenType.NotEqual, "<>"));
                    continue;
                }

                if (c == '<' && LookAhead(1) == '=')
                {
                    _pos += 2;
                    tokens.Add(
                        new Token(TokenType.LessEqual, "<="));
                    continue;
                }

                if (c == '>' && LookAhead(1) == '=')
                {
                    _pos += 2;
                    tokens.Add(
                        new Token(TokenType.GreaterEqual, ">="));
                    continue;
                }

                switch (c)
                {
                    case '<':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.LessThan, "<"));
                        break;

                    case '>':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.GreaterThan, ">"));
                        break;

                    case '=':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Equal, "="));
                        break;

                    case '+':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Plus, "+"));
                        break;

                    case '-':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Minus, "-"));
                        break;

                    case '*':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Multiply, "*"));
                        break;

                    case '/':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Divide, "/"));
                        break;

                    case '(':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.LParen, "("));
                        break;

                    case ')':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.RParen, ")"));
                        break;

                    case ':':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Colon, ":"));
                        break;

                    case ',':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Comma, ","));
                        break;

                    case ';':
                        _pos++;
                        tokens.Add(
                            new Token(TokenType.Semicolon, ";"));
                        break;

                    default:
                        throw new Exception(
                            $"Lexer Error: Unexpected character '{c}'.");
                }
            }

            tokens.Add(new Token(TokenType.EOF));

            return tokens;
        }

        private char LookAhead(int offset)
        {
            int index = _pos + offset;

            if (index >= 0 && index < _src.Length)
                return _src[index];

            return '\0';
        }
    }
}