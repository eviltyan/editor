using System;
using System.Collections.Generic;
using System.Linq;

namespace editor
{
    public class PolizCalculator
    {
        private readonly List<Token> tokens;
        private readonly List<string> errors;
        private string poliz;

        public IReadOnlyList<string> Errors => errors;
        public string Poliz => poliz;

        public PolizCalculator(List<Token> inputTokens)
        {
            this.tokens = inputTokens?
                .Where(t => t != null && t.Type != TokenType.Eof)
                .ToList() ?? new List<Token>();

            this.errors = new List<string>();
            this.poliz = "";
        }

        private bool IsPureIntegerExpression()
        {
            return !tokens.Any(t => t != null && t.Type == TokenType.Identifier);
        }

        public string ConvertToPoliz()
        {
            if (tokens.Count == 0)
            {
                poliz = "";
                return "";
            }

            if (!IsPureIntegerExpression())
            {
                errors.Add("Выражение содержит идентификаторы, ПОЛИЗ только для целых чисел");
                return "";
            }

            var output = new List<string>();
            var stack = new Stack<Token>();

            var precedence = new Dictionary<TokenType, int>
            {
                { TokenType.Plus, 1 },
                { TokenType.Minus, 1 },
                { TokenType.Multiply, 2 },
                { TokenType.Divide, 2 }
            };

            foreach (var token in tokens)
            {
                if (token == null) continue;

                if (token.Type == TokenType.Number)
                {
                    output.Add(token.Value);
                }
                else if (token.Type == TokenType.LParen)
                {
                    stack.Push(token);
                }
                else if (token.Type == TokenType.RParen)
                {
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.LParen)
                    {
                        output.Add(stack.Pop().Value);
                    }
                    if (stack.Count > 0 && stack.Peek().Type == TokenType.LParen)
                    {
                        stack.Pop();
                    }
                    else
                    {
                        errors.Add("Несогласованные скобки");
                    }
                }
                else if (precedence.ContainsKey(token.Type))
                {
                    while (stack.Count > 0 &&
                           stack.Peek().Type != TokenType.LParen &&
                           precedence.ContainsKey(stack.Peek().Type) &&
                           precedence[stack.Peek().Type] >= precedence[token.Type])
                    {
                        output.Add(stack.Pop().Value);
                    }
                    stack.Push(token);
                }
            }

            while (stack.Count > 0)
            {
                if (stack.Peek().Type == TokenType.LParen)
                {
                    errors.Add("Несогласованные скобки");
                    stack.Pop();
                }
                else
                {
                    output.Add(stack.Pop().Value);
                }
            }

            poliz = string.Join(" ", output);
            return poliz;
        }

        public double? EvaluatePoliz()
        {
            if (errors.Count > 0) return null;
            if (string.IsNullOrEmpty(poliz)) return null;

            var polizTokens = poliz.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var stack = new Stack<double>();

            foreach (var token in polizTokens)
            {
                if (int.TryParse(token, out int number))
                {
                    stack.Push(number);
                }
                else
                {
                    if (stack.Count < 2)
                    {
                        errors.Add("Недостаточно операндов для операции " + token);
                        return null;
                    }

                    double right = stack.Pop();
                    double left = stack.Pop();
                    double result = 0;

                    switch (token)
                    {
                        case "+": result = left + right; break;
                        case "-": result = left - right; break;
                        case "*": result = left * right; break;
                        case "/":
                            if (right == 0)
                            {
                                errors.Add("Деление на ноль");
                                return null;
                            }
                            result = left / right;
                            break;
                        default:
                            errors.Add("Неизвестная операция: " + token);
                            return null;
                    }
                    stack.Push(result);
                }
            }

            if (stack.Count != 1)
            {
                errors.Add("Ошибка вычисления: неверное количество операндов");
                return null;
            }

            return stack.Pop();
        }
    }
}