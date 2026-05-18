using System;
using System.Windows.Forms;

namespace editor
{
    public class LineNumberRichTextBox : RichTextBox
    {
        public LineNumberRichTextBox()
        {
            this.Multiline = true;
            this.ScrollBars = RichTextBoxScrollBars.Both;
            this.WordWrap = false;
        }
    }
}