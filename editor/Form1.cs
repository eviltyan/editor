namespace editor
{
    public partial class Form1 : Form
    {
        private int newDocumentCounter = 2; // По умолчанию при запуске программы открыт "Документ 1"

        public Form1()
        {
            InitializeComponent();
        }

        private void createNewDocument()
        {
            TabPage newPage = new TabPage();
            newPage.Text = $"Документ {newDocumentCounter++}";

            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.SplitterDistance = splitContainer.Height / 2;

            RichTextBox richTextBoxEdit = new RichTextBox();
            richTextBoxEdit.Dock = DockStyle.Fill;
            richTextBoxEdit.AcceptsTab = true;

            RichTextBox richTextBoxReadOnly = new RichTextBox();
            richTextBoxReadOnly.Dock = DockStyle.Fill;
            richTextBoxReadOnly.ReadOnly = true;
            richTextBoxReadOnly.BackColor = Color.White;
            richTextBoxReadOnly.TabStop = false;

            splitContainer.Panel1.Controls.Add(richTextBoxEdit);
            splitContainer.Panel2.Controls.Add(richTextBoxReadOnly);
            newPage.Controls.Add(splitContainer);

            tabControl1.TabPages.Add(newPage);

            tabControl1.SelectedTab = newPage;
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            createNewDocument();
        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            createNewDocument();
        }

        private void openDocument()
        {
            if (tabControl1.TabPages.Count == 0) return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Files|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    TabPage currentPage = tabControl1.SelectedTab;

                    SplitContainer split = currentPage.Controls[0] as SplitContainer;
                    RichTextBox editBox = split.Panel1.Controls[0] as RichTextBox;

                    if (openFileDialog.FileName.EndsWith(".rtf"))
                        editBox.LoadFile(openFileDialog.FileName, RichTextBoxStreamType.RichText);
                    else
                    {
                        using (StreamReader reader = new StreamReader(openFileDialog.FileName, true)) // true = auto-detect encoding
                        {
                            editBox.Text = reader.ReadToEnd();
                        }
                    }

                    currentPage.Text = Path.GetFileName(openFileDialog.FileName);

                    SplitContainer splitReadOnly = currentPage.Controls[0] as SplitContainer;
                    RichTextBox readOnlyBox = splitReadOnly.Panel2.Controls[0] as RichTextBox;
                    readOnlyBox.Clear();
                }
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            openDocument();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openDocument();
        }
    }
}
