using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace editor
{
    public class SemanticAnalyzer
    {
        private SymbolTable symbolTable;
        private List<Token> tokens;
        private int position;
        private List<SemanticError> errors;
        private bool currentNodeHasErrors;

        private static readonly HashSet<string> ReservedWords = new HashSet<string>
        {
            "TRUE", "FALSE", "NULL", "c", "Inf"
        };

        public SemanticAnalyzer()
        {
            symbolTable = new SymbolTable();
            errors = new List<SemanticError>();
        }

        public (List<VectorDeclNode> astNodes, List<SemanticError> errors) Analyze(List<Token> tokens)
        {
            this.tokens = tokens;
            this.position = 0;
            this.errors = new List<SemanticError>();
            symbolTable.Clear();
            symbolTable.ClearErrors();

            List<VectorDeclNode> astNodes = new List<VectorDeclNode>();

            while (position < tokens.Count)
            {
                SkipSpaces();

                if (position >= tokens.Count)
                    break;

                currentNodeHasErrors = false;

                VectorDeclNode node = ParseVectorDeclaration();

                if (node != null)
                {
                    node.HasErrors = currentNodeHasErrors;

                    if (!currentNodeHasErrors)
                    {
                        astNodes.Add(node);
                    }
                }
                else
                {
                    position++;
                }
            }

            errors.AddRange(symbolTable.GetErrors());
            return (astNodes, errors);
        }

        private VectorDeclNode ParseVectorDeclaration()
        {
            int startPos = position;

            SkipSpaces();

            if (position >= tokens.Count)
                return null;

            var node = new VectorDeclNode();

            if (tokens[position].Type != "id")
            {
                return null;
            }

            string vectorName = tokens[position].Value;
            node.Name = vectorName;
            node.Line = tokens[position].Line;
            node.Position = tokens[position].StartPos;

            if (!IsValidIdentifierName(vectorName, node.Line, node.Position))
            {
                currentNodeHasErrors = true;
            }

            position++;

            SkipSpaces();

            if (position >= tokens.Count || tokens[position].Value != "<-")
            {
                return null;
            }
            position++;

            SkipSpaces();

            if (position >= tokens.Count)
                return null;

            bool nameAlreadyExists = symbolTable.Contains(vectorName);
            if (nameAlreadyExists)
            {
                currentNodeHasErrors = true;
            }

            if (tokens[position].Value == "NULL")
            {
                node.IsNull = true;
                node.Type = "NULL";
                node.Initializer = new NullLiteralNode
                {
                    Line = tokens[position].Line,
                    Position = tokens[position].StartPos
                };

                if (!currentNodeHasErrors)
                {
                    symbolTable.Declare(node.Name, "NULL", node.Line, node.Position, node);
                }

                position++;

                SkipSpaces();

                return node;
            }
            else if (tokens[position].Value == "c")
            {
                node.IsNull = false;
                var funcCall = new FuncCallNode
                {
                    FunctionName = "c",
                    Line = tokens[position].Line,
                    Position = tokens[position].StartPos
                };

                position++;
                SkipSpaces();

                if (position < tokens.Count && tokens[position].Value == "(")
                {
                    position++;
                    SkipSpaces();

                    funcCall.Arguments = ParseElements();
                    node.Initializer = funcCall;

                    string vectorType = DetermineVectorType(funcCall.Arguments);
                    node.Type = vectorType;


                    symbolTable.Declare(node.Name, vectorType, node.Line, node.Position, node);

                    return node;
                }
            }

            position = startPos;
            return null;
        }

        private List<AstNode> ParseElements()
        {
            var elements = new List<AstNode>();

            while (position < tokens.Count && tokens[position].Value != ")" && tokens[position].Value != ";")
            {
                SkipSpaces();

                if (position >= tokens.Count)
                    break;

                AstNode element = ParseElement();
                if (element != null)
                {
                    elements.Add(element);
                }

                SkipSpaces();

                if (position < tokens.Count && tokens[position].Value == ",")
                {
                    position++;
                    SkipSpaces();
                }
            }

            if (position < tokens.Count && tokens[position].Value == ")")
            {
                position++;
            }

            return elements;
        }

        private AstNode ParseElement()
        {
            if (position >= tokens.Count)
                return null;

            Token token = tokens[position];

            if (token.Value == "-" && position + 1 < tokens.Count)
            {
                Token nextToken = tokens[position + 1];
                if (nextToken.Type == "integer" || nextToken.Type == "numeric")
                {
                    string fullValue = "-" + nextToken.Value;
                    bool isInteger = (nextToken.Type == "integer");
                    string finalValue = fullValue;
                    string type = nextToken.Type;

                    bool hasError = ValidateNumericValue(fullValue, ref isInteger, ref finalValue, ref type,
                                    token.Line, token.StartPos);

                    if (hasError)
                    {
                        currentNodeHasErrors = true;
                    }

                    var node = new NumberLiteralNode
                    {
                        Value = finalValue,
                        IsInteger = isInteger,
                        Type = type,
                        Line = token.Line,
                        Position = token.StartPos
                    };
                    position += 2;
                    return node;
                }
            }

            if (token.Type == "integer" || token.Type == "numeric")
            {
                bool isInteger = (token.Type == "integer");
                string finalValue = token.Value;
                string type = token.Type;

                bool hasError = ValidateNumericValue(token.Value, ref isInteger, ref finalValue, ref type,
                    token.Line, token.StartPos);

                if (hasError)
                {
                    currentNodeHasErrors = true;
                }

                position++;

                return new NumberLiteralNode
                {
                    Value = finalValue,
                    IsInteger = isInteger,
                    Type = type,
                    Line = token.Line,
                    Position = token.StartPos
                };
            }

            if (token.Type == "character")
            {
                position++;
                string value = token.Value.Trim('"');
                return new CharacterLiteralNode
                {
                    Value = value,
                    Type = token.Type,
                    Line = token.Line,
                    Position = token.StartPos
                };
            }

            if (token.Value == "TRUE" || token.Value == "FALSE")
            {
                position++;
                return new LogicalLiteralNode
                {
                    Value = token.Value == "TRUE",
                    Type = "logical",
                    Line = token.Line,
                    Position = token.StartPos
                };
            }

            if (token.Value == "NULL")
            {
                position++;
                return new NullLiteralNode
                {
                    Type = "NULL",
                    Line = token.Line,
                    Position = token.StartPos
                };
            }

            return null;
        }

        private string DetermineVectorType(List<AstNode> elements)
        {
            if (elements.Count == 0)
                return "empty";

            string type = "unknown";

            foreach (var element in elements)
            {
                if (element is CharacterLiteralNode)
                    type = "character";
                if (element is NumberLiteralNode num)
                    type = num.IsInteger ? "integer" : "numeric";
                if (element is LogicalLiteralNode)
                    type = "logical";
                if (element is NullLiteralNode && elements.Count == 1)
                    type = "NULL";
            }
            return type;
        }

        private bool IsValidIdentifierName(string name, int line, int position)
        {
            if (ReservedWords.Contains(name))
            {
                errors.Add(new SemanticError
                {
                    Message = $"Ошибка: '{name}' является зарезервированным словом или встроенной функцией R и не может быть использовано как имя переменной",
                    Line = line,
                    Position = position,
                    Fragment = name
                });
                return false;
            }

            return true;
        }

        private bool ValidateNumericValue(string value, ref bool isInteger, ref string finalValue, ref string type, int line, int position)
        {
            string originalValue = value;
            string shortValue = ShortenNumber(value);

            bool isNegative = value.StartsWith("-");
            string trimmed = value.TrimStart('-');

            const int MAX_SIGNIFICANT_DIGITS = 15;
            const int MAX_INTEGER_DIGITS = 309;

            string integerPart = "";
            string fractionalPart = "";

            if (trimmed.Contains("."))
            {
                int dotIndex = trimmed.IndexOf('.');
                integerPart = trimmed.Substring(0, dotIndex);
                fractionalPart = trimmed.Substring(dotIndex + 1);
            }
            else
            {
                integerPart = trimmed;
                fractionalPart = "";
            }

            string trimmedInteger = integerPart.TrimStart('0');
            if (string.IsNullOrEmpty(trimmedInteger))
                trimmedInteger = "0";

            string trimmedFractional = fractionalPart.TrimEnd('0');

            string normalizedValue;
            if (!string.IsNullOrEmpty(trimmedFractional))
            {
                normalizedValue = trimmedInteger + "." + trimmedFractional;
            }
            else
            {
                normalizedValue = trimmedInteger;
            }

            if (normalizedValue != trimmed)
            {
                finalValue = isNegative ? "-" + normalizedValue : normalizedValue;

                bool hadLeadingZeros = integerPart.Length > 1 && integerPart.StartsWith("0");
                bool hadTrailingZeros = fractionalPart.EndsWith("0") && fractionalPart.Length > 0;

                if (normalizedValue == "0")
                {
                    isInteger = false;
                    //errors.Add(new SemanticError
                    //{
                    //    Message = $"Примечание: число '{shortValue}' эквивалентно нулю и преобразовано в 0",
                    //    Line = line,
                    //    Position = position,
                    //    Fragment = value
                    //});
                }
                else if (hadLeadingZeros && hadTrailingZeros)
                {
                    //errors.Add(new SemanticError
                    //{
                    //    Message = $"Примечание: число '{shortValue}' нормализовано: убраны ведущие нули в целой части и замыкающие нули в дробной части",
                    //    Line = line,
                    //    Position = position,
                    //    Fragment = value
                    //});
                }
                else if (hadLeadingZeros)
                {
                    //errors.Add(new SemanticError
                    //{
                    //    Message = $"Примечание: число '{shortValue}' нормализовано: убраны ведущие нули в целой части",
                    //    Line = line,
                    //    Position = position,
                    //    Fragment = value
                    //});
                }
                else if (hadTrailingZeros)
                {
                    //errors.Add(new SemanticError
                    //{
                    //    Message = $"Примечание: число '{shortValue}' нормализовано: убраны замыкающие нули в дробной части",
                    //    Line = line,
                    //    Position = position,
                    //    Fragment = value
                    //});
                }

                value = finalValue;
                trimmed = normalizedValue;
            }


            if (integerPart.Length > MAX_INTEGER_DIGITS)
            {
                finalValue = isNegative ? "-Inf" : "Inf";
                isInteger = false;
                type = "numeric";
                errors.Add(new SemanticError
                {
                    Message = $"Примечание: число '{shortValue}' имеет более {MAX_INTEGER_DIGITS} цифр и преобразовано в {finalValue}",
                    Line = line,
                    Position = position,
                    Fragment = value
                });
                return false;
            }

            if (!string.IsNullOrEmpty(fractionalPart) && integerPart == "0")
            {
                int leadingZeros = 0;
                bool hasSignificant = false;

                foreach (char c in fractionalPart)
                {
                    if (c == '0')
                        leadingZeros++;
                    else if (c >= '1' && c <= '9')
                    {
                        hasSignificant = true;
                        break;
                    }
                }

                if (hasSignificant && leadingZeros >= 15)
                {
                    finalValue = "0";
                    isInteger = false;
                    errors.Add(new SemanticError
                    {
                        Message = $"Примечание: число '{shortValue}' имеет {leadingZeros} нулей после запятой и преобразовано в 0",
                        Line = line,
                        Position = position,
                        Fragment = value
                    });
                    return false;
                }
            }

            string allDigits = integerPart + fractionalPart;
            string significantDigits = allDigits.TrimStart('0');

            if (significantDigits.Length > MAX_SIGNIFICANT_DIGITS)
            {
                string roundedValue = TruncateToSignificantDigits(
                    integerPart, fractionalPart, MAX_SIGNIFICANT_DIGITS, isNegative);

                if (roundedValue != value)
                {
                    finalValue = roundedValue;
                    isInteger = false;
                    type = "numeric";

                    errors.Add(new SemanticError
                    {
                        Message = $"Примечание: число '{shortValue}' имеет {significantDigits.Length} значащих цифр. Допустимо  {MAX_SIGNIFICANT_DIGITS} значащих цифр." +
                                    $"Значение обрезано до '{roundedValue}'",
                        Line = line,
                        Position = position,
                        Fragment = value
                    });
                    return false;
                }
            }

            if (isInteger)
            {
                if (!int.TryParse(value, out int intValue))
                {
                    isInteger = false;
                    type = "numeric";
                    finalValue = value;
                    errors.Add(new SemanticError
                    {
                        Message = $"Примечание: значение '{shortValue}' выходит за пределы integer и преобразовано в numeric",
                        Line = line,
                        Position = position,
                        Fragment = value
                    });
                    return false;
                }
            }

            if (string.IsNullOrEmpty(finalValue))
                finalValue = value;

            return false;
        }

        private string TruncateToSignificantDigits(string integerPart, string fractionalPart,
        int maxDigits, bool isNegative)
        {
            string significantInteger = integerPart.TrimStart('0');

            if (!string.IsNullOrEmpty(significantInteger))
            {
                if (significantInteger.Length >= maxDigits)
                {
                    string truncated = significantInteger.Substring(0, maxDigits);

                    int zerosToAdd = integerPart.Length - significantInteger.Length;
                    string result = new string('0', zerosToAdd) + truncated;

                    return isNegative ? "-" + result : result;
                }
                else
                {
                    int remainingDigits = maxDigits - significantInteger.Length;

                    string truncatedFractional = "";
                    if (fractionalPart.Length > 0)
                    {
                        truncatedFractional = fractionalPart.Substring(0,
                            Math.Min(remainingDigits, fractionalPart.Length));
                    }

                    string result = integerPart;
                    if (!string.IsNullOrEmpty(truncatedFractional))
                        result += "." + truncatedFractional;

                    return isNegative ? "-" + result : result;
                }
            }
            else
            {
                int leadingZeros = 0;
                foreach (char c in fractionalPart)
                {
                    if (c == '0')
                        leadingZeros++;
                    else
                        break;
                }

                string significantFractional = fractionalPart.Substring(leadingZeros);

                if (significantFractional.Length > maxDigits)
                {
                    significantFractional = significantFractional.Substring(0, maxDigits);
                }

                string result = "0." + new string('0', leadingZeros) + significantFractional;
                return isNegative ? "-" + result : result;
            }
        }

        private string ShortenNumber(string value, int maxLength = 30)
        {
            if (value.Length <= maxLength)
                return value;

            bool isNegative = value.StartsWith("-");
            string absValue = isNegative ? value.Substring(1) : value;

            if (absValue.Contains("."))
            {
                int dotIndex = absValue.IndexOf('.');
                string integerPart = absValue.Substring(0, dotIndex);
                string fractionalPart = absValue.Substring(dotIndex + 1);

                string trimmedInteger = integerPart.TrimStart('0');
                int leadingZeros = integerPart.Length - trimmedInteger.Length;

                if (!string.IsNullOrEmpty(trimmedInteger) && trimmedInteger != "0")
                {
                    if (trimmedInteger.Length > 15)
                    {
                        string shortened = trimmedInteger.Substring(0, 5) + "..." +
                                          trimmedInteger.Substring(trimmedInteger.Length - 5);
                        string result = new string('0', leadingZeros) + shortened;
                        return isNegative ? "-" + result : result;
                    }
                    else
                    {
                        if (fractionalPart.Length > 10)
                        {
                            string shortFractional = fractionalPart.Substring(0, 5) + "..." +
                                                    fractionalPart.Substring(fractionalPart.Length - 2);
                            return (isNegative ? "-" : "") + absValue.Substring(0, dotIndex + 1) + shortFractional;
                        }
                    }
                }
                else
                {
                    string trimmedFractional = fractionalPart.TrimStart('0');
                    int leadingZerosInFraction = fractionalPart.Length - trimmedFractional.Length;

                    if (leadingZerosInFraction > 10)
                    {
                        string zeros = new string('0', 3);
                        return (isNegative ? "-" : "") + "0." + zeros + "..." +
                               (trimmedFractional.Length > 0 ? trimmedFractional.Substring(0, Math.Min(3, trimmedFractional.Length)) : "0");
                    }
                    else if (trimmedFractional.Length > 10)
                    {
                        string shortFrac = trimmedFractional.Substring(0, 5) + "..." +
                                          trimmedFractional.Substring(trimmedFractional.Length - 3);
                        return (isNegative ? "-" : "") + "0." + new string('0', leadingZerosInFraction) + shortFrac;
                    }
                }
            }
            else
            {
                string trimmed = absValue.TrimStart('0');
                int leadingZeros = absValue.Length - trimmed.Length;

                if (trimmed.Length > 15)
                {
                    string shortened = trimmed.Substring(0, 5) + "..." +
                                      trimmed.Substring(trimmed.Length - 5);
                    string result = new string('0', leadingZeros) + shortened;
                    return isNegative ? "-" + result : result;
                }
            }

            if (absValue.Length > maxLength)
            {
                string shortValue = absValue.Substring(0, maxLength - 3) + "...";
                return isNegative ? "-" + shortValue : shortValue;
            }

            return value;
        }

        private void SkipSpaces()
        {
            while (position < tokens.Count &&
                   (tokens[position].Value == "(пробел)" || tokens[position].Type == "space"))
                position++;
        }
    }

    public static class AstPrinter
    {
        public static string PrintToTree(AstNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine(node.GetNodeType());

            var attrs = node.GetAttributes();
            var children = new List<AstNode>(node.GetChildren());

            int totalItems = attrs.Count + children.Count;
            int itemIndex = 0;

            foreach (var attr in attrs)
            {
                itemIndex++;
                bool isLast = (itemIndex == totalItems);

                sb.Append(isLast ? "└── " : "├── ");

                string valueStr;
                if (attr.Value is bool b)
                    valueStr = b ? "True" : "False";
                else if (attr.Value is string s)
                    valueStr = $"\"{s}\"";
                else
                    valueStr = attr.Value?.ToString() ?? "null";

                sb.AppendLine($"{attr.Key}: {valueStr}");
            }

            for (int i = 0; i < children.Count; i++)
            {
                itemIndex++;
                bool isLast = (itemIndex == totalItems);

                string prefix = isLast ? "└── " : "├── ";
                string childIndent = isLast ? "    " : "│   ";

                PrintChildNode(children[i], prefix, childIndent, sb);
            }

            return sb.ToString();
        }

        private static void PrintChildNode(AstNode node, string prefix, string indent, StringBuilder sb)
        {
            sb.Append(prefix);
            sb.AppendLine(node.GetNodeType());

            var attrs = node.GetAttributes();
            var children = new List<AstNode>(node.GetChildren());

            int totalItems = attrs.Count + children.Count;
            int itemIndex = 0;

            foreach (var attr in attrs)
            {
                itemIndex++;
                bool isLast = (itemIndex == totalItems);

                sb.Append(indent);
                sb.Append(isLast ? "└── " : "├── ");

                string valueStr;
                if (attr.Value is bool b)
                    valueStr = b ? "True" : "False";
                else if (attr.Value is string s)
                    valueStr = $"\"{s}\"";
                else
                    valueStr = attr.Value?.ToString() ?? "null";

                sb.AppendLine($"{attr.Key}: {valueStr}");
            }

            for (int i = 0; i < children.Count; i++)
            {
                itemIndex++;
                bool isLast = (itemIndex == totalItems);

                string childPrefix = indent + (isLast ? "└── " : "├── ");
                string childIndent = indent + (isLast ? "    " : "│   ");

                PrintChildNode(children[i], childPrefix, childIndent, sb);
            }
        }

        public static string PrintToJson(AstNode node)
        {
            var sb = new StringBuilder();
            PrintJsonNode(node, sb, 0);
            return sb.ToString();
        }

        private static void PrintJsonNode(AstNode node, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 2);
            sb.AppendLine(indentStr + "{");

            sb.AppendLine(indentStr + $"  \"type\": \"{node.GetNodeType()}\",");
            sb.AppendLine(indentStr + $"  \"line\": {node.Line},");
            sb.AppendLine(indentStr + $"  \"position\": {node.Position},");

            var attrs = node.GetAttributes();
            if (attrs.Count > 0)
            {
                sb.AppendLine(indentStr + "  \"attributes\": {");
                int i = 0;
                foreach (var attr in attrs)
                {
                    i++;
                    string comma = i < attrs.Count ? "," : "";
                    string valueStr;
                    if (attr.Value is bool b)
                        valueStr = b.ToString().ToLower();
                    else if (attr.Value is string s)
                        valueStr = $"\"{s}\"";
                    else
                        valueStr = attr.Value?.ToString() ?? "null";

                    sb.AppendLine(indentStr + $"    \"{attr.Key}\": {valueStr}{comma}");
                }
                sb.AppendLine(indentStr + "  },");
            }

            var children = new List<AstNode>(node.GetChildren());
            if (children.Count > 0)
            {
                sb.AppendLine(indentStr + "  \"children\": [");
                for (int i = 0; i < children.Count; i++)
                {
                    PrintJsonNode(children[i], sb, indent + 2);
                    if (i < children.Count - 1)
                        sb.AppendLine(indentStr + "    ,");
                }
                sb.AppendLine(indentStr + "  ]");
            }
            else
            {
                if (sb.ToString().EndsWith(",\r\n"))
                {
                    sb.Length -= 3;
                    sb.AppendLine();
                }
            }

            sb.Append(indentStr + "}");
            if (indent > 0)
                sb.AppendLine();
        }
    }
}