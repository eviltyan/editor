using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }
        public bool HasErrors { get; set; }

        public abstract string GetNodeType();
        public abstract IEnumerable<AstNode> GetChildren();
        public virtual Dictionary<string, object> GetAttributes() => new Dictionary<string, object>();
    }

    public class VectorDeclNode : AstNode
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public AstNode Initializer { get; set; }
        public bool IsNull { get; set; }

        public override string GetNodeType() => "VectorDecl";
        public override IEnumerable<AstNode> GetChildren()
        {
            if (Initializer != null)
                yield return Initializer;
        }
        public override Dictionary<string, object> GetAttributes() => new Dictionary<string, object>
        {
            ["name"] = Name,
            ["type"] = Type,
            ["isNull"] = IsNull
        };
    }

    public class FuncCallNode : AstNode
    {
        public string FunctionName { get; set; } = "c";

        public List<AstNode> Arguments { get; set; } = new List<AstNode>();

        public override string GetNodeType() => "FuncCall";
        public override IEnumerable<AstNode> GetChildren() => Arguments;
        public override Dictionary<string, object> GetAttributes() => new Dictionary<string, object>
        {
            ["name"] = FunctionName
        };
    }

    public class NumberLiteralNode : AstNode
    {
        public string Value { get; set; }
        public string Type { get; set; }

        public bool IsInteger { get; set; }

        public override string GetNodeType() => "NumberLiteral";
        public override IEnumerable<AstNode> GetChildren() => new AstNode[0];
        public override Dictionary<string, object> GetAttributes() => new Dictionary<string, object>
        {
            ["type"] = Type,
            ["value"] = Value
        };
    }

    public class CharacterLiteralNode : AstNode
    {
        public string Value { get; set; }
        public string Type { get; set; } = "character";

        public override string GetNodeType() => "CharacterLiteral";
        public override IEnumerable<AstNode> GetChildren() => new AstNode[0];
        public override Dictionary<string, object> GetAttributes() => new Dictionary<string, object>
        {
            ["type"] = Type,
            ["value"] = Value
        };
    }

    public class LogicalLiteralNode : AstNode
    {
        public bool Value { get; set; }
        public string Type { get; set; } = "logical";

        public override string GetNodeType() => "LogicalLiteral";
        public override IEnumerable<AstNode> GetChildren() => new AstNode[0];
        public override Dictionary<string, object> GetAttributes() => new Dictionary<string, object>
        {
            ["type"] = Type,
            ["value"] = Value
        };
    }

    public class NullLiteralNode : AstNode
    {
        public string Type { get; set; } = "NULL";

        public override string GetNodeType() => "NullLiteral";
        public override IEnumerable<AstNode> GetChildren() => new AstNode[0];
        public override Dictionary<string, object> GetAttributes() => new Dictionary<string, object>
        {
            ["type"] = Type,
            ["value"] = "NULL"
        };
    }
}