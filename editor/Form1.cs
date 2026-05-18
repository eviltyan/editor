using System.Diagnostics;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace editor
{
    public partial class Form1 : Form
    {
        private int newDocumentCounter = 2;
        private Dictionary<TabPage, DocumentInfo> documentInfo = new Dictionary<TabPage, DocumentInfo>();

        private Font tabFont;
        private Dictionary<TabPage, Rectangle> closeButtons = new Dictionary<TabPage, Rectangle>();

        private LexicalAnalyzer analyzer;
        private SyntaxAutomaton syntax;

        private float currentGlobalEditZoom = 1.0f;
        private float currentGlobalGridFontSize = 8f;

        public Form1()
        {
            InitializeComponent();

            analyzer = new LexicalAnalyzer();
            syntax = new SyntaxAutomaton();

            LocalizationManager.Initialize(this);

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += TabControl1_DrawItem;
            tabControl1.MouseDown += TabControl1_MouseDown;

            RebuildFirstTab();

            var firstPage = tabControl1.SelectedTab;
            var errorsGrid = GetErrorsGrid(firstPage);
            var lexemesGrid = GetLexemesGrid(firstPage);

            if (errorsGrid != null) 
                ConfigureErrorsGrid(errorsGrid);
            if (lexemesGrid != null) 
                ConfigureLexemesGrid(lexemesGrid);

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

            tabControl1.SelectedIndexChanged += (s, e) => UpdateStatus();

            RichTextBox firstBox = GetEditRichTextBox(tabControl1.SelectedTab);
            DataGridView firstGrid = GetErrorsGrid(tabControl1.SelectedTab);

            if (firstBox != null)
            {
                currentGlobalEditZoom = firstBox.ZoomFactor;
                firstBox.SelectionChanged += UpdateCursorPosition;
            }

            if (firstGrid != null)
            {
                currentGlobalGridFontSize = firstGrid.Font.Size / 8f;
            }

            UpdateStatus();

            UpdateUILanguage();
        }

        private void RebuildFirstTab()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage firstPage = tabControl1.TabPages[0];

            string oldText = "";
            float oldZoom = 1.0f;

            if (firstPage.Controls.Count > 0 && firstPage.Controls[0] is SplitContainer oldSplit)
            {
                if (oldSplit.Panel1.Controls.Count > 0 && oldSplit.Panel1.Controls[0] is RichTextBox oldBox)
                {
                    oldText = oldBox.Text;
                    oldZoom = oldBox.ZoomFactor;
                }
            }

            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.SplitterDistance = splitContainer.Height / 2;

            TableLayoutPanel container = new TableLayoutPanel();
            container.Dock = DockStyle.Fill;
            container.ColumnCount = 2;
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            container.RowCount = 1;
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            LineNumberRichTextBox newBox = new LineNumberRichTextBox();
            newBox.Dock = DockStyle.Fill;
            newBox.AcceptsTab = true;
            newBox.Text = oldText;
            newBox.ZoomFactor = oldZoom;
            newBox.WordWrap = false;
            newBox.BorderStyle = BorderStyle.None;

            LineNumberPanel linePanel = new LineNumberPanel(newBox);
            linePanel.Dock = DockStyle.Fill;
            linePanel.Width = 50;
            linePanel.BackColor = Color.FromArgb(240, 240, 240);

            container.Controls.Add(linePanel, 0, 0);
            container.Controls.Add(newBox, 1, 0);

            TabControl resultTabs = new TabControl();
            resultTabs.Dock = DockStyle.Fill;

            TabPage errorsTab = new TabPage("Ошибки");
            DataGridView errorsGrid = new DataGridView();
            errorsGrid.Dock = DockStyle.Fill;
            errorsTab.Controls.Add(errorsGrid);

            TabPage lexemesTab = new TabPage("Лексемы");
            DataGridView lexemesGrid = new DataGridView();
            lexemesGrid.Dock = DockStyle.Fill;
            lexemesTab.Controls.Add(lexemesGrid);

            resultTabs.TabPages.Add(errorsTab);
            resultTabs.TabPages.Add(lexemesTab);

            splitContainer.Panel1.Controls.Add(container);
            splitContainer.Panel2.Controls.Add(resultTabs);

            firstPage.Controls.Clear();
            firstPage.Controls.Add(splitContainer);

            newBox.MouseWheel += EditBox_MouseWheel;
            newBox.TextChanged += RichTextBox_TextChanged;
            newBox.SelectionChanged += UpdateCursorPosition;
            newBox.TextChanged += HighlightSyntax;
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

            TableLayoutPanel container = new TableLayoutPanel();
            container.Dock = DockStyle.Fill;
            container.ColumnCount = 2;
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            container.RowCount = 1;
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            LineNumberRichTextBox richTextBoxEdit = new LineNumberRichTextBox();
            richTextBoxEdit.Dock = DockStyle.Fill;
            richTextBoxEdit.AcceptsTab = true;
            richTextBoxEdit.WordWrap = false;
            richTextBoxEdit.BorderStyle = BorderStyle.None;

            LineNumberPanel lineNumberPanel = new LineNumberPanel(richTextBoxEdit);
            lineNumberPanel.Dock = DockStyle.Fill;
            lineNumberPanel.Width = 50;

            container.Controls.Add(lineNumberPanel, 0, 0);
            container.Controls.Add(richTextBoxEdit, 1, 0);

            TabControl resultTabs = new TabControl();
            resultTabs.Dock = DockStyle.Fill;

            TabPage errorsTab = new TabPage("Ошибки");
            DataGridView errorsGrid = new DataGridView();
            errorsGrid.Dock = DockStyle.Fill;
            errorsGrid.AllowUserToAddRows = false;
            errorsGrid.AllowUserToDeleteRows = false;
            errorsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            errorsGrid.ReadOnly = true;
            errorsGrid.RowHeadersWidth = 70;
            errorsGrid.Columns.Add("Fragment", "Неверный фрагмент");
            errorsGrid.Columns["Fragment"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            errorsGrid.Columns.Add("Location", "Местоположение");
            errorsGrid.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            errorsGrid.Columns.Add("Description", "Описание ошибки");
            errorsGrid.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            errorsGrid.CellClick += ErrorGridView_CellClick;
            errorsTab.Controls.Add(errorsGrid);

            TabPage lexemesTab = new TabPage("Лексемы");
            DataGridView lexemesGrid = new DataGridView();
            lexemesGrid.Dock = DockStyle.Fill;
            lexemesGrid.AllowUserToAddRows = false;
            lexemesGrid.AllowUserToDeleteRows = false;
            lexemesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            lexemesGrid.ReadOnly = true;
            lexemesGrid.RowHeadersWidth = 70;
            lexemesGrid.Columns.Add("Code", "Код");
            lexemesGrid.Columns["Code"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            lexemesGrid.Columns.Add("Type", "Тип лексемы");
            lexemesGrid.Columns["Type"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            lexemesGrid.Columns.Add("Lexeme", "Лексема");
            lexemesGrid.Columns["Lexeme"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            lexemesGrid.Columns.Add("Location", "Местоположение");
            lexemesGrid.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            lexemesGrid.CellClick += LexemeGridView_CellClick;
            lexemesTab.Controls.Add(lexemesGrid);

            resultTabs.TabPages.Add(errorsTab);
            resultTabs.TabPages.Add(lexemesTab);

            splitContainer.Panel1.Controls.Add(container);
            splitContainer.Panel2.Controls.Add(resultTabs);
            newPage.Controls.Add(splitContainer);

            DocumentInfo info = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                IsSaved = false,
                OriginalTabName = tabName
            };

            richTextBoxEdit.TextChanged += RichTextBox_TextChanged;
            richTextBoxEdit.TextChanged += HighlightSyntax;
            richTextBoxEdit.SelectionChanged += UpdateCursorPosition;

            info.History.AddState(new TextState(richTextBoxEdit.Text, richTextBoxEdit.SelectionStart, richTextBoxEdit.SelectionLength));
            documentInfo[newPage] = info;

            tabControl1.TabPages.Add(newPage);
            tabControl1.SelectedTab = newPage;

            ApplyFontSizeToPage(newPage, currentGlobalEditZoom, currentGlobalGridFontSize);

            UpdateUndoRedoButtons();
            UpdateUILanguage();
            UpdateStatus();
        }

        private void AddTextState(RichTextBox editBox, TabPage page)
        {
            DocumentInfo info = documentInfo[page];
            info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
            UpdateUndoRedoButtons();
        }

        private void EditBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                RichTextBox editBox = sender as RichTextBox;
                if (editBox != null)
                {
                    float newSize = editBox.ZoomFactor + (e.Delta > 0 ? 0.1f : -0.1f);
                    newSize = Math.Max(0.5f, Math.Min(2.0f, newSize));
                    editBox.ZoomFactor = newSize;
                }
            }
        }

        private TabPage FindPageContainingControl(Control control)
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                SplitContainer split = page.Controls[0] as SplitContainer;
                if (split != null)
                {
                    if (split.Panel1.Controls.Contains(control))
                        return page;
                    if (split.Panel2.Controls.Contains(control))
                        return page;
                }
            }
            return null;
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
            UpdateStatus();
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
            bool canUndoAll = (info.History.GetCurrentState() != null);

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
                    RichTextBox editBox = GetEditRichTextBox(currentPage);

                    if (editBox == null)
                    {
                        MessageBox.Show("Не удалось получить редактор текста");
                        tabControl1.TabPages.Remove(currentPage);
                        documentInfo.Remove(currentPage);
                        return;
                    }

                    try
                    {
                        if (filePath.EndsWith(".rtf"))
                            editBox.LoadFile(filePath, RichTextBoxStreamType.RichText);
                        else
                        {
                            using (StreamReader reader = new StreamReader(filePath, true))
                            {
                                editBox.Text = reader.ReadToEnd();
                            }
                        }

                        HighlightSyntax(editBox, EventArgs.Empty);

                        DocumentInfo info = documentInfo[currentPage];
                        info.FilePath = filePath;
                        info.IsModified = false;
                        info.IsSaved = true;
                        currentPage.Text = Path.GetFileName(filePath);

                        info.History.Clear();
                        info.History.AddState(new TextState(editBox.Text, 0, 0));

                        UpdateStatus();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии: {ex.Message}");
                        tabControl1.TabPages.Remove(currentPage);
                        documentInfo.Remove(currentPage);
                    }
                }
                else
                {
                    tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                    documentInfo.Remove(tabControl1.SelectedTab);
                }
            }
            UpdateUILanguage();
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
            UpdateStatus();
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
            UpdateStatus();
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
                DialogResult result = LocalizationManager.ShowSaveDialog(info.DisplayName);

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

            UpdateStatus();
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
                    DocumentInfo info = documentInfo[page];

                    DialogResult result = LocalizationManager.ShowSaveDialog(info.DisplayName);

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

            if (keyData == (Keys.Control | Keys.N))
            {
                createNewDocument();
                return true;
            }

            if (keyData == (Keys.Control | Keys.O))
            {
                openDocument();
                return true;
            }

            if (keyData == (Keys.Control | Keys.S))
            {
                saveDocument();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                saveDocumentAs();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Z))
            {
                Undo();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.Z))
            {
                Redo();
                return true;
            }

            if (keyData == (Keys.Control | Keys.C))
            {
                if (editBox != null && editBox.SelectionLength > 0)
                {
                    editBox.Copy();
                }
                return true;
            }

            if (keyData == (Keys.Control | Keys.X))
            {
                if (editBox != null && editBox.SelectionLength > 0)
                {
                    editBox.Cut();
                }
                return true;
            }

            if (keyData == (Keys.Control | Keys.V))
            {
                if (editBox != null && Clipboard.ContainsText())
                {
                    editBox.Paste();
                }
                return true;
            }

            if (keyData == (Keys.Control | Keys.A))
            {
                if (editBox != null)
                {
                    editBox.SelectAll();
                }
                return true;
            }

            if (keyData == Keys.F5)
            {
                Analyze();
                return true;
            }

            if (keyData == Keys.F1)
            {
                string url = "editor.Руководство-пользователя.-Компилятор.html";
                openHtmlFile(url);
                return true;
            }

            if (keyData == Keys.F12)
            {
                InfoForm.ShowInstance("О программе");
                return true;
            }

            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Analyze();
        }
        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Analyze();
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
            string url = "editor.Руководство-пользователя.-Компилятор.html";
            openHtmlFile(url);
        }

        private void infoButton_Click(object sender, EventArgs e)
        {
            string url = "editor.Руководство-пользователя.-Компилятор.html";
            openHtmlFile(url);
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InfoForm.ShowInstance("О программе");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InfoForm.ShowInstance("О программе");
        }

        private void Analyze()
        {
            if (tabControl1.TabPages.Count == 0) return;

            TabPage currentPage = tabControl1.SelectedTab;
            RichTextBox editBox = GetEditRichTextBox(currentPage);
            DataGridView errorsGrid = GetErrorsGrid(currentPage);
            DataGridView lexemesGrid = GetLexemesGrid(currentPage);

            try
            {
                string input = editBox.Text;

                errorsGrid.Rows.Clear();
                lexemesGrid.Rows.Clear();

                LexicalAnalyzer analyzer = new LexicalAnalyzer();
                var tokens = analyzer.Analyze(input);

                List<SyntaxError> allErrors = new List<SyntaxError>();

                foreach (var token in tokens)
                {
                    if (token.IsError)
                    {
                        allErrors.Add(new SyntaxError
                        {
                            InvalidFragment = token.Value,
                            Line = token.Line,
                            Position = token.StartPos,
                            Description = LocalizationManager.TranslateError(token.ErrorMessage)
                        });

                        string displayValue = token.Value;
                        lexemesGrid.Rows[lexemesGrid.Rows.Count].DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);

                        lexemesGrid.Rows.Add(
                            token.Code,
                            GetTokenTypeName(token),
                            displayValue,
                            token.Location
                        );
                    }
                    else
                    {
                        string displayValue = token.Value;
                        if (token.Type == "space")
                            displayValue = LocalizationManager.GetString("spaceDisplay");

                        lexemesGrid.Rows.Add(
                            token.Code,
                            GetTokenTypeName(token),
                            displayValue,
                            token.Location
                        );

                        lexemesGrid.Rows[lexemesGrid.Rows.Count - 1].Tag = Tuple.Create(token.Line, token.StartPos);
                    }
                }

                SyntaxAutomaton syntax = new SyntaxAutomaton();
                dynamic syntaxErrors = syntax.Parse(tokens);

                foreach (var error in syntaxErrors)
                {
                    allErrors.Add(error);
                }

                var sortedErrors = allErrors.OrderBy(e => e.Line).ThenBy(e => e.Position).ToList();

                foreach (var error in sortedErrors)
                {
                    int rowIndex = errorsGrid.Rows.Add(
                        error.InvalidFragment,
                        error.Location,
                        error.Description
                    );

                    errorsGrid.Rows[rowIndex].Tag = Tuple.Create(error.Line, error.Position);

                    if (LocalizationManager.CurrentLanguage == "en")
                        errorsGrid.Rows[rowIndex].Cells[1].Value = $"line {error.Line}, position {error.Position}";
                    else
                        errorsGrid.Rows[rowIndex].Cells[1].Value = $"строка {error.Line}, позиция {error.Position}";

                    errorsGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                }

                int totalErrors = allErrors.Count;
                DataGridViewRow countRow = new DataGridViewRow();

                float currentFontSize = currentGlobalGridFontSize * 8;
                countRow.DefaultCellStyle.Font = new Font(errorsGrid.Font.FontFamily, currentFontSize, FontStyle.Bold);
                countRow.DefaultCellStyle.BackColor = totalErrors == 0 ? Color.FromArgb(220, 255, 220) : Color.FromArgb(255, 220, 220);
                countRow.DefaultCellStyle.ForeColor = totalErrors == 0 ? Color.Green : Color.Red;

                DataGridViewCell countCell = new DataGridViewTextBoxCell();
                if (totalErrors == 0)
                {
                    countCell.Value = LocalizationManager.GetString("totalErrorsZero");
                }
                else
                {
                    countCell.Value = LocalizationManager.FormatString("totalErrorsCount", totalErrors);
                }
                countRow.Cells.Add(countCell);
                countRow.Cells.Add(new DataGridViewTextBoxCell());
                countRow.Cells.Add(new DataGridViewTextBoxCell());
                errorsGrid.Rows.Add(countRow);

                if (totalErrors == 0)
                {
                    MessageBox.Show(LocalizationManager.GetString("analysisComplete"),
                        LocalizationManager.GetString("start"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(LocalizationManager.FormatString("errorsFound", totalErrors),
                        LocalizationManager.GetString("start"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetTokenTypeName(Token token)
        {
            if (token.IsError) return LocalizationManager.GetString("tokenError");

            string key = $"tokenType_{token.Type}";
            string translated = LocalizationManager.GetString(key);
            if (translated != key) return translated;

            return token.Type switch
            {
                "keyword" => "Ключевое слово",
                "id" => "Идентификатор",
                "integer" => "Целое число",
                "numeric" => "Вещественное число",
                "character" => "Строка",
                "assign" => "Присваивание",
                "leftparen" => "Открывающая скобка",
                "rightparen" => "Закрывающая скобка",
                "comma" => "Запятая",
                "minus" => "Минус",
                "end" => "Конец оператора",
                "space" => "Пробел",
                _ => token.Type ?? "Неизвестно"
            };
        }

        private void ErrorGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null) return;
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count - 1) return;

            var row = grid.Rows[e.RowIndex];

            string location = "";
            if (row.Cells.Count > 1 && row.Cells[1].Value != null)
            {
                location = row.Cells[1].Value.ToString();
            }

            if (string.IsNullOrEmpty(location) && grid.Columns.Contains("Location"))
            {
                location = row.Cells["Location"].Value?.ToString();
            }

            if (!string.IsNullOrEmpty(location))
            {
                int line = -1;
                int position = -1;

                if (location.Contains("строка"))
                {
                    var parts = location.Replace("строка ", "").Split(',');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out line);
                        var posPart = parts[1].Replace("позиция", "").Trim();
                        int.TryParse(posPart, out position);
                    }
                }
                else if (location.Contains("line"))
                {
                    var parts = location.Replace("line ", "").Split(',');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out line);
                        var posPart = parts[1].Replace("position", "").Trim();
                        int.TryParse(posPart, out position);
                    }
                }

                if (line > 0 && position >= 0)
                {
                    NavigateToPosition(line, position);
                }
            }
        }

        private void NavigateToPosition(int line, int position)
        {
            RichTextBox richTextBoxEd = GetEditRichTextBox(tabControl1.SelectedTab);
            if (richTextBoxEd == null) return;

            string[] lines = richTextBoxEd.Lines;

            if (line <= lines.Length && line >= 1)
            {
                int charIndex = 0;

                for (int i = 0; i < line - 1; i++)
                {
                    charIndex += lines[i].Length;
                    charIndex += Environment.NewLine.Length;
                }

                int positionInLine = Math.Min(position - 1, lines[line - 1].Length - 1);
                if (positionInLine < 0) positionInLine = 0;
                charIndex += positionInLine;

                if (charIndex >= 0 && charIndex < richTextBoxEd.TextLength)
                {
                    richTextBoxEd.Focus();
                    richTextBoxEd.Select(charIndex, 1);
                    richTextBoxEd.ScrollToCaret();

                    richTextBoxEd.SelectionColor = Color.Red;

                    System.Timers.Timer timer = new System.Timers.Timer(500);
                    timer.Elapsed += (s, args) =>
                    {
                        richTextBoxEd.Invoke(new Action(() =>
                        {
                            richTextBoxEd.SelectionColor = Color.Black;
                        }));
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }

        private void LexemeGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null) return;
            if (e.RowIndex < 0) return;

            var row = grid.Rows[e.RowIndex];

            string location = "";
            if (row.Cells.Count > 3 && row.Cells[3].Value != null)
            {
                location = row.Cells[3].Value.ToString();
            }

            if (string.IsNullOrEmpty(location) && grid.Columns.Contains("Location"))
            {
                location = row.Cells["Location"].Value?.ToString();
            }

            if (!string.IsNullOrEmpty(location))
            {
                int line = -1;
                int position = -1;

                if (location.Contains("строка"))
                {
                    var parts = location.Replace("строка ", "").Split(',');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out line);
                        var posPart = parts[1].Replace("позиция", "").Trim();
                        int.TryParse(posPart, out position);
                    }
                }
                else if (location.Contains("line"))
                {
                    var parts = location.Replace("line ", "").Split(',');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out line);
                        var posPart = parts[1].Replace("position", "").Trim();
                        int.TryParse(posPart, out position);
                    }
                }

                if (line > 0 && position >= 0)
                {
                    NavigateToPosition(line, position);
                }
            }
        }

        private void openHtmlFile(string url)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");
                using (Stream stream = assembly.GetManifestResourceStream(url))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string htmlContent = reader.ReadToEnd();
                    File.WriteAllText(tempFile, htmlContent);
                }

                Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
            }
            catch (Exception ex) { }
        }

        private void постановкаЗадачиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.ПостановкаЗадачи.html";
            openHtmlFile(url);
        }

        private void грамматикаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.Грамматика.html";
            openHtmlFile(url);
        }

        private void классификацияГрамматикиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.КлассификацияГрамматики.html";
            openHtmlFile(url);
        }

        private void методАнализаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.МетодАнализа.html";
            openHtmlFile(url);
        }

        private void текстовыйПримерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.ТекстовыйПример.html";
            openHtmlFile(url);
        }

        private void списокЛитературыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.СписокЛитературы.html";
            openHtmlFile(url);
        }

        private void исходныйКодПрограммыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "editor.ИсходныйКод.html";
            openHtmlFile(url);
        }

        private void открытьПример1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            createNewDocument();
            RichTextBox richTextBox = GetEditRichTextBox(tabControl1.SelectedTab);

            try
            {
                string url = "editor.test.txt";
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(url))
                using (StreamReader reader = new StreamReader(stream))
                {
                    richTextBox.Text = reader.ReadToEnd();
                }

                HighlightSyntax(richTextBox, EventArgs.Empty);
            }
            catch (Exception ex)
            { }
        }

        private void открытьПример2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            createNewDocument();
            RichTextBox richTextBox = GetEditRichTextBox(tabControl1.SelectedTab);

            try
            {
                string url = "editor.ntest.txt";
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(url))
                using (StreamReader reader = new StreamReader(stream))
                {
                    richTextBox.Text = reader.ReadToEnd();
                }

                HighlightSyntax(richTextBox, EventArgs.Empty);
            }
            catch (Exception ex)
            { }
        }

        private void ИзменениеРазмераТекстаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0) return;

            RichTextBox currentEditBox = GetEditRichTextBox(tabControl1.SelectedTab);
            DataGridView currentdataGridView = GetErrorsGrid(tabControl1.SelectedTab);

            if (currentEditBox != null && currentdataGridView != null)
            {
                float currentEditZoom = currentEditBox.ZoomFactor;
                float currentGridSize = currentdataGridView.Font.Size / 8f;

                using (FontSizeDialog dialog = new FontSizeDialog(currentEditZoom, currentGridSize))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        currentGlobalEditZoom = dialog.EditBoxFontSize;
                        currentGlobalGridFontSize = dialog.DataGridViewFontSize;

                        foreach (TabPage page in tabControl1.TabPages)
                        {
                            ApplyFontSizeToPage(page, currentGlobalEditZoom, currentGlobalGridFontSize);
                        }
                    }
                }
            }
        }

        private void ApplyFontSizeToPage(TabPage page, float editZoom, float gridFontSize)
        {
            RichTextBox box = GetEditRichTextBox(page);
            DataGridView errorsGrid = GetErrorsGrid(page);
            DataGridView lexemesGrid = GetLexemesGrid(page);

            if (box != null)
            {
                box.ZoomFactor = editZoom;
            }

            float newFontSize = gridFontSize * 8;

            if (errorsGrid != null)
            {
                ApplyFontToDataGridView(errorsGrid, newFontSize, gridFontSize);
            }

            if (lexemesGrid != null)
            {
                ApplyFontToDataGridView(lexemesGrid, newFontSize, gridFontSize);
            }
        }

        private void ApplyFontToDataGridView(DataGridView grid, float fontSize, float zoomFactor)
        {
            if (grid == null) return;

            FontStyle existingStyle = grid.Font.Style;

            grid.Font = new Font(grid.Font.FontFamily, fontSize, existingStyle);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font.FontFamily, fontSize, FontStyle.Regular);
            grid.RowsDefaultCellStyle.Font = new Font(grid.Font.FontFamily, fontSize, existingStyle);
            grid.RowTemplate.Height = (int)(25 * zoomFactor);
            grid.ColumnHeadersHeight = (int)(30 * zoomFactor);

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!row.IsNewRow)
                {
                    row.DefaultCellStyle.Font = new Font(grid.Font.FontFamily, fontSize, existingStyle);
                }
            }
        }

        private void РусскийToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LocalizationManager.SetLanguage("ru");
            UpdateUILanguage();
        }

        private void АнглийскийToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LocalizationManager.SetLanguage("en");
            UpdateUILanguage();
        }

        private void UpdateUILanguage()
        {
            файлToolStripMenuItem.Text = LocalizationManager.GetString("file");
            создатьToolStripMenuItem.Text = LocalizationManager.GetString("new");
            открытьToolStripMenuItem.Text = LocalizationManager.GetString("open");
            сохранитьToolStripMenuItem.Text = LocalizationManager.GetString("save");
            сохранитьКакToolStripMenuItem.Text = LocalizationManager.GetString("saveAs");
            выходToolStripMenuItem.Text = LocalizationManager.GetString("exit");
            открытьПример1ToolStripMenuItem.Text = LocalizationManager.GetString("openExample1");
            открытьПример2ToolStripMenuItem.Text = LocalizationManager.GetString("openExample2");

            правкаToolStripMenuItem.Text = LocalizationManager.GetString("edit");
            отменитьToolStripMenuItem.Text = LocalizationManager.GetString("undo");
            вернутьToolStripMenuItem.Text = LocalizationManager.GetString("redo");
            вырезатьToolStripMenuItem.Text = LocalizationManager.GetString("cut");
            копироватьToolStripMenuItem.Text = LocalizationManager.GetString("copy");
            вставитьToolStripMenuItem.Text = LocalizationManager.GetString("paste");
            отменитьВсеИзмененияToolStripMenuItem.Text = LocalizationManager.GetString("undoAll");
            выделитьВсёToolStripMenuItem.Text = LocalizationManager.GetString("selectAll");

            текстToolStripMenuItem.Text = LocalizationManager.GetString("text");
            постановкаЗадачиToolStripMenuItem.Text = LocalizationManager.GetString("task");
            грамматикаToolStripMenuItem.Text = LocalizationManager.GetString("grammar");
            классификацияГрамматикиToolStripMenuItem.Text = LocalizationManager.GetString("grammarClass");
            методАнализаToolStripMenuItem.Text = LocalizationManager.GetString("analysisMethod");
            текстовыйПримерToolStripMenuItem.Text = LocalizationManager.GetString("testExample");
            списокЛитературыToolStripMenuItem.Text = LocalizationManager.GetString("literature");
            исходныйКодПрограммыToolStripMenuItem.Text = LocalizationManager.GetString("sourceCode");

            пускToolStripMenuItem.Text = LocalizationManager.GetString("start");
            настройкиToolStripMenuItem.Text = LocalizationManager.GetString("settings");
            изменениеРазмераТекстаToolStripMenuItem.Text = LocalizationManager.GetString("fontSize");
            языкToolStripMenuItem.Text = LocalizationManager.GetString("language");
            русскийToolStripMenuItem.Text = LocalizationManager.GetString("russian");
            английскийToolStripMenuItem.Text = LocalizationManager.GetString("english");

            справкаToolStripMenuItem.Text = LocalizationManager.GetString("help");
            вызовСправкиToolStripMenuItem.Text = LocalizationManager.GetString("callHelp");
            оПрограммеToolStripMenuItem.Text = LocalizationManager.GetString("about");

            toolTip1.SetToolTip(createButton, LocalizationManager.GetString("tooltipNew"));
            toolTip1.SetToolTip(openButton, LocalizationManager.GetString("tooltipOpen"));
            toolTip1.SetToolTip(saveButton, LocalizationManager.GetString("tooltipSave"));
            toolTip1.SetToolTip(cancelButton, LocalizationManager.GetString("tooltipUndoAll"));
            toolTip1.SetToolTip(backButton, LocalizationManager.GetString("tooltipUndo"));
            toolTip1.SetToolTip(forwardButton, LocalizationManager.GetString("tooltipRedo"));
            toolTip1.SetToolTip(copyButton, LocalizationManager.GetString("tooltipCopy"));
            toolTip1.SetToolTip(cutButton, LocalizationManager.GetString("tooltipCut"));
            toolTip1.SetToolTip(pasteButton, LocalizationManager.GetString("tooltipPaste"));
            toolTip1.SetToolTip(startButton, LocalizationManager.GetString("tooltipStart"));
            toolTip1.SetToolTip(infoButton, LocalizationManager.GetString("tooltipHelp"));
            toolTip1.SetToolTip(button1, LocalizationManager.GetString("tooltipAbout"));

            foreach (TabPage page in tabControl1.TabPages)
            {
                UpdatePageLanguage(page);
            }

            UpdateStatus();
        }

        private void UpdatePageLanguage(TabPage page)
        {
            if (page?.Controls[0] is not SplitContainer split) return;

            if (split.Panel2.Controls[0] is TabControl resultTabs)
            {
                if (resultTabs.TabPages.Count > 0)
                    resultTabs.TabPages[0].Text = LocalizationManager.GetString("errorsTab");
                if (resultTabs.TabPages.Count > 1)
                    resultTabs.TabPages[1].Text = LocalizationManager.GetString("lexemesTab");
            }

            DataGridView errorsGrid = GetErrorsGrid(page);
            if (errorsGrid != null)
            {
                UpdateErrorsGridLanguage(errorsGrid);
            }

            DataGridView lexemesGrid = GetLexemesGrid(page);
            if (lexemesGrid != null)
            {
                UpdateLexemesGridLanguage(lexemesGrid);
            }
        }

        private void UpdateErrorsGridLanguage(DataGridView grid)
        {
            if (grid == null) return;

            if (grid.Columns.Count >= 3)
            {
                grid.Columns[0].HeaderText = LocalizationManager.GetString("errorColumn");
                grid.Columns[1].HeaderText = LocalizationManager.GetString("locationColumn");
                grid.Columns[2].HeaderText = LocalizationManager.GetString("descriptionColumn");
            }

            for (int i = 0; i < grid.Rows.Count - 1; i++)
            {
                var row = grid.Rows[i];

                if (row.Cells[2].Value != null)
                {
                    string originalError = row.Cells[2].Value.ToString();
                    string translatedError = LocalizationManager.TranslateError(originalError);
                    if (translatedError != originalError)
                    {
                        row.Cells[2].Value = translatedError;
                    }
                }

                if (row.Tag is Tuple<int, int> locationData)
                {
                    int line = locationData.Item1;
                    int position = locationData.Item2;
                    if (LocalizationManager.CurrentLanguage == "en")
                        row.Cells[1].Value = $"line {line}, position {position}";
                    else
                        row.Cells[1].Value = $"строка {line}, позиция {position}";
                }
            }

            if (grid.Rows.Count > 0)
            {
                var lastRow = grid.Rows[grid.Rows.Count - 1];
                if (lastRow.Cells[0].Value != null)
                {
                    string totalText = lastRow.Cells[0].Value.ToString();
                    int errorsCount = 0;
                    var match = System.Text.RegularExpressions.Regex.Match(totalText, @"\d+");
                    if (match.Success) errorsCount = int.Parse(match.Value);

                    if (errorsCount == 0)
                        lastRow.Cells[0].Value = LocalizationManager.GetString("totalErrorsZero");
                    else
                        lastRow.Cells[0].Value = LocalizationManager.FormatString("totalErrorsCount", errorsCount);
                }
            }
        }

        private void UpdateLexemesGridLanguage(DataGridView grid)
        {
            if (grid == null) return;

            if (grid.Columns.Count >= 4)
            {
                grid.Columns[0].HeaderText = LocalizationManager.GetString("lexemeCode");
                grid.Columns[1].HeaderText = LocalizationManager.GetString("lexemeType");
                grid.Columns[2].HeaderText = LocalizationManager.GetString("lexemeValue");
                grid.Columns[3].HeaderText = LocalizationManager.GetString("lexemeLocation");
            }

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var row = grid.Rows[i];

                if (row.Cells[2].Value != null)
                {
                    string value = row.Cells[2].Value.ToString();
                    if (value == " " || value == "(пробел)" || value == "(space)")
                    {
                        row.Cells[2].Value = LocalizationManager.GetString("spaceDisplay");
                    }
                }

                if (row.Cells[1].Value != null)
                {
                    string typeName = row.Cells[1].Value.ToString();
                    string typeKey = GetTypeKeyFromDisplayName(typeName);
                    string translatedType = LocalizationManager.GetString($"tokenType_{typeKey}");
                    if (translatedType != $"tokenType_{typeKey}")
                    {
                        row.Cells[1].Value = translatedType;
                    }
                }

                if (row.Tag is Tuple<int, int> locationData)
                {
                    int line = locationData.Item1;
                    int position = locationData.Item2;
                    if (LocalizationManager.CurrentLanguage == "en")
                        row.Cells[3].Value = $"line {line}, position {position}";
                    else
                        row.Cells[3].Value = $"строка {line}, позиция {position}";
                }
            }
        }

        private string GetTypeKeyFromDisplayName(string displayName)
        {
            return displayName switch
            {
                "Ключевое слово" or "Keyword" => "keyword",
                "Идентификатор" or "Identifier" => "id",
                "Целое число" or "Integer" => "integer",
                "Вещественное число" or "Number" => "numeric",
                "Строка" or "String" => "character",
                "Присваивание" or "Assignment" => "assign",
                "Открывающая скобка" or "Left Parenthesis" => "leftparen",
                "Закрывающая скобка" or "Right Parenthesis" => "rightparen",
                "Запятая" or "Comma" => "comma",
                "Минус" or "Minus" => "minus",
                "Конец оператора" or "End of statement" => "end",
                "Пробел" or "Space" => "space",
                "Ошибка" or "Error" => "error",
                _ => displayName
            };
        }

        private void UpdateAllDataGridViewColumns()
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                DataGridView dataGridView = GetErrorsGrid(page);
                if (dataGridView != null && dataGridView.Columns.Count >= 3)
                {
                    dataGridView.Columns[0].HeaderText = LocalizationManager.GetString("errorColumn");
                    dataGridView.Columns[1].HeaderText = LocalizationManager.GetString("locationColumn");
                    dataGridView.Columns[2].HeaderText = LocalizationManager.GetString("descriptionColumn");
                }
            }
        }

        private void UpdateAllDataGridViewContent()
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                DataGridView dataGridView = GetErrorsGrid(page);
                if (dataGridView != null && dataGridView.Rows.Count > 0)
                {
                    if (dataGridView.Columns.Count >= 3)
                    {
                        dataGridView.Columns[0].HeaderText = LocalizationManager.GetString("errorColumn");
                        dataGridView.Columns[1].HeaderText = LocalizationManager.GetString("locationColumn");
                        dataGridView.Columns[2].HeaderText = LocalizationManager.GetString("descriptionColumn");
                    }

                    for (int i = 0; i < dataGridView.Rows.Count - 1; i++)
                    {
                        var row = dataGridView.Rows[i];
                        if (row.Cells[2].Value != null)
                        {
                            string originalError = row.Cells[2].Value.ToString();
                            row.Cells[2].Value = LocalizationManager.TranslateError(originalError);
                        }
                    }

                    if (dataGridView.Rows.Count > 0)
                    {
                        var lastRow = dataGridView.Rows[dataGridView.Rows.Count - 1];
                        if (lastRow.Cells[0].Value != null)
                        {
                            string totalText = lastRow.Cells[0].Value.ToString();
                            if (totalText.Contains("Общее количество ошибок") || totalText.Contains("Total errors"))
                            {
                                int errorsCount = 0;
                                var match = System.Text.RegularExpressions.Regex.Match(totalText, @"\d+");
                                if (match.Success)
                                {
                                    errorsCount = int.Parse(match.Value);
                                }

                                if (errorsCount == 0)
                                {
                                    lastRow.Cells[0].Value = LocalizationManager.FormatString("totalErrors", errorsCount) + " - " + LocalizationManager.GetString("noErrors");
                                }
                                else
                                {
                                    lastRow.Cells[0].Value = LocalizationManager.FormatString("totalErrors", errorsCount);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateStatus()
        {
            if (tabControl1.TabPages.Count == 0 || tabControl1.SelectedTab == null)
            {
                statusLabel.Text = LocalizationManager.GetString("statusNoDocuments");
                cursorPositionLabel.Text = LocalizationManager.FormatString("cursorPosition", 0, 0);
                fileInfoLabel.Text = "";
                languageLabel.Text = LocalizationManager.CurrentLanguage == "ru" ? "Русский" : "English";
                return;
            }

            TabPage currentPage = tabControl1.SelectedTab;
            DocumentInfo info = documentInfo[currentPage];
            RichTextBox editBox = GetEditRichTextBox(currentPage);

            if (info.IsModified)
            {
                statusLabel.Text = LocalizationManager.GetString("statusModified");
                statusLabel.ForeColor = Color.Orange;
            }
            else
            {
                statusLabel.Text = LocalizationManager.GetString("statusReady");
                statusLabel.ForeColor = Color.Green;
            }

            if (!info.IsNewDocument && info.FilePath != null)
            {
                fileInfoLabel.Text = Path.GetFileName(info.FilePath);
            }
            else
            {
                fileInfoLabel.Text = info.OriginalTabName;
            }

            if (editBox != null)
            {
                int line = editBox.GetLineFromCharIndex(editBox.SelectionStart) + 1;
                int col = editBox.SelectionStart - editBox.GetFirstCharIndexFromLine(line - 1) + 1;
                cursorPositionLabel.Text = LocalizationManager.FormatString("cursorPosition", line, col);
            }

            languageLabel.Text = LocalizationManager.CurrentLanguage == "ru" ? "Русский" : "English";
        }

        private void UpdateCursorPosition(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void HighlightSyntax(object sender, EventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;
            if (richTextBox.TextLength > 50000) return;

            int selectionStart = richTextBox.SelectionStart;
            int selectionLength = richTextBox.SelectionLength;

            richTextBox.TextChanged -= HighlightSyntax;
            richTextBox.SuspendLayout();

            richTextBox.SelectAll();
            richTextBox.SelectionColor = Color.Black;

            string text = richTextBox.Text;

            int index = 0;
            while ((index = text.IndexOf("TRUE", index, StringComparison.Ordinal)) != -1)
            {
                bool isWordStart = (index == 0 || !char.IsLetterOrDigit(text[index - 1]));
                bool isWordEnd = (index + 4 == text.Length || !char.IsLetterOrDigit(text[index + 4]));
                if (isWordStart && isWordEnd)
                {
                    richTextBox.Select(index, 4);
                    richTextBox.SelectionColor = Color.DodgerBlue;
                }
                index += 4;
            }

            index = 0;
            while ((index = text.IndexOf("FALSE", index, StringComparison.Ordinal)) != -1)
            {
                bool isWordStart = (index == 0 || !char.IsLetterOrDigit(text[index - 1]));
                bool isWordEnd = (index + 5 == text.Length || !char.IsLetterOrDigit(text[index + 5]));
                if (isWordStart && isWordEnd)
                {
                    richTextBox.Select(index, 5);
                    richTextBox.SelectionColor = Color.DodgerBlue;
                }
                index += 5;
            }

            index = 0;
            while ((index = text.IndexOf("NULL", index, StringComparison.Ordinal)) != -1)
            {
                bool isWordStart = (index == 0 || !char.IsLetterOrDigit(text[index - 1]));
                bool isWordEnd = (index + 4 == text.Length || !char.IsLetterOrDigit(text[index + 4]));
                if (isWordStart && isWordEnd)
                {
                    richTextBox.Select(index, 4);
                    richTextBox.SelectionColor = Color.DarkViolet;
                }
                index += 4;
            }

            index = 0;
            while ((index = text.IndexOf("c", index, StringComparison.Ordinal)) != -1)
            {
                bool isWordStart = (index == 0 || !char.IsLetterOrDigit(text[index - 1]));
                bool isWordEnd = (index + 1 == text.Length || !char.IsLetterOrDigit(text[index + 1]));
                if (isWordStart && isWordEnd)
                {
                    richTextBox.Select(index, 1);
                    richTextBox.SelectionColor = Color.LimeGreen;
                }
                index += 1;
            }

            int arrowIndex = 0;
            while ((arrowIndex = text.IndexOf("<-", arrowIndex)) != -1)
            {
                richTextBox.Select(arrowIndex, 2);
                richTextBox.SelectionColor = Color.DarkBlue;
                arrowIndex += 2;
            }

            System.Text.RegularExpressions.Regex numberRegex = new System.Text.RegularExpressions.Regex(@"\b\d+(\.\d+)?\b");
            foreach (System.Text.RegularExpressions.Match match in numberRegex.Matches(text))
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Crimson;
            }

            int quoteIndex = 0;
            bool inQuote = false;
            int quoteStart = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '"')
                {
                    if (!inQuote)
                    {
                        inQuote = true;
                        quoteStart = i;
                    }
                    else
                    {
                        inQuote = false;
                        richTextBox.Select(quoteStart, i - quoteStart + 1);
                        richTextBox.SelectionColor = Color.Teal;
                    }
                }
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool isValid = char.IsLetterOrDigit(c) ||
                               c == ' ' || c == '<' || c == '-' || c == '(' || c == ')' ||
                               c == ',' || c == ';' || c == '"';

                if (!isValid)
                {
                    richTextBox.Select(i, 1);
                    richTextBox.SelectionColor = Color.Red;
                }
            }

            richTextBox.Select(selectionStart, selectionLength);
            richTextBox.SelectionColor = Color.Black;
            richTextBox.ResumeLayout();
            richTextBox.TextChanged += HighlightSyntax;
        }

        private RichTextBox GetEditRichTextBox(TabPage page)
        {
            if (page?.Controls[0] is not SplitContainer split) return null;
            if (split.Panel1.Controls[0] is not TableLayoutPanel container) return null;
            if (container.Controls.Count < 2) return null;
            return container.Controls[1] as RichTextBox;
        }

        private DataGridView GetErrorsGrid(TabPage page)
        {
            if (page?.Controls[0] is not SplitContainer split) return null;
            if (split.Panel2.Controls[0] is not TabControl resultTabs) return null;
            if (resultTabs.TabPages.Count == 0) return null;
            return resultTabs.TabPages[0].Controls[0] as DataGridView;
        }

        private DataGridView GetLexemesGrid(TabPage page)
        {
            if (page?.Controls[0] is not SplitContainer split) return null;
            if (split.Panel2.Controls[0] is not TabControl resultTabs) return null;
            if (resultTabs.TabPages.Count < 2) return null;
            return resultTabs.TabPages[1].Controls[0] as DataGridView;
        }

        private void ConfigureErrorsGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ReadOnly = true;
            grid.RowHeadersWidth = 70;

            if (grid.Columns.Count == 0)
            {
                grid.Columns.Add("Fragment", "Неверный фрагмент");
                grid.Columns["Fragment"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.Columns.Add("Location", "Местоположение");
                grid.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.Columns.Add("Description", "Описание ошибки");
                grid.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            grid.CellClick += ErrorGridView_CellClick;
        }

        private void ConfigureLexemesGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ReadOnly = true;
            grid.RowHeadersWidth = 70;

            if (grid.Columns.Count == 0)
            {
                grid.Columns.Add("Code", "Код");
                grid.Columns["Code"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.Columns.Add("Type", "Тип лексемы");
                grid.Columns["Type"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.Columns.Add("Lexeme", "Лексема");
                grid.Columns["Lexeme"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.Columns.Add("Location", "Местоположение");
                grid.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            grid.CellClick += LexemeGridView_CellClick;
        }
    }
}
