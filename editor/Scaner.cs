using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class Token
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public string Location => $"строка {Line}, позиция {StartPos}";
    }

    public class LexicalAnalyzer
    {
        private enum State
        {
            Start,
            Space,
            AssignOp,
            Int,
            Numeric,
            CharStart,
            CharContent,
            CharComplete,
            LeftParen,
            RightParen,
            Comma,
            Minus,
            Id,
            End,
            Error
        }

        private readonly HashSet<string> keywords = new() { "TRUE", "FALSE", "NULL" };

        public List<Token> Analyze(string input)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrEmpty(input))
                return tokens;

            int line = 1;
            int pos = 1;
            int tokenStartLine = 1;
            int tokenStartPos = 1;
            State currentState = State.Start;
            StringBuilder currentToken = new StringBuilder();

            for (int i = 0; i <= input.Length; i++)
            {
                char c = (i < input.Length) ? input[i] : '\0';

                if (c == '\n')
                {
                    if (currentState == State.CharContent || currentState == State.CharStart)
                    {
                        tokens.Add(new Token
                        {
                            Code = -1,
                            Type = "error",
                            Value = currentToken.ToString(),
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1,
                            IsError = true,
                            ErrorMessage = "Незакрытая кавычка в конце строки"
                        });
                        currentState = State.Start;
                        currentToken.Clear();
                    }

                    line++;
                    pos = 1;
                    continue;
                }
                if (c == '\r')
                {
                    pos++;
                    continue;
                }

                switch (currentState)
                {
                    case State.Start:
                        tokenStartLine = line;
                        tokenStartPos = pos;

                        if (char.IsLetter(c) && (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z')))
                        {
                            currentState = State.Id;
                            currentToken.Append(c);
                        }
                        else if (char.IsDigit(c))
                        {
                            currentState = State.Int;
                            currentToken.Append(c);
                        }
                        else if (c == ' ')
                        {
                            currentState = State.Space;
                            currentToken.Append(c);
                        }
                        else if (c == '<')
                        {
                            currentState = State.AssignOp;
                            currentToken.Append(c);
                        }
                        else if (c == '(')
                        {
                            currentState = State.LeftParen;
                            currentToken.Append(c);
                        }
                        else if (c == ')')
                        {
                            currentState = State.RightParen;
                            currentToken.Append(c);
                        }
                        else if (c == ',')
                        {
                            currentState = State.Comma;
                            currentToken.Append(c);
                        }
                        else if (c == '-')
                        {
                            currentState = State.Minus;
                            currentToken.Append(c);
                        }
                        else if (c == '"')
                        {
                            currentState = State.CharStart;
                            currentToken.Append(c);
                        }
                        else if (c == ';')
                        {
                            currentState = State.End;
                            currentToken.Append(c);
                        }
                        else if (c == '\0')
                        {
                            i = input.Length;
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = -1,
                                Type = "error",
                                Value = c.ToString(),
                                Line = line,
                                StartPos = pos,
                                EndPos = pos,
                                IsError = true,
                                ErrorMessage = $"Недопустимый символ '{c}'"
                            });
                        }
                        break;

                    case State.Id:
                        if (char.IsLetterOrDigit(c) || c == '_')
                        {
                            currentToken.Append(c);
                        }
                        else
                        {
                            string value = currentToken.ToString();
                            int code;
                            string type;

                            if (keywords.Contains(value))
                            {
                                code = value == "TRUE" ? 12 : value == "FALSE" ? 13 : 14;
                                type = "keyword";
                            }
                            else
                            {
                                code = 1;
                                type = "id";
                            }

                            tokens.Add(new Token
                            {
                                Code = code,
                                Type = type,
                                Value = value,
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = pos - 1
                            });

                            currentState = State.Start;
                            currentToken.Clear();
                            i--;
                            pos--;
                        }
                        break;

                    case State.Int:
                        if (char.IsDigit(c))
                        {
                            currentToken.Append(c);
                        }
                        else if (c == '.')
                        {
                            currentToken.Append(c);
                            currentState = State.Numeric;
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = 4,
                                Type = "integer",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = pos - 1
                            });

                            currentState = State.Start;
                            currentToken.Clear();
                            i--;
                            pos--;
                        }
                        break;

                    case State.Numeric:
                        if (char.IsDigit(c))
                        {
                            currentToken.Append(c);
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = 5,
                                Type = "numeric",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = pos - 1
                            });

                            currentState = State.Start;
                            currentToken.Clear();
                            i--;
                            pos--;
                        }
                        break;

                    case State.Space:
                        if (c == ' ')
                        {
                            currentToken.Append(c);
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = 2,
                                Type = "space",
                                Value = "(пробел)",
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = pos - 1
                            });

                            currentState = State.Start;
                            currentToken.Clear();
                            i--;
                            pos--;
                        }
                        break;

                    case State.AssignOp:
                        if (c == '-')
                        {
                            tokens.Add(new Token
                            {
                                Code = 3,
                                Type = "assign",
                                Value = "<-",
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = pos
                            });
                            currentToken.Append(c);
                            currentState = State.Start;
                            currentToken.Clear();
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = -1,
                                Type = "error",
                                Value = "<",
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = tokenStartPos,
                                IsError = true,
                                ErrorMessage = "Ожидался символ '-' после '<'"
                            });

                            currentState = State.Start;
                            currentToken.Clear();
                            i--;
                            pos--;
                        }
                        break;

                    case State.LeftParen:
                        tokens.Add(new Token
                        {
                            Code = 7,
                            Type = "leftparen",
                            Value = "(",
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1
                        });

                        currentState = State.Start;
                        currentToken.Clear();
                        i--;
                        pos--;
                        break;

                    case State.RightParen:
                        tokens.Add(new Token
                        {
                            Code = 8,
                            Type = "rightparen",
                            Value = ")",
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1
                        });

                        currentState = State.Start;
                        currentToken.Clear();
                        i--;
                        pos--;
                        break;

                    case State.Comma:
                        tokens.Add(new Token
                        {
                            Code = 9,
                            Type = "comma",
                            Value = ",",
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1
                        });

                        currentState = State.Start;
                        currentToken.Clear();
                        i--;
                        pos--;
                        break;

                    case State.Minus:
                        tokens.Add(new Token
                        {
                            Code = 11,
                            Type = "minus",
                            Value = "-",
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1
                        });

                        currentState = State.Start;
                        currentToken.Clear();
                        i--;
                        pos--;
                        break;

                    case State.CharStart:
                        if (c == '"')
                        {
                            currentState = State.CharComplete;
                            currentToken.Append(c);
                        }
                        else
                        {
                            currentState = State.CharContent;
                            currentToken.Append(c);
                        }
                        break;

                    case State.CharContent:
                        if (c == '"')
                        {
                            currentState = State.CharComplete;
                            currentToken.Append(c);
                        }
                        else if (c == '\0' || c == '\n')
                        {
                            tokens.Add(new Token
                            {
                                Code = -1,
                                Type = "error",
                                Value = currentToken.ToString(),
                                Line = tokenStartLine,
                                StartPos = tokenStartPos,
                                EndPos = pos - 1,
                                IsError = true,
                                ErrorMessage = "Незакрытая кавычка"
                            });
                            currentState = State.Start;
                            currentToken.Clear();
                            if (c == '\n') 
                                i--;
                        }
                        else
                        {
                            currentToken.Append(c);
                        }
                        break;

                    case State.CharComplete:
                        tokens.Add(new Token
                        {
                            Code = 6,
                            Type = "character",
                            Value = currentToken.ToString(),
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1
                        });

                        currentState = State.Start;
                        currentToken.Clear();
                        i--;
                        pos--;
                        break;

                    case State.End:
                        tokens.Add(new Token
                        {
                            Code = 10,
                            Type = "end",
                            Value = ";",
                            Line = tokenStartLine,
                            StartPos = tokenStartPos,
                            EndPos = pos - 1
                        });

                        currentState = State.Start;
                        currentToken.Clear();
                        i--;
                        pos--;
                        break;
                }

                pos++;
            }

            return tokens;
        }
    }
}