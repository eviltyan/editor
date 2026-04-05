using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class SyntaxError
    {
        public string InvalidFragment { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
        public string Description { get; set; }
        public string Location => $"строка {Line}, позиция {Position}";
    }

    public class SyntaxAutomaton
    {
        private enum State
        {
            Start,
            InNameVec,
            ExpectArrow,
            ExpectMinus,
            ExpectCOrNull,
            InFuncCall,
            InParams,
            AfterParam,
            InNumber,
            InNegativeNumber,
            AfterDecimalPoint,
            InString,
            ExpectSemicolon,
            End,
            Error
        }

        private enum StackSymbol
        {
            Left,
            Right
        }

        private bool CommaParam = false;

        private State currentState;
        private Stack<StackSymbol> stack;
        private List<Token> tokens;
        private int position;
        private List<SyntaxError> errors;
        private int currentLine;
        private int currentPos;

        public SyntaxAutomaton()
        {
            stack = new Stack<StackSymbol>();
            errors = new List<SyntaxError>();
        }

        public List<SyntaxError> Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            this.position = 0;
            this.currentState = State.Start;
            this.errors = new List<SyntaxError>();
            this.stack.Clear();

            while (position < tokens.Count && currentState != State.Error)
            {
                Token token = tokens[position];
                currentLine = token.Line;
                currentPos = token.StartPos;
                ProcessToken(token);
            }

            if (currentState == State.End && position >= tokens.Count)
            {
                // Успех
            }
            else if (tokens.Count == 0)
                return errors;
            else if (currentState != State.Error && currentState != State.End)
            {
                AddError("<конец файла>", currentLine, currentPos, "Неожиданный конец строки. Возможно, не закрыта скобка или отсутствует ';'");
            }

            return errors;
        }

        private void ProcessToken(Token token)
        {
            
            switch (currentState)
            {
                case State.Start:
                    // <Def> → <Letter> <NameVec>
                    if (IsLetter(token))
                    {
                        currentState = State.InNameVec;
                        position++;
                    }
                    else if (token.Value == "(пробел)")
                    {
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается идентификатор (буква), найдено '{token.Value}'");
                        string[] syncTokens = { "<-", "(", ";", ")", "," };
                        RecoverToSyncPoint(syncTokens);
                    }
                    break;

                case State.InNameVec:
                    // <NameVec> → <Letter> <NameVec> | "<" <Arrow>
                    if (token.Value == "<-")
                    {
                        currentState = State.ExpectCOrNull;
                        position++;
                    }
                    else if (token.Value == "(пробел)")
                    {
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается '<-', найдено '{token.Value}'");
                        string[] syncTokens = { "(", ";", ")", "," };
                        RecoverToSyncPoint(syncTokens);
                    }
                    break;

                case State.ExpectCOrNull:
                    // <RightPart> → "c" <FuncCall> | "NULL;"
                    if (token.Value == "c" && token.Type == "id")
                    {
                        currentState = State.InFuncCall;
                        position++;
                    }
                    else if (token.Value == "(пробел)")
                    {
                        position++;
                    }
                    else if (token.Value == "NULL" && token.Code == 14)
                    {
                        currentState = State.ExpectSemicolon;
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается 'c' или 'NULL', найдено '{token.Value}'");
                        string[] syncTokens = { "(", ";", ")", "," };
                        RecoverToSyncPoint(syncTokens);
                    }
                    break;

                case State.InFuncCall:
                    // <FuncCall> → "(" <Param> | “();”
                    if (token.Value == "(")
                    {
                        stack.Push(StackSymbol.Left);
                        currentState = State.InParams;
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается '(', найдено '{token.Value}'");
                        string[] syncTokens = { ";", ")", "," };
                        RecoverToSyncPoint(syncTokens);
                    }
                    break;

                case State.InParams:
                    // <Param> → “-“ <UnsignedInt>, <Digit> <Int>, “"” <CharSeq> | “TRUE,” | “FALSE,” | “NULL,” | “TRUE);” | “FALSE);” | “NULL);”
                    if (token.Value == ")")
                    {
                        if (CommaParam)
                        {
                            CommaParam = false;
                            AddError(token.Value, token.Line, token.StartPos,
                                "Неожиданная ')'. После запятой ожидается параметр (число, строка, TRUE, FALSE, NULL)");
                            string[] syncTokens = { ";", ")", "," };
                            RecoverToSyncPoint(syncTokens);
                        }
                        else if (stack.Count > 0 && stack.Peek() == StackSymbol.Left)
                        {
                            stack.Pop();
                            currentState = State.ExpectSemicolon;
                            position++;
                        }
                        else
                        {
                            AddError(token.Value, token.Line, token.StartPos,
                                "Неожиданная ')'. После запятой ожидается параметр (число, строка, TRUE, FALSE, NULL)");
                            string[] syncTokens = { ";", ")", "," };
                            RecoverToSyncPoint(syncTokens);
                        }
                    }
                    else if (token.Value == "(пробел)")
                    {
                        position++;
                    }
                    else if (IsNumberParamStart(token))
                    {
                        if (token.Value == "-")
                        {
                            currentState = State.InNegativeNumber;
                            position++;
                        }
                        else
                        {
                            currentState = State.InNumber;
                        }
                    }
                    else if (IsStringParamStart(token))
                    {
                        currentState = State.AfterParam;
                        position++;
                    }
                    else if (token.Value == "TRUE" || token.Value == "NULL" || token.Value == "FALSE")
                    {
                        currentState = State.AfterParam;
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается параметр (число, строка, TRUE, FALSE, NULL) или закрытие скобки, найдено '{token.Value}'");
                        RecoverToNextParam();
                    }
                    break;

                case State.InNumber:
                    /*
                      <Int> → <Digit> <Int> | “.” <SecondPart> | <Digit> <EndParams> | “,” <Param>
                      <SecondPart> → <Digit> <SecondPart> | <Digit> <EndParams> | “,” <Param>
                     */
                    if (IsNumber(token))
                    {
                        if (token.Value.Contains("."))
                        {
                            string[] parts = token.Value.Split('.');
                            if (string.IsNullOrEmpty(parts[0]) || parts[0].Length == 0)
                            {
                                AddError(token.Value, token.Line, token.StartPos,
                                    "Ожидается цифра перед десятичной точкой");
                            }
                            if (string.IsNullOrEmpty(parts[1]) || parts[1].Length == 0)
                            {
                                AddError(token.Value, token.Line, token.StartPos,
                                    "Ожидается цифра после десятичной точки");
                            }
                        }

                        currentState = State.AfterParam;
                        position++;
                    }
                    else if (token.Value == ",")
                    {
                        currentState = State.InParams;
                        CommaParam = true;
                        position++;
                    }
                    else if (token.Value == ")")
                    {
                        currentState = State.ExpectSemicolon;
                        stack.Push(StackSymbol.Right);
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается число, найдено '{token.Value}'");
                        RecoverToNextParam();
                    }
                    break;

                case State.InNegativeNumber:
                    // <UnsignedInt> → <Digit> <Int> | <Digit> <EndParams>
                    if (IsNumber(token))
                    {
                        currentState = State.AfterParam;
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается число после '-', найдено '{token.Value}'");
                        RecoverToNextParam();
                    }
                    break;

                case State.AfterParam:
                    // “,” <Param> | <_> <EndParams>
                    if (token.Value == ",")
                    {
                        currentState = State.InParams;
                        CommaParam = true;
                        position++;
                    }
                    else if (token.Value == "(пробел)")
                    {
                        position++;
                    }
                    else if (token.Value == ")")
                    {
                        if (stack.Count > 0 && stack.Peek() == StackSymbol.Left)
                        {
                            stack.Pop();
                            currentState = State.ExpectSemicolon;
                            position++;
                        }
                        else
                        {
                            AddError(token.Value, token.Line, token.StartPos,
                                "Неожиданная ')'");
                            string[] syncTokens = { ";", ")", "," };
                            RecoverToSyncPoint(syncTokens);
                        }
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается ',' или ')', найдено '{token.Value}'");
                        RecoverToNextParam();
                    }
                    break;

                case State.ExpectSemicolon:
                    if (token.Value == ";")
                    {
                        currentState = State.End;
                        position++;
                    }
                    else
                    {
                        AddError(token.Value, token.Line, token.StartPos,
                            $"Ожидается ';', найдено '{token.Value}'");
                        string[] syncTokens = { ";" };
                        RecoverToSyncPoint(syncTokens);
                    }
                    break;

                case State.End:
                    currentState = State.Start;
                    break;
            }
        }

        private void RecoverToSyncPoint(string[] syncTokens)
        {
            //string[] syncTokens = { "<-", "(", ";", ")", ","};

            while (position < tokens.Count && !syncTokens.Contains(tokens[position].Value))
            {
                position++;
            }

            if (position < tokens.Count)
            {
                Token syncToken = tokens[position];

                switch (syncToken.Value)
                {
                    case ";":
                        currentState = State.End;
                        position++;
                        break;

                    case "<-":
                        currentState = State.ExpectCOrNull;
                        position++;
                        break;

                    case "(":
                        while (stack.Count > 0 && stack.Peek() != StackSymbol.Right)
                        {
                            stack.Pop();
                        }
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }
                        currentState = State.InParams;
                        stack.Push(StackSymbol.Left);
                        position++;
                        break;

                    case ")":
                        while (stack.Count > 0 && stack.Peek() != StackSymbol.Left)
                        {
                            stack.Pop();
                        }
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }
                        currentState = State.ExpectSemicolon;
                        stack.Push(StackSymbol.Right);
                        position++;
                        break;

                    case ",":
                        currentState = State.InParams;
                        position++;
                        break;
                }
            }
            else
            {
                currentState = State.Error;
            }
        }

        private void RecoverToNextParam()
        {
            string[] syncTokens = { ",", ")"};

            while (position < tokens.Count && !syncTokens.Contains(tokens[position].Value))
            {
                position++;
            }

            if (position < tokens.Count)
            {
                if (tokens[position].Value == ",")
                {
                    currentState = State.InParams;
                    position++;
                }
                else if (tokens[position].Value == ")")
                {
                    if (stack.Count > 0 && stack.Peek() == StackSymbol.Left)
                    {
                        stack.Pop();
                    }
                    currentState = State.ExpectSemicolon;
                    stack.Push(StackSymbol.Right);
                    position++;
                }
            }
            else
            {
                currentState = State.Error;
            }
        }

        private bool IsLetter(Token token)
        {
            if (token.Type != "id") return false;
            if (token.Value.Length == 0) return false;
            char firstChar = token.Value[0];
            return char.IsLetter(firstChar);
        }

        private bool IsNumberParamStart(Token token)
        {
            return IsNumber(token) || token.Value == "-";
        }

        private bool IsNumber(Token token)
        {
            return token.Type == "integer" || token.Type == "numeric";
        }

        private bool IsStringParamStart(Token token)
        {
            return token.Type == "character";
        }

        private void AddError(string fragment, int line, int position, string description)
        {
            errors.Add(new SyntaxError
            {
                InvalidFragment = string.IsNullOrEmpty(fragment) ? "<конец файла>" : fragment,
                Line = line,
                Position = position,
                Description = description
            });
        }
    }
}
