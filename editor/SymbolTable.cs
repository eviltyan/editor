using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class SymbolInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
        public VectorDeclNode Declaration { get; set; }
    }

    public class SymbolTable
    {
        private Dictionary<string, SymbolInfo> symbols = new Dictionary<string, SymbolInfo>();
        private List<SemanticError> errors = new List<SemanticError>();

        public bool Declare(string name, string type, int line, int position, VectorDeclNode declaration)
        {
            if (!CheckDuplicate(name, line, position))
            {
                return false;
            }

            symbols[name] = new SymbolInfo
            {
                Name = name,
                Type = type,
                Line = line,
                Position = position,
                Declaration = declaration
            };
            return true;
        }

        public bool CheckDuplicate(string name, int line, int position)
        {
            if (symbols.ContainsKey(name))
            {
                var existing = symbols[name];
                errors.Add(new SemanticError
                {
                    Message = $"Ошибка: идентификатор \"{name}\" уже объявлен ранее",
                    Line = line,
                    Position = position,
                    Fragment = name
                });
                return false;
            }
            return true;
        }

        public bool Contains(string name)
        {
            return symbols.ContainsKey(name);
        }

        public void AddError(SemanticError error)
        {
            errors.Add(error);
        }

        public List<SemanticError> GetErrors() => errors;
        public void Clear() => symbols.Clear();
        public void ClearErrors() => errors.Clear();
    }

    public class SemanticError
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
        public string Fragment { get; set; }
        public string Location => $"строка {Line}, позиция {Position}";
    }
}