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
    }
}
