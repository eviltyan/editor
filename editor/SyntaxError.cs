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
}
