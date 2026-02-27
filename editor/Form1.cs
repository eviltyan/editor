using System.Text;

namespace editor
{
    public partial class Form1 : Form
    {
        private int newDocumentCounter = 2; // По умолчанию при запуске программы открыт "Документ 1"
        private Dictionary<TabPage, DocumentInfo> documentInfo = new Dictionary<TabPage, DocumentInfo>();

        public Form1()
        {
            InitializeComponent();

            documentInfo[tabControl1.SelectedTab] = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                OriginalTabName = "Документ 1"
            };

            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);
            editBox.TextChanged += (s, e) =>
            {
                documentInfo[tabControl1.SelectedTab].IsModified = true;
                if (!tabControl1.SelectedTab.Text.EndsWith("*"))
                    tabControl1.SelectedTab.Text += "*";
            };
        }

        private void createNewDocument()
        {
            TabPage newPage = new TabPage();
            string tabName = $"Документ {newDocumentCounter++}";
            newPage.Text = tabName;

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

            documentInfo[newPage] = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                OriginalTabName = tabName
            };

            RichTextBox editBox = GetEditRichTextBox(newPage);
            editBox.TextChanged += (s, e) =>
            {
                documentInfo[newPage].IsModified = true;
                if (!newPage.Text.EndsWith("*"))
                    newPage.Text += "*";
            };

            tabControl1.TabPages.Add(newPage);

            tabControl1.SelectedTab = newPage;
        }

        private RichTextBox GetEditRichTextBox(TabPage page)
        {
            SplitContainer split = page.Controls[0] as SplitContainer;
            return split.Panel1.Controls[0] as RichTextBox;
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
            createNewDocument();
            if (tabControl1.TabPages.Count == 0) return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Files|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    TabPage currentPage = tabControl1.SelectedTab;
                    string filePath = openFileDialog.FileName;

                    SplitContainer split = currentPage.Controls[0] as SplitContainer;
                    RichTextBox editBox = split.Panel1.Controls[0] as RichTextBox;

                    try
                    {
                        if (filePath.EndsWith(".rtf"))
                            editBox.LoadFile(filePath, RichTextBoxStreamType.RichText);
                        else
                        {
                            using (StreamReader reader = new StreamReader(filePath, true)) // true = auto-detect encoding
                            {
                                editBox.Text = reader.ReadToEnd();
                            }
                        }

                        DocumentInfo info = documentInfo[currentPage];
                        info.FilePath = filePath;
                        info.IsModified = false;
                        currentPage.Text = Path.GetFileName(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии: {ex.Message}");
                        tabControl1.TabPages.Remove(currentPage);
                        documentInfo.Remove(currentPage);
                    }

                    SplitContainer splitReadOnly = currentPage.Controls[0] as SplitContainer;
                    RichTextBox readOnlyBox = splitReadOnly.Panel2.Controls[0] as RichTextBox;
                    readOnlyBox.Clear();
                }
                else
                {
                    tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                    documentInfo.Remove(tabControl1.SelectedTab);
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

        private void saveDocument()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            if (info.IsNewDocument)
            {
                saveDocumentAs();
            }
            else
            {
                SaveToFile(currentPage, info.FilePath);
            }
        }

        private void saveDocumentAs()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Rich Text Files|*.rtf|Text Files|*.txt";
                saveFileDialog.DefaultExt = "rtf";
                saveFileDialog.FileName = info.IsNewDocument ? info.OriginalTabName : Path.GetFileName(info.FilePath);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveToFile(currentPage, saveFileDialog.FileName);

                    // Обновляем информацию о документе
                    info.FilePath = saveFileDialog.FileName;
                    info.IsModified = false;
                    currentPage.Text = Path.GetFileName(saveFileDialog.FileName);
                }
            }
        }

        private void SaveAllDocuments()
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                tabControl1.SelectedTab = page; // Переключаемся на вкладку
                saveDocument(); // Сохраняем
            }
        }

        private void SaveToFile(TabPage page, string filePath)
        {
            try
            {
                RichTextBox editBox = GetEditRichTextBox(page);

                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".rtf")
                {
                    editBox.SaveFile(filePath, RichTextBoxStreamType.RichText);
                }
                else
                {
                    // Для txt сохраняем в UTF-8
                    File.WriteAllText(filePath, editBox.Text, Encoding.UTF8);
                }

                // Обновляем состояние
                DocumentInfo info = documentInfo[page];
                info.IsModified = false;

                // Убираем звездочку из названия, если была
                page.Text = Path.GetFileName(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            saveDocument();
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveDocument();
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveDocumentAs();
        }
    }
}
