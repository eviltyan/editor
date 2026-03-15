using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace editor
{
    public partial class Form1 : Form
    {
        private int newDocumentCounter = 2; // По умолчанию при запуске программы открыт "Документ 1"
        private Dictionary<TabPage, DocumentInfo> documentInfo = new Dictionary<TabPage, DocumentInfo>();

        private Font tabFont;
        private Dictionary<TabPage, Rectangle> closeButtons = new Dictionary<TabPage, Rectangle>();

        private LexicalAnalyzer analyzer;

        public Form1()
        {
            InitializeComponent();

            analyzer = new LexicalAnalyzer();

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += TabControl1_DrawItem;
            tabControl1.MouseDown += TabControl1_MouseDown;

            tabFont = this.Font;

            DocumentInfo info = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                IsSaved = false,
                OriginalTabName = "Документ 1"
            };

            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);

            info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
            documentInfo[tabControl1.SelectedTab] = info;

            this.StartPosition = FormStartPosition.CenterScreen;

            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView.Columns.Add("Code", "Условный код");
            dataGridView.Columns.Add("Type", "Тип лексемы");
            dataGridView.Columns["Type"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns.Add("Value", "Лексема");
            dataGridView.Columns.Add("Location", "Местоположение");
            dataGridView.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns.Add("IsError", "Ошибка");
            dataGridView.Columns.Add("ErrorMessage", "Сообщение об ошибке");

            dataGridView.Columns["IsError"].Visible = false;
            dataGridView.Columns["ErrorMessage"].Visible = false;
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

            DataGridView dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView.Columns.Add("Code", "Условный код");
            dataGridView.Columns.Add("Type", "Тип лексемы");
            dataGridView.Columns["Type"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns.Add("Value", "Лексема");
            dataGridView.Columns.Add("Location", "Местоположение");
            dataGridView.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns.Add("IsError", "Ошибка");
            dataGridView.Columns.Add("ErrorMessage", "Сообщение об ошибке");

            dataGridView.Columns["IsError"].Visible = false;
            dataGridView.Columns["ErrorMessage"].Visible = false;

            dataGridView.CellClick += ResultGridView_CellClick;

            splitContainer.Panel1.Controls.Add(richTextBoxEdit);
            splitContainer.Panel2.Controls.Add(dataGridView);
            newPage.Controls.Add(splitContainer);

            DocumentInfo info = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                IsSaved = false,
                OriginalTabName = tabName
            };

            RichTextBox editBox = GetEditRichTextBox(newPage);
            editBox.TextChanged += RichTextBox_TextChanged;

            info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
            documentInfo[newPage] = info;

            tabControl1.TabPages.Add(newPage);

            tabControl1.SelectedTab = newPage;

            UpdateUndoRedoButtons();
        }

        private RichTextBox GetEditRichTextBox(TabPage page)
        {
            SplitContainer split = page.Controls[0] as SplitContainer;
            return split.Panel1.Controls[0] as RichTextBox;
        }

        private void AddTextState(RichTextBox editBox, TabPage page)
        {
            DocumentInfo info = documentInfo[page];
            info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
            UpdateUndoRedoButtons();
        }

        private void Undo()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            if (info.History.CanUndo)
            {
                RichTextBox editBox = GetEditRichTextBox(currentPage);

                editBox.TextChanged -= RichTextBox_TextChanged;

                TextState previousState = info.History.Undo();
                if (previousState != null)
                {
                    editBox.Text = previousState.Text;
                    editBox.SelectionStart = previousState.SelectionStart;
                    editBox.SelectionLength = previousState.SelectionLength;
                }

                editBox.TextChanged += RichTextBox_TextChanged;

                UpdateModifiedState(currentPage);
                UpdateUndoRedoButtons();
            }
        }

        private void Redo()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            if (info.History.CanRedo)
            {
                RichTextBox editBox = GetEditRichTextBox(currentPage);

                editBox.TextChanged -= RichTextBox_TextChanged;

                TextState nextState = info.History.Redo();
                if (nextState != null)
                {
                    editBox.Text = nextState.Text;
                    editBox.SelectionStart = nextState.SelectionStart;
                    editBox.SelectionLength = nextState.SelectionLength;
                }

                editBox.TextChanged += RichTextBox_TextChanged;

                UpdateModifiedState(currentPage);
                UpdateUndoRedoButtons();
            }
        }

        private void UndoAll()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            DialogResult result = MessageBox.Show(
                "Отменить все изменения? Это действие нельзя будет отменить.",
                "Отмена всех изменений",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                RichTextBox editBox = GetEditRichTextBox(currentPage);

                editBox.TextChanged -= RichTextBox_TextChanged;

                TextState firstState = info.History.UndoAll();
                if (firstState != null)
                {
                    editBox.Text = firstState.Text;
                    editBox.SelectionStart = firstState.SelectionStart;
                    editBox.SelectionLength = firstState.SelectionLength;
                }

                editBox.TextChanged += RichTextBox_TextChanged;

                UpdateModifiedState(currentPage);
                UpdateUndoRedoButtons();
            }
        }

        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0) return;

            RichTextBox editBox = sender as RichTextBox;
            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            AddTextState(editBox, currentPage);

            if (!info.IsModified && !currentPage.Text.EndsWith("*"))
            {
                currentPage.Text += "*";
                info.IsModified = true;
            }

            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            if (tabControl1.TabPages.Count == 0)
            {
                backButton.Enabled = false;
                forwardButton.Enabled = false;
                cancelButton.Enabled = false;

                if (отменитьToolStripMenuItem != null) отменитьToolStripMenuItem.Enabled = false;
                if (вернутьToolStripMenuItem != null) вернутьToolStripMenuItem.Enabled = false;
                if (отменитьВсеИзмененияToolStripMenuItem != null) отменитьВсеИзмененияToolStripMenuItem.Enabled = false;
                return;
            }

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];

            if (info.IsSaved)
            {
                info.History.CanUndo = false;
                info.History.CanRedo = false;
            }

            bool canUndo = info.History.CanUndo;
            bool canRedo = info.History.CanRedo;
            bool canUndoAll = (info.History.GetCurrentState() != null); //&&
                                                                        // info.History.GetCurrentState() != info.History.UndoAll()); // Есть изменения

            if (info.IsSaved)
            {
                canUndoAll = false;
            }

            backButton.Enabled = canUndo;
            forwardButton.Enabled = canRedo;
            cancelButton.Enabled = canUndoAll;

            if (отменитьToolStripMenuItem != null) отменитьToolStripMenuItem.Enabled = canUndo;
            if (вернутьToolStripMenuItem != null) вернутьToolStripMenuItem.Enabled = canRedo;
            if (отменитьВсеИзмененияToolStripMenuItem != null) отменитьВсеИзмененияToolStripMenuItem.Enabled = canUndoAll;
        }

        private void UpdateModifiedState(TabPage page)
        {
            DocumentInfo info = documentInfo[page];
            RichTextBox editBox = GetEditRichTextBox(page);

            TextState currentState = info.History.GetCurrentState();
            bool isModified = (currentState != null && editBox.Text != currentState.Text) || info.IsSaved == false;

            if (info.IsModified != isModified)
            {
                info.IsModified = isModified;

                if (isModified && !page.Text.EndsWith("*"))
                {
                    page.Text += "*";
                }
                else if (!isModified && page.Text.EndsWith("*"))
                {
                    page.Text = page.Text.TrimEnd('*');
                }
            }
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
                        info.IsSaved = true;
                        currentPage.Text = Path.GetFileName(filePath);

                        info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
                        documentInfo[tabControl1.SelectedTab] = info;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии: {ex.Message}");
                        tabControl1.TabPages.Remove(currentPage);
                        documentInfo.Remove(currentPage);
                    }

                    SplitContainer splitReadOnly = currentPage.Controls[0] as SplitContainer;
                    DataGridView dataGridView = splitReadOnly.Panel2.Controls[0] as DataGridView;
                    dataGridView.Rows.Clear();
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
            UpdateUndoRedoButtons();
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

                    info.FilePath = saveFileDialog.FileName;
                    info.IsModified = false;
                    info.IsSaved = true;
                    currentPage.Text = Path.GetFileName(saveFileDialog.FileName);
                }
                UpdateUndoRedoButtons();
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
                    File.WriteAllText(filePath, editBox.Text, Encoding.UTF8);
                }

                DocumentInfo info = documentInfo[page];
                info.IsModified = false;
                info.IsSaved = true;

                info.History.Clear();
                info.History.AddState(new TextState(editBox.Text, 0, 0));

                info.FilePath = filePath;

                page.Text = Path.GetFileName(filePath);

                UpdateUndoRedoButtons();
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

        private void TabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl1.TabPages[e.Index];
            Rectangle tabRect = tabControl1.GetTabRect(e.Index);

            Brush textBrush = SystemBrushes.ControlText;
            if (e.State == DrawItemState.Selected)
            {
                textBrush = new SolidBrush(Color.Black);
            }

            string tabText = page.Text;
            SizeF textSize = e.Graphics.MeasureString(tabText, tabFont);
            float textX = tabRect.X + 5;
            float textY = tabRect.Y + (tabRect.Height - textSize.Height) / 2;
            e.Graphics.DrawString(tabText, tabFont, textBrush, textX, textY);

            int closeSize = 16;
            int closeX = tabRect.Right - closeSize - 5;
            int closeY = tabRect.Y + (tabRect.Height - closeSize) / 2;
            Rectangle closeRect = new Rectangle(closeX, closeY, closeSize, closeSize);

            tabControl1.SizeMode = TabSizeMode.Normal;
            tabControl1.Padding = new Point(15, 5);

            closeButtons[page] = closeRect;

            using (Pen pen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawLine(pen, closeX + 3, closeY + 3, closeX + closeSize - 3, closeY + closeSize - 3);
                e.Graphics.DrawLine(pen, closeX + closeSize - 3, closeY + 3, closeX + 3, closeY + closeSize - 3);
            }
        }

        private void TabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                TabPage page = tabControl1.TabPages[i];

                if (closeButtons.ContainsKey(page) && closeButtons[page].Contains(e.Location))
                {
                    CloseTab(page);
                    break;
                }
            }
        }

        private void CloseTab(TabPage page)
        {
            DocumentInfo info = documentInfo[page];

            if (info.IsModified)
            {
                DialogResult result = MessageBox.Show(
                    $"Сохранить изменения в документе '{info.DisplayName}'?",
                    "Несохраненные изменения",
                    MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    tabControl1.SelectedTab = page;
                    saveDocument();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            tabControl1.TabPages.Remove(page);
            documentInfo.Remove(page);
            closeButtons.Remove(page);
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                if (documentInfo.ContainsKey(page) && documentInfo[page].IsModified)
                {
                    tabControl1.SelectedTab = page;

                    DialogResult result = MessageBox.Show(
                        $"Документ '{documentInfo[page].DisplayName}' имеет несохраненные изменения.\nСохранить перед выходом?",
                        "Несохраненные изменения",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        saveDocument();

                        if (documentInfo[page].IsModified)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        private void copyText()
        {
            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);
            if (editBox != null && editBox.SelectionLength > 0)
            {
                editBox.Copy();
            }
            else
            {
                MessageBox.Show("Нет выделенного текста для копирования!",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            copyText();
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyText();
        }

        private void cutText()
        {
            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);
            if (editBox != null && editBox.SelectionLength > 0)
            {
                editBox.Cut();
            }
            else
            {
                MessageBox.Show("Нет выделенного текста для вырезания!",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void cutButton_Click(object sender, EventArgs e)
        {
            cutText();
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cutText();
        }

        private void pasteText()
        {
            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);
            if (editBox != null)
            {
                if (Clipboard.ContainsText())
                {
                    editBox.Paste();
                }
                else
                {
                    MessageBox.Show("Буфер обмена пуст или содержит не текст!",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void pasteButton_Click(object sender, EventArgs e)
        {
            pasteText();
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteText();
        }

        private void выделитьВсёToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);
            if (editBox != null)
            {
                editBox.SelectAll();
                editBox.Focus();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            RichTextBox editBox = GetEditRichTextBox(tabControl1.SelectedTab);
            if (editBox == null) return base.ProcessCmdKey(ref msg, keyData);

            if (keyData == (Keys.Control | Keys.A))
            {
                editBox.SelectAll();
                return true;
            }

            if (keyData == (Keys.Control | Keys.C))
            {
                if (editBox.SelectionLength > 0)
                    editBox.Copy();
                return true;
            }

            if (keyData == (Keys.Control | Keys.X))
            {
                if (editBox.SelectionLength > 0)
                    editBox.Cut();
                return true;
            }

            if (keyData == (Keys.Control | Keys.V))
            {
                if (Clipboard.ContainsText())
                    editBox.Paste();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }



        private void startButton_Click(object sender, EventArgs e)
        {
            StartAnalysis();
        }
        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartAnalysis();
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void вернутьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void forwardButton_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            UndoAll();
        }

        private void отменитьВсеИзмененияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoAll();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUndoRedoButtons();
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InfoForm info = new InfoForm("Справка");
            info.Show();
        }

        private void infoButton_Click(object sender, EventArgs e)
        {
            InfoForm info = new InfoForm("Справка");
            info.Show();
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InfoForm info = new InfoForm("О программе");
            info.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InfoForm info = new InfoForm("О программе");
            info.Show();
        }

        private void StartAnalysis()
        {
            try
            {
                SplitContainer splitReadOnly = tabControl1.SelectedTab.Controls[0] as SplitContainer;
                DataGridView dataGridView = splitReadOnly.Panel2.Controls[0] as DataGridView;

                RichTextBox richTextBox = GetEditRichTextBox(tabControl1.SelectedTab);
                richTextBox.SelectionStart = richTextBox.Text.Length;
                richTextBox.ScrollToCaret();
                string input = richTextBox.Text;
                var tokens = analyzer.Analyze(input);

                dataGridView.Rows.Clear();
                dataGridView.Columns["IsError"].Visible = false;
                dataGridView.Columns["ErrorMessage"].Visible = false;

                foreach (var token in tokens)
                {
                    int rowIndex = dataGridView.Rows.Add(
                        token.Code,
                        token.Type,
                        token.Value,
                        token.Location,
                        token.IsError,
                        token.ErrorMessage
                    );

                    if (token.IsError)
                    {
                        dataGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                        dataGridView.Columns["IsError"].Visible = true;
                        dataGridView.Columns["ErrorMessage"].Visible = true;
                        dataGridView.Columns["ErrorMessage"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                }

                if (tokens.Count == 0)
                {
                    MessageBox.Show("Нет лексем для анализа.", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResultGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                SplitContainer splitReadOnly = tabControl1.SelectedTab.Controls[0] as SplitContainer;
                DataGridView dataGridView = splitReadOnly.Panel2.Controls[0] as DataGridView;

                var row = dataGridView.Rows[e.RowIndex];

                string location = row.Cells["Location"].Value?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var parts = location.Replace("строка ", "").Split(',');
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int line))
                        {
                            var positions = parts[1].Trim().Split('-');
                            if (positions.Length == 2 && int.TryParse(positions[0], out int startPos))
                            {
                                NavigateToPosition(line, startPos);
                            }
                        }
                    }
                }
            }
        }

        private void NavigateToPosition(int line, int position)
        {
            RichTextBox richTextBox = GetEditRichTextBox(tabControl1.SelectedTab);
            string[] lines = richTextBox.Lines;
            if (line <= lines.Length)
            {
                int charIndex = 0;
                for (int i = 0; i < line - 1; i++)
                {
                    charIndex += lines[i].Length;
                }
                charIndex += position + (1 * line - 2);

                if (charIndex >= 0 && charIndex <= richTextBox.TextLength)
                {
                    richTextBox.Focus();
                    richTextBox.Select(charIndex, 1);
                    richTextBox.ScrollToCaret();
                }
            }
        }
    }
}
