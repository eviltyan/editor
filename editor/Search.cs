using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class SearchResult
    {
        public string Substring { get; set; }
        public int LineNumber { get; set; }
        public int PositionInLine { get; set; }
        public int GlobalPosition { get; set; }
        public int Length { get; set; }
        public string LineText { get; set; }
    }

    public class SearchPatternInfo
    {
        public string Pattern { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }
}
