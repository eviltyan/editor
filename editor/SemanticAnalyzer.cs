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
            position++;

            if (!IsValidIdentifierName(vectorName, node.Line, node.Position))
            {
                currentNodeHasErrors = true;
            }

            SkipSpaces();

            if (position >= tokens.Count || tokens[position].Value != "<-")
            {
                return null;
            }
            position++;

            SkipSpaces();

            if (position >= tokens.Count)
                return null;

            if (symbolTable.Contains(vectorName))
            {
                var existing = symbolTable.Lookup(vectorName, node.Line, node.Position);
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
                    symbolTable.Declare(node.Name, node.Type, node.Line, node.Position, node);
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

                    if (!currentNodeHasErrors)
                    {
                        symbolTable.Declare(node.Name, vectorType, node.Line, node.Position, node);
                    }

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
            double numValue;
            try
            {
                numValue = double.Parse(value);
                if (double.IsInfinity(numValue))
                {
                    finalValue = numValue > 0 ? "Inf" : "-Inf";
                    isInteger = false;
                    type = "numeric";

                    errors.Add(new SemanticError
                    {
                        Message = $"Примечание: число '{value}' слишком велико и преобразовано в {finalValue}",
                        Line = line,
                        Position = position,
                        Fragment = value
                    });
                    return false;
                }


                if (isInteger)
                {
                    const int R_INT_MIN = -2147483647;
                    const int R_INT_MAX = 2147483647;

                    if (numValue < R_INT_MIN || numValue > R_INT_MAX)
                    {
                        isInteger = false;
                        finalValue = value;
                        type = "numeric";

                        errors.Add(new SemanticError
                        {
                            Message = $"Примечание: значение '{value}' выходит за пределы integer и преобразовано в numeric",
                            Line = line,
                            Position = position,
                            Fragment = value
                        });
                        return false;
                    }
                }
            }
            catch 
            {
                string trimmed = value.TrimStart('-');

                if (trimmed.Contains("."))
                {
                    int dotIndex = trimmed.IndexOf('.');
                    string afterDot = trimmed.Substring(dotIndex + 1);

                    int leadingZeros = 0;
                    foreach (char c in afterDot)
                    {
                        if (c == '0')
                            leadingZeros++;
                        else if (c >= '1' && c <= '9')
                            break;
                    }

                    if (leadingZeros >= 308)
                    {
                        finalValue = "0";
                        isInteger = false;

                        errors.Add(new SemanticError
                        {
                            Message = $"Примечание: число '{value}' имеет более 308 нулей после запятой и преобразовано в 0",
                            Line = line,
                            Position = position,
                            Fragment = value
                        });
                        return false;
                    }
                }
            }
            
            finalValue = value;
            return false;
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