using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace editor
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int position;
        private readonly List<SyntaxError> errors;
        private readonly TetraGenerator tetraGenerator;

        public IReadOnlyList<SyntaxError> Errors => errors;
        public List<Tetra> Tetras => tetraGenerator.Tetras;

        public Parser(List<Token> inputTokens)
        {
            tokens = inputTokens?
                .Where(t => t != null && t.Type != TokenType.Eof && t.Type != TokenType.Space)
                .ToList() ?? new List<Token>();
            position = 0;
            errors = new List<SyntaxError>();
            tetraGenerator = new TetraGenerator();
        }

        private Token CurrentToken
        {
            get
            {
                if (position < tokens.Count)
                    return tokens[position];
                if (tokens.Count > 0)
                {
                    var last = tokens[tokens.Count - 1];
                    int endPos = last.EndPosition;
                    return new Token(TokenType.Eof, "", endPos, endPos, last.Line);
                }
                return new Token(TokenType.Eof, "", 0, 0, 1);
            }
        }

        private bool IsStartOfFactor(Token token)
        {
            return token.Type == TokenType.Number ||
                   token.Type == TokenType.Identifier ||
                   token.Type == TokenType.LParen;
        }

        private bool IsOperator(Token token)
        {
            return token.Type == TokenType.Plus ||
                   token.Type == TokenType.Minus ||
                   token.Type == TokenType.Multiply ||
                   token.Type == TokenType.Divide;
        }

        private bool CheckOperandAfterOperator(string opStr, int opLine, int opPos)
        {
            var errorFragments = new List<string>();
            int errorLine = opLine;
            int errorPos = opPos + opStr.Length;

            while (position < tokens.Count && tokens[position].Type == TokenType.Error)
            {
                if (errorFragments.Count == 0)
                {
                    errorLine = tokens[position].Line;
                    errorPos = tokens[position].Position;
                }
                errorFragments.Add(tokens[position].Value);
                position++;
            }

            if (CurrentToken.Type == TokenType.Eof || IsOperator(CurrentToken) || CurrentToken.Type == TokenType.RParen || CurrentToken.Type == TokenType.Semicolon)
            {
                string fragment;
                string what;

                if (errorFragments.Count > 0)
                {
                    fragment = string.Join("", errorFragments);
                    if (IsOperator(CurrentToken))
                    {
                        fragment += " " + CurrentToken.Value;
                        position++;
                    }
                    what = "ошибочный символ";
                }
                else if (CurrentToken.Type == TokenType.Eof)
                {
                    fragment = "конец строки";
                    what = "конец строки";
                }
                else if (CurrentToken.Type == TokenType.Semicolon)
                {
                    fragment = ";";
                    what = "';'";
                    errorLine = CurrentToken.Line;
                    errorPos = CurrentToken.Position;
                }
                else if (CurrentToken.Type == TokenType.RParen)
                {
                    fragment = ")";
                    what = "')'";
                    errorLine = CurrentToken.Line;
                    errorPos = CurrentToken.Position;
                }
                else
                {
                    fragment = CurrentToken.Value;
                    what = $"'{CurrentToken.Value}'";
                    errorLine = CurrentToken.Line;
                    errorPos = CurrentToken.Position;
                }

                errors.Add(new SyntaxError
                {
                    InvalidFragment = fragment,
                    Line = errorLine,
                    Position = errorPos,
                    Description = $"После '{opStr}' ожидалось число, идентификатор или '('"
                });

                return false;
            }

            return true;
        }

        private string ParseE()
        {
            string result = ParseT();
            result = ParseA(result);
            return result;
        }

        private string ParseA(string inherited)
        {
            while (position < tokens.Count &&
                   (CurrentToken.Type == TokenType.Plus || CurrentToken.Type == TokenType.Minus))
            {
                string opStr = CurrentToken.Value;
                int opLine = CurrentToken.Line;
                int opPos = CurrentToken.Position;
                position++;

                if (!CheckOperandAfterOperator(opStr, opLine, opPos))
                    return inherited;

                string tResult = ParseT();
                inherited = tetraGenerator.AddTetra(opStr, inherited, tResult);

                if (position < tokens.Count && IsStartOfFactor(CurrentToken))
                    break;
            }
            return inherited;
        }

        private string ParseT()
        {
            string result = ParseF();
            result = ParseB(result);
            return result;
        }

        private string ParseB(string inherited)
        {
            while (position < tokens.Count &&
                   (CurrentToken.Type == TokenType.Multiply || CurrentToken.Type == TokenType.Divide))
            {
                string opStr = CurrentToken.Value;
                int opLine = CurrentToken.Line;
                int opPos = CurrentToken.Position;
                position++;

                if (!CheckOperandAfterOperator(opStr, opLine, opPos))
                    return inherited;

                string fResult = ParseF();
                inherited = tetraGenerator.AddTetra(opStr, inherited, fResult);

                if (position < tokens.Count && IsStartOfFactor(CurrentToken))
                    break;
            }
            return inherited;
        }

        private string ParseF()
        {
            if (position >= tokens.Count)
                return "error";

            if (CurrentToken.Type == TokenType.Number || CurrentToken.Type == TokenType.Identifier)
            {
                string value = CurrentToken.Value;
                position++;
                return value;
            }

            if (CurrentToken.Type == TokenType.LParen)
            {
                int parenLine = CurrentToken.Line;
                int parenPos = CurrentToken.Position;
                position++;

                if (CurrentToken.Type == TokenType.RParen)
                {
                    errors.Add(new SyntaxError
                    {
                        InvalidFragment = ")",
                        Line = CurrentToken.Line,
                        Position = CurrentToken.Position,
                        Description = "После '(' ожидалось число, идентификатор или '('"
                    });
                    position++;
                    return "error";
                }

                if (!IsStartOfFactor(CurrentToken))
                {
                    string fragment = CurrentToken.Value;
                    if (string.IsNullOrEmpty(fragment))
                        fragment = "конец строки";
                    else if (CurrentToken.Type == TokenType.Semicolon)
                        fragment = "';'";

                    errors.Add(new SyntaxError
                    {
                        InvalidFragment = fragment,
                        Line = CurrentToken.Line,
                        Position = CurrentToken.Position,
                        Description = "После '(' ожидалось число, идентификатор или '('"
                    });

                    if (CurrentToken.Type != TokenType.Eof && CurrentToken.Type != TokenType.Semicolon)
                        position++;
                }

                string result = ParseE();

                while (position < tokens.Count && CurrentToken.Type != TokenType.RParen && CurrentToken.Type != TokenType.Eof && CurrentToken.Type != TokenType.Semicolon)
                {
                    if (IsStartOfFactor(CurrentToken))
                    {
                        int prev = position - 1;
                        while (prev >= 0 && tokens[prev].Type == TokenType.Error)
                            prev--;

                        if (prev >= 0 && tokens[prev].Type == TokenType.RParen)
                        {
                            errors.Add(new SyntaxError
                            {
                                InvalidFragment = CurrentToken.Value,
                                Line = CurrentToken.Line,
                                Position = CurrentToken.Position,
                                Description = $"После ')' ожидался оператор"
                            });
                        }

                        if (prev >= 0 && IsStartOfFactor(tokens[prev]) && tokens[prev].Type != TokenType.LParen)
                        {
                            string fragment = CurrentToken.Value;
                            errors.Add(new SyntaxError
                            {
                                InvalidFragment = fragment,
                                Line = CurrentToken.Line,
                                Position = CurrentToken.Position,
                                Description = $"После '{tokens[prev].Value}' ожидался оператор"
                            });
                        }

                        result = ParseE();
                    }
                    else if (IsOperator(CurrentToken))
                    {
                        string opStr = CurrentToken.Value;
                        int opLine = CurrentToken.Line;
                        int opPos = CurrentToken.Position;
                        position++;
                        CheckOperandAfterOperator(opStr, opLine, opPos);
                    }
                    else
                    {
                        position++;
                    }
                }

                if (CurrentToken.Type == TokenType.RParen)
                {
                    position++;
                }
                else if (CurrentToken.Type == TokenType.Eof || CurrentToken.Type == TokenType.Semicolon)
                {
                    string fragment = CurrentToken.Type == TokenType.Eof ? "конец строки" : "';'";
                    errors.Add(new SyntaxError
                    {
                        InvalidFragment = fragment,
                        Line = CurrentToken.Line,
                        Position = CurrentToken.Position,
                        Description = "Ожидалась закрывающая скобка ')'"
                    });
                }

                return result;
            }

            return "error";
        }

        private void ParseExpression()
        {
            if (position < tokens.Count && IsOperator(CurrentToken))
            {
                errors.Add(new SyntaxError
                {
                    InvalidFragment = CurrentToken.Value,
                    Line = CurrentToken.Line,
                    Position = CurrentToken.Position,
                    Description = $"Неожиданный оператор '{CurrentToken.Value}' в начале выражения"
                });
            }

            while (position < tokens.Count)
            {
                if (CurrentToken.Type == TokenType.Semicolon)
                    break;

                if (CurrentToken.Type == TokenType.Eof)
                    break;

                if (CurrentToken.Type == TokenType.Error)
                {
                    int prev = position - 1;
                    while (prev >= 0 && tokens[prev].Type == TokenType.Error)
                        prev--;

                    if (prev >= 0 && tokens[prev].Type == TokenType.RParen)
                    {
                        errors.Add(new SyntaxError
                        {
                            InvalidFragment = CurrentToken.Value,
                            Line = CurrentToken.Line,
                            Position = CurrentToken.Position,
                            Description = $"После ')' ожидался оператор"
                        });
                        ParseE();
                        continue;
                    }

                    if (prev >= 0 && IsStartOfFactor(tokens[prev]))
                    {
                        var parts = new List<string>();
                        int errLine = CurrentToken.Line;
                        int errPos = CurrentToken.Position;
                        int savedPos = position;

                        while (position < tokens.Count && CurrentToken.Type == TokenType.Error)
                        {
                            parts.Add(CurrentToken.Value);
                            position++;
                        }

                        if (position < tokens.Count && IsStartOfFactor(CurrentToken))
                        {
                            position = savedPos;
                            position++;
                            continue;
                        }

                        if (position >= tokens.Count || CurrentToken.Type == TokenType.Eof || CurrentToken.Type == TokenType.Semicolon)
                        {
                            continue;
                        }

                        string fragment = string.Join(" ", parts);
                        errors.Add(new SyntaxError
                        {
                            InvalidFragment = fragment,
                            Line = errLine,
                            Position = errPos,
                            Description = $"После '{tokens[prev].Value}' ожидался оператор"
                        });
                        continue;
                    }

                    position++;
                    continue;
                }

                if (IsStartOfFactor(CurrentToken))
                {
                    int startPos = position;

                    if (startPos > 0)
                    {
                        int prev = startPos - 1;
                        while (prev >= 0 && tokens[prev].Type == TokenType.Error)
                            prev--;

                        if (prev >= 0 && tokens[prev].Type == TokenType.RParen)
                        {
                            errors.Add(new SyntaxError
                            {
                                InvalidFragment = CurrentToken.Value,
                                Line = CurrentToken.Line,
                                Position = CurrentToken.Position,
                                Description = $"После ')' ожидался оператор"
                            });
                            ParseE();
                            continue;
                        }

                        if (prev >= 0 && IsStartOfFactor(tokens[prev]))
                        {
                            var fragmentParts = new List<string>();
                            int firstErrorIdx = prev + 1;
                            int errorLine = tokens[firstErrorIdx].Line;
                            int errorPos = tokens[firstErrorIdx].Position;

                            int tempPos = prev + 1;
                            while (tempPos < position)
                            {
                                fragmentParts.Add(tokens[tempPos].Value);
                                tempPos++;
                            }

                            if (CurrentToken.Type == TokenType.LParen)
                            {
                                string fragment = string.Join(" ", fragmentParts);
                                if (string.IsNullOrEmpty(fragment))
                                    fragment = "(";

                                errors.Add(new SyntaxError
                                {
                                    InvalidFragment = fragment,
                                    Line = errorLine,
                                    Position = errorPos,
                                    Description = $"После '{tokens[prev].Value}' ожидался оператор"
                                });

                                ParseE();
                                continue;
                            }

                            fragmentParts.Add(CurrentToken.Value);
                            string fragment2 = string.Join(" ", fragmentParts);

                            errors.Add(new SyntaxError
                            {
                                InvalidFragment = fragment2,
                                Line = errorLine,
                                Position = errorPos,
                                Description = $"После '{tokens[prev].Value}' ожидался оператор"
                            });

                            ParseE();
                            continue;
                        }
                    }

                    ParseE();
                }
                else if (IsOperator(CurrentToken))
                {
                    string opStr = CurrentToken.Value;
                    int opLine = CurrentToken.Line;
                    int opPos = CurrentToken.Position;
                    position++;

                    if (CheckOperandAfterOperator(opStr, opLine, opPos))
                    {
                        ParseT();
                    }
                }
                else if (CurrentToken.Type == TokenType.RParen)
                {
                    errors.Add(new SyntaxError
                    {
                        InvalidFragment = CurrentToken.Value,
                        Line = CurrentToken.Line,
                        Position = CurrentToken.Position,
                        Description = "Лишняя закрывающая скобка ')'"
                    });
                    position++;
                }
                else
                {
                    position++;
                }
            }
        }

        public bool Parse()
        {
            try
            {
                tetraGenerator.Clear();

                while (position < tokens.Count)
                {
                    if (CurrentToken.Type == TokenType.Error)
                    {
                        position++;
                        continue;
                    }

                    if (CurrentToken.Type == TokenType.Eof)
                        break;

                    if (CurrentToken.Type == TokenType.Semicolon)
                    {
                        position++;
                        continue;
                    }

                    ParseExpression();

                    if (position < tokens.Count && CurrentToken.Type == TokenType.Semicolon)
                    {
                        position++;
                    }
                }

                if (tokens.Count > 0)
                {
                    int lastIdx = tokens.Count - 1;
                    while (lastIdx >= 0 && (tokens[lastIdx].Type == TokenType.Error || tokens[lastIdx].Type == TokenType.Eof || tokens[lastIdx].Type == TokenType.Semicolon))
                        lastIdx--;

                    if (lastIdx >= 0 && tokens[lastIdx].Type != TokenType.Semicolon)
                    {
                        bool hasSemicolon = tokens.Any(t => t.Type == TokenType.Semicolon);
                        if (!hasSemicolon)
                        {
                            errors.Add(new SyntaxError
                            {
                                InvalidFragment = "конец строки",
                                Line = tokens[lastIdx].Line,
                                Position = tokens[lastIdx].Position + tokens[lastIdx].Value.Length,
                                Description = "Ожидалась ';' в конце выражения"
                            });
                        }
                    }
                }

                return errors.Count == 0;
            }
            catch (Exception ex)
            {
                errors.Add(new SyntaxError
                {
                    InvalidFragment = "",
                    Line = CurrentToken.Line,
                    Position = CurrentToken.Position,
                    Description = $"Ошибка разбора: {ex.Message}"
                });
                return false;
            }
        }
    }
}