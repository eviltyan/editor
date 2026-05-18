using System;
using System.Drawing;
using System.Windows.Forms;

namespace editor
{
    public class LineNumberPanel : Panel
    {
        private RichTextBox targetRichTextBox;

        public LineNumberPanel(RichTextBox target)
        {
            targetRichTextBox = target;
            this.Width = 50;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.BorderStyle = BorderStyle.None;
            this.DoubleBuffered = true;

            targetRichTextBox.SelectionChanged += (s, e) => this.Invalidate();
            targetRichTextBox.VScroll += (s, e) => this.Invalidate();
            targetRichTextBox.TextChanged += (s, e) => this.Invalidate();
            targetRichTextBox.Resize += (s, e) => this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (targetRichTextBox == null || targetRichTextBox.IsDisposed) return;

            e.Graphics.Clear(Color.FromArgb(240, 240, 240));

            using (Pen pen = new Pen(Color.FromArgb(180, 180, 180)))
            {
                e.Graphics.DrawLine(pen, this.Width - 1, 0, this.Width - 1, this.Height);
            }

            try
            {
                Font textFont = targetRichTextBox.Font;

                int firstCharIndex = targetRichTextBox.GetCharIndexFromPosition(new Point(1, 1));
                int firstLine = targetRichTextBox.GetLineFromCharIndex(firstCharIndex);
                Point firstLinePos = targetRichTextBox.GetPositionFromCharIndex(firstCharIndex);

                int lineHeight;
                if (firstLine + 1 < targetRichTextBox.Lines.Length)
                {
                    int nextLineIndex = targetRichTextBox.GetFirstCharIndexFromLine(firstLine + 1);
                    Point nextLinePos = targetRichTextBox.GetPositionFromCharIndex(nextLineIndex);
                    lineHeight = nextLinePos.Y - firstLinePos.Y;
                }
                else
                {
                    lineHeight = textFont.Height;
                }

                if (lineHeight < 8) lineHeight = 20;

                using (Brush textBrush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                {
                    for (int i = 0; i < targetRichTextBox.Lines.Length; i++)
                    {
                        int lineNumber = i + 1;
                        int yPos = firstLinePos.Y + (i - firstLine) * lineHeight;

                        if (yPos >= -lineHeight && yPos < this.Height + lineHeight)
                        {
                            string lineNumText = lineNumber.ToString();
                            SizeF textSize = e.Graphics.MeasureString(lineNumText, this.Font);
                            float xPos = this.Width - textSize.Width - 5;

                            float yPosCentered = yPos + (lineHeight - this.Font.Height) / 2;

                            e.Graphics.DrawString(lineNumText, this.Font, textBrush, xPos, yPosCentered);
                        }
                    }
                }
            }
            catch (Exception) { }
        }
    }
}