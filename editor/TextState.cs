using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class TextState
    {
        public string Text { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }

        public TextState(string text, int selectionStart, int selectionLength)
        {
            Text = text;
            SelectionStart = selectionStart;
            SelectionLength = selectionLength;
        }
    }
}
