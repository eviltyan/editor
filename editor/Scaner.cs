using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public enum TokenType
    {
        Number = 1,
        Identifier = 2,
        Plus = 3,
        Minus = 4,
        Multiply = 5,
        Divide = 6,
        LParen = 7,
        RParen = 8,
        Semicolon = 9,
        Space = 10,
        Eof = 0,
        Error = -1
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }
        public int EndPosition { get; set; }
        public int Line { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public Token(TokenType type, string value, int startPos, int endPos, int line)
        {
            Type = type;
            Value = value;
            Position = startPos;
            EndPosition = endPos;
            Line = line;
            IsError = false;
            ErrorMessage = "";
        }

        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case TokenType.Number: return "целое число";
                    case TokenType.Identifier: return "идентификатор";
                    case TokenType.Plus: return "оператор '+'";
                    case TokenType.Minus: return "оператор '-'";
                    case TokenType.Multiply: return "оператор '*'";
                    case TokenType.Divide: return "оператор '/'";
                    case TokenType.LParen: return "открывающая скобка";
                    case TokenType.RParen: return "закрывающая скобка";
                    case TokenType.Semicolon: return "конец оператора";
                    case TokenType.Space: return "разделитель (пробел)";
                    case TokenType.Eof: return "конец строки";
                    case TokenType.Error: return "ошибка";
                    default: return "неизвестно";
                }
            }
        }

        public string Location => $"строка {Line}, {Position}-{EndPosition}";

        public override string ToString()
        {
            return $"{TypeName}: '{Value}' {Location}";
        }
    }

    public class Lexer
    {
        private readonly string input;
        private int position;
        private int line;
        private int column;
        private readonly List<SyntaxError> errors;

        public IReadOnlyList<SyntaxError> Errors => errors;

        public Lexer(string input_str)
        {
            input = input_str ?? "";
            position = 0;
            line = 1;
            column = 1;
            errors = new List<SyntaxError>();
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            if (string.IsNullOrEmpty(input))
            {
                tokens.Add(new Token(TokenType.Eof, "", column, column, line));
                return tokens;
            }

            while (position < input.Length)
            {
                char current = input[position];

                if (current == '\n')
                {
                    line++;
                    column = 1;
                    position++;
                    continue;
                }

                if (current == '\r')
                {
                    position++;
                    continue;
                }

                if (current == ' ')
                {
                    int start = column;
                    position++;
                    column++;

                    while (position < input.Length && input[position] == ' ')
                    {
                        position++;
                        column++;
                    }

                    tokens.Add(new Token(TokenType.Space, new string(' ', column - start), start, column - 1, line));
                    continue;
                }

                if ((current >= 'a' && current <= 'z') || (current >= 'A' && current <= 'Z'))
                {
                    tokens.Add(ReadIdentifier());
                    continue;
                }

                if (char.IsDigit(current))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                switch (current)
                {
                    case '+':
                        tokens.Add(new Token(TokenType.Plus, "+", column, column, line));
                        position++;
                        column++;
                        break;
                    case '-':
                        tokens.Add(new Token(TokenType.Minus, "-", column, column, line));
                        position++;
                        column++;
                        break;
                    case '*':
                        tokens.Add(new Token(TokenType.Multiply, "*", column, column, line));
                        position++;
                        column++;
                        break;
                    case '/':
                        tokens.Add(new Token(TokenType.Divide, "/", column, column, line));
                        position++;
                        column++;
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LParen, "(", column, column, line));
                        position++;
                        column++;
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RParen, ")", column, column, line));
                        position++;
                        column++;
                        break;
                    case ';':
                        tokens.Add(new Token(TokenType.Semicolon, ";", column, column, line));
                        position++;
                        column++;
                        break;
                    default:
                        var errorToken = new Token(TokenType.Error, current.ToString(), column, column, line)
                        {
                            IsError = true,
                            ErrorMessage = $"Недопустимый символ '{current}'"
                        };
                        tokens.Add(errorToken);

                        errors.Add(new SyntaxError
                        {
                            InvalidFragment = current.ToString(),
                            Line = line,
                            Position = column,
                            Description = $"Недопустимый символ '{current}'"
                        });

                        position++;
                        column++;
                        break;
                }
            }

            tokens.Add(new Token(TokenType.Eof, "", column, column, line));
            return tokens;
        }

        private Token ReadIdentifier()
        {
            int startColumn = column;
            int startLine = line;
            var sb = new StringBuilder();

            while (position < input.Length)
            {
                char c = input[position];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || char.IsDigit(c) || c == '_')
                {
                    sb.Append(c);
                    position++;
                    column++;
                }
                else
                {
                    break;
                }
            }

            return new Token(TokenType.Identifier, sb.ToString(), startColumn, column - 1, startLine);
        }

        private Token ReadNumber()
        {
            int startColumn = column;
            int startLine = line;
            var sb = new StringBuilder();

            while (position < input.Length && char.IsDigit(input[position]))
            {
                sb.Append(input[position]);
                position++;
                column++;
            }

            return new Token(TokenType.Number, sb.ToString(), startColumn, column - 1, startLine);
        }
    }
}