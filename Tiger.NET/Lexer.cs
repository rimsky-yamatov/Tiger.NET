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
        Struct,
        If,
        Then,
        Else,
        While,
        Do,
        For,
        To,
        Break,
        Continue,
        True,
        False,
        And,
        Or,

        Assign,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,

        Equal,
        NotEqual,
        LessThan,
        LessEqual,
        GreaterThan,
        GreaterEqual,

        LParen,
        RParen,
        LBracket,
        RBracket,
        LBrace,
        RBrace,

        Dot,
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
        public int Line { get; set; }
        public int Column { get; set; }

        public Token(
            TokenType type,
            string value = "",
            int line = 1,
            int column = 1)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}('{Value}') [{Line}:{Column}]";
        }
    }

    public class Lexer
    {
        private readonly string _src;
        private int _pos;
        private int _line = 1;
        private int _column = 1;

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
                    Advance();
                    continue;
                }

                if (c == '/' && LookAhead(1) == '*')
                {
                    SkipComment();
                    continue;
                }

                int line = _line;
                int column = _column;

                if (c == '"')
                {
                    tokens.Add(ReadString(line, column));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    tokens.Add(ReadNumber(line, column));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    tokens.Add(ReadIdentifier(line, column));
                    continue;
                }

                if (c == ':' && LookAhead(1) == '=')
                {
                    Advance();
                    Advance();
                    tokens.Add(new Token(TokenType.Assign, ":=", line, column));
                    continue;
                }

                if (c == '<' && LookAhead(1) == '>')
                {
                    Advance();
                    Advance();
                    tokens.Add(new Token(TokenType.NotEqual, "<>", line, column));
                    continue;
                }

                if (c == '<' && LookAhead(1) == '=')
                {
                    Advance();
                    Advance();
                    tokens.Add(new Token(TokenType.LessEqual, "<=", line, column));
                    continue;
                }

                if (c == '>' && LookAhead(1) == '=')
                {
                    Advance();
                    Advance();
                    tokens.Add(new Token(TokenType.GreaterEqual, ">=", line, column));
                    continue;
                }

                TokenType? type = c switch
                {
                    '+' => TokenType.Plus,
                    '-' => TokenType.Minus,
                    '*' => TokenType.Multiply,
                    '/' => TokenType.Divide,
                    '%' => TokenType.Modulo,
                    '=' => TokenType.Equal,
                    '<' => TokenType.LessThan,
                    '>' => TokenType.GreaterThan,
                    '(' => TokenType.LParen,
                    ')' => TokenType.RParen,
                    '[' => TokenType.LBracket,
                    ']' => TokenType.RBracket,
                    '{' => TokenType.LBrace,
                    '}' => TokenType.RBrace,
                    '.' => TokenType.Dot,
                    ',' => TokenType.Comma,
                    ':' => TokenType.Colon,
                    ';' => TokenType.Semicolon,
                    _ => null
                };

                if (type.HasValue)
                {
                    Advance();
                    tokens.Add(new Token(type.Value, c.ToString(), line, column));
                    continue;
                }

                throw new Exception(
                    $"Lexer Error at {line}:{column}: unexpected character '{c}'");
            }

            tokens.Add(new Token(TokenType.EOF, "", _line, _column));
            return tokens;
        }

        private Token ReadString(int line, int column)
        {
            Advance();

            var sb = new StringBuilder();

            while (_pos < _src.Length)
            {
                char c = _src[_pos];

                if (c == '"')
                {
                    Advance();
                    return new Token(TokenType.String, sb.ToString(), line, column);
                }

                if (c == '\\')
                {
                    Advance();

                    if (_pos >= _src.Length)
                        break;

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
                            sb.Append(escaped);
                            break;
                    }

                    Advance();
                    continue;
                }

                sb.Append(c);
                Advance();
            }

            throw new Exception(
                $"Lexer Error at {line}:{column}: unterminated string literal");
        }

        private Token ReadNumber(int line, int column)
        {
            var sb = new StringBuilder();

            while (_pos < _src.Length && char.IsDigit(_src[_pos]))
            {
                sb.Append(_src[_pos]);
                Advance();
            }

            return new Token(TokenType.Int, sb.ToString(), line, column);
        }

        private Token ReadIdentifier(int line, int column)
        {
            var sb = new StringBuilder();

            while (_pos < _src.Length &&
                   (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_'))
            {
                sb.Append(_src[_pos]);
                Advance();
            }

            string value = sb.ToString();

            TokenType type = value switch
            {
                "let" => TokenType.Let,
                "in" => TokenType.In,
                "end" => TokenType.End,
                "var" => TokenType.Var,
                "function" => TokenType.Function,
                "struct" => TokenType.Struct,
                "if" => TokenType.If,
                "then" => TokenType.Then,
                "else" => TokenType.Else,
                "while" => TokenType.While,
                "do" => TokenType.Do,
                "for" => TokenType.For,
                "to" => TokenType.To,
                "break" => TokenType.Break,
                "continue" => TokenType.Continue,
                "true" => TokenType.True,
                "false" => TokenType.False,
                "and" => TokenType.And,
                "or" => TokenType.Or,
                _ => TokenType.Identifier
            };

            return new Token(type, value, line, column);
        }

        private void SkipComment()
        {
            Advance();
            Advance();

            while (_pos < _src.Length)
            {
                if (_src[_pos] == '*' && LookAhead(1) == '/')
                {
                    Advance();
                    Advance();
                    return;
                }

                Advance();
            }

            throw new Exception(
                $"Lexer Error at {_line}:{_column}: unterminated comment");
        }

        private void Advance()
        {
            if (_pos >= _src.Length)
                return;

            char c = _src[_pos++];

            if (c == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
        }

        private char LookAhead(int offset)
        {
            int index = _pos + offset;
            return index < _src.Length ? _src[index] : '\0';
        }
    }
}