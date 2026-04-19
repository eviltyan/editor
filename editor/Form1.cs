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

        private List<VectorDeclNode> lastAstNodes = new List<VectorDeclNode>();
        private bool isJson = false;

        public Form1()
        {
            InitializeComponent();

            analyzer = new LexicalAnalyzer();
            syntax = new SyntaxAutomaton();

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += TabControl1_DrawItem;
            tabControl1.MouseDown += TabControl1_MouseDown;

            tabFont = this.Font;

            var controls = new PageControls
            {
                EditBox = richTextBoxEdit,
                AstBox = astTextBox,
                ErrorGrid = dataGridView
            };
            tabPage1.Tag = controls;

            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView.Columns.Add("Fragment", "Неверный фрагмент");
            dataGridView.Columns["Fragment"].Width = 150;
            dataGridView.Columns["Fragment"].MinimumWidth = 100; dataGridView.Columns.Add("Location", "Местоположение");
            dataGridView.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns.Add("Description", "Описание ошибки");
            dataGridView.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.CellClick += ErrorGridView_CellClick;

            DocumentInfo info = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                IsSaved = false,
                OriginalTabName = "Документ 1"
            };

            richTextBoxEdit.TextChanged += RichTextBox_TextChanged;
            info.History.AddState(new TextState(richTextBoxEdit.Text, richTextBoxEdit.SelectionStart, richTextBoxEdit.SelectionLength));
            documentInfo[tabPage1] = info;

            this.StartPosition = FormStartPosition.CenterScreen;
            UpdateUndoRedoButtons();
        }

        private void createNewDocument()
        {
            TabPage newPage = new TabPage();
            string tabName = $"Документ {newDocumentCounter++}";
            newPage.Text = tabName;

            SplitContainer mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Orientation = Orientation.Horizontal;
            mainSplit.SplitterDistance = mainSplit.Height * 70 / 100;

            SplitContainer editorSplit = new SplitContainer();
            editorSplit.Dock = DockStyle.Fill;
            editorSplit.Orientation = Orientation.Vertical;
            editorSplit.SplitterDistance = editorSplit.Width / 2;

            RichTextBox editBox = new RichTextBox();
            editBox.Dock = DockStyle.Fill;
            editBox.AcceptsTab = true;
            editBox.Font = new Font("Consolas", 11);

            TextBox astBox = new TextBox();
            astBox.Dock = DockStyle.Fill;
            astBox.Multiline = true;
            astBox.ScrollBars = ScrollBars.Both;
            astBox.Font = new Font("Consolas", 10);
            astBox.WordWrap = false;
            astBox.ReadOnly = true;
            astBox.BackColor = Color.FromArgb(250, 250, 250);

            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;
            Label leftLabel = new Label();
            leftLabel.Text = "Редактор кода";
            leftLabel.Dock = DockStyle.Top;
            leftLabel.BackColor = Color.FromArgb(240, 240, 240);
            leftLabel.Padding = new Padding(5);
            leftLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            leftPanel.Controls.Add(editBox);
            leftPanel.Controls.Add(leftLabel);

            Panel rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            Label rightLabel = new Label();
            rightLabel.Text = "AST (Абстрактное синтаксическое дерево)";
            rightLabel.Dock = DockStyle.Top;
            rightLabel.BackColor = Color.FromArgb(240, 240, 240);
            rightLabel.Padding = new Padding(5);
            rightLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            rightPanel.Controls.Add(astBox);
            rightPanel.Controls.Add(rightLabel);

            editorSplit.Panel1.Controls.Add(leftPanel);
            editorSplit.Panel2.Controls.Add(rightPanel);

            Panel errorPanel = new Panel();
            errorPanel.Dock = DockStyle.Fill;

            Label errorLabel = new Label();
            errorLabel.Text = "Ошибки";
            errorLabel.Dock = DockStyle.Top;
            errorLabel.BackColor = Color.FromArgb(240, 240, 240);
            errorLabel.Padding = new Padding(5);
            errorLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            DataGridView dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView.Columns.Add("Fragment", "Неверный фрагмент");
            dataGridView.Columns["Fragment"].Width = 150;
            dataGridView.Columns["Fragment"].MinimumWidth = 100;
            dataGridView.Columns.Add("Location", "Местоположение");
            dataGridView.Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns.Add("Description", "Описание ошибки");
            dataGridView.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.CellClick += ErrorGridView_CellClick;

            errorPanel.Controls.Add(dataGridView);
            errorPanel.Controls.Add(errorLabel);

            mainSplit.Panel1.Controls.Add(editorSplit);
            mainSplit.Panel2.Controls.Add(errorPanel);
            newPage.Controls.Add(mainSplit);

            newPage.Tag = new PageControls
            {
                EditBox = editBox,
                AstBox = astBox,
                ErrorGrid = dataGridView
            };

            DocumentInfo info = new DocumentInfo
            {
                FilePath = null,
                IsModified = false,
                IsSaved = false,
                OriginalTabName = tabName
            };

            editBox.TextChanged += RichTextBox_TextChanged;
            info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
            documentInfo[newPage] = info;

            tabControl1.TabPages.Add(newPage);
            tabControl1.SelectedTab = newPage;

            UpdateUndoRedoButtons();
        }

        private RichTextBox GetEditRichTextBox(TabPage page)
        {
            var controls = page.Tag as PageControls;
            return controls?.EditBox;
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
                    DataGridView dataGridView = GetErrorGridView(currentPage);
                    TextBox astBox = GetAstTextBox(currentPage);

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

                        DocumentInfo info = documentInfo[currentPage];
                        info.FilePath = filePath;
                        info.IsModified = false;
                        info.IsSaved = true;
                        currentPage.Text = Path.GetFileName(filePath);

                        info.History.AddState(new TextState(editBox.Text, editBox.SelectionStart, editBox.SelectionLength));
                        documentInfo[currentPage] = info;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии: {ex.Message}");
                        tabControl1.TabPages.Remove(currentPage);
                        documentInfo.Remove(currentPage);
                    }

                    dataGridView.Rows.Clear();
                    astBox.Clear();
                    dataGridView.CellClick += ErrorGridView_CellClick;
                    tabControl1.SelectedTab = currentPage;
                }
                else
                {
                    tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                    documentInfo.Remove(tabControl1.SelectedTab);
                }
            }
        }

        private TextBox GetAstTextBox(TabPage page)
        {
            var controls = page.Tag as PageControls;
            return controls?.AstBox;
        }

        private DataGridView GetErrorGridView(TabPage page)
        {
            var controls = page.Tag as PageControls;
            return controls?.ErrorGrid;
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
            DataGridView dataGridView = GetErrorGridView(currentPage);
            TextBox astTextBox = GetAstTextBox(currentPage);

            if (editBox == null || dataGridView == null || astTextBox == null) return;

            try
            {
                Application.DoEvents();

                string input = editBox.Text;
                dataGridView.Rows.Clear();
                astTextBox.Clear();

                List<SyntaxError> allErrors = new List<SyntaxError>();
                var tokens = analyzer.Analyze(input);

                foreach (var token in tokens)
                {
                    if (token.IsError)
                    {
                        allErrors.Add(new SyntaxError
                        {
                            InvalidFragment = token.Value,
                            Line = token.Line,
                            Position = token.StartPos,
                            Description = token.ErrorMessage
                        });
                    }
                }

                dynamic syntaxErrors = syntax.Parse(tokens);
                allErrors.AddRange(syntaxErrors);

                List<VectorDeclNode> astNodes = new List<VectorDeclNode>();
                List<SemanticError> semanticErrors = new List<SemanticError>();

                SemanticAnalyzer semantic = new SemanticAnalyzer();
                var result = semantic.Analyze(tokens);
                astNodes = result.astNodes;
                semanticErrors = result.errors;

                List<SyntaxError> sortedErrors = allErrors.OrderBy(e => e.Line)
                                           .ThenBy(e => e.Position)
                                           .ToList();

                List<SemanticError> sortedSemanticErrors = semanticErrors.OrderBy(e => e.Line)
                                           .ThenBy(e => e.Position)
                                           .ToList();

                foreach (var error in sortedErrors)
                {
                    int rowIndex = dataGridView.Rows.Add(
                        error.InvalidFragment,
                        error.Location,
                        error.Description
                    );
                    dataGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                }

                foreach (var error in sortedSemanticErrors)
                {
                    int rowIndex = dataGridView.Rows.Add(
                        error.Fragment,
                        error.Location,
                        error.Message
                    );
                    dataGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                }

                int totalErrors = allErrors.Count + semanticErrors.Count;

                DataGridViewRow countRow = new DataGridViewRow();
                countRow.DefaultCellStyle.BackColor = totalErrors == 0 ? Color.FromArgb(220, 255, 220) : Color.FromArgb(255, 220, 220);
                countRow.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                countRow.DefaultCellStyle.ForeColor = totalErrors == 0 ? Color.Green : Color.Red;

                DataGridViewCell countCell = new DataGridViewTextBoxCell();
                countCell.Value = totalErrors == 0
                    ? $"Общее количество ошибок: 0 - Ошибок не обнаружено!"
                    : $"Общее количество ошибок: {totalErrors}";
                countRow.Cells.Add(countCell);
                countRow.Cells.Add(new DataGridViewTextBoxCell());
                countRow.Cells.Add(new DataGridViewTextBoxCell());
                dataGridView.Rows.Add(countRow);

                lastAstNodes = astNodes;

                if (astNodes.Count > 0)
                {
                    StringBuilder allAst = new StringBuilder();

                    for (int i = 0; i < astNodes.Count; i++)
                    {
                        if (i > 0)
                        {
                            allAst.AppendLine();
                            allAst.AppendLine();
                        }

                        allAst.Append(AstPrinter.PrintToTree(astNodes[i]));
                    }

                    astTextBox.Text = allAst.ToString();
                }
                else if (totalErrors > 0)
                {
                    astTextBox.Text = "AST не построено из-за ошибок.";
                }
                isJson = false;

                if (totalErrors == 0)
                {
                    MessageBox.Show("Анализ завершен. Ошибок не обнаружено!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Анализ завершен. Найдено ошибок: {totalErrors}", "Обнаружены ошибки",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ErrorGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView.Rows.Count - 1)
            {
                var row = dataGridView.Rows[e.RowIndex];
                string location = row.Cells["Location"].Value?.ToString();

                if (!string.IsNullOrEmpty(location))
                {
                    var parts = location.Replace("строка ", "").Split(',');
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int line))
                        {
                            var posPart = parts[1].Replace("позиция", "").Trim();
                            if (int.TryParse(posPart, out int position))
                            {
                                NavigateToPosition(line, position);
                            }
                        }
                    }
                }
            }
        }

        private void NavigateToPosition(int line, int position)
        {
            RichTextBox richTextBoxEd = GetEditRichTextBox(tabControl1.SelectedTab);
            string[] lines = richTextBoxEd.Lines;
            if (line <= lines.Length)
            {
                int charIndex = 0;
                for (int i = 0; i < line - 1; i++)
                {
                    charIndex += lines[i].Length;
                }
                charIndex += position + (1 * line - 2);

                if (charIndex >= 0 && charIndex <= richTextBoxEd.TextLength)
                {
                    richTextBoxEd.Focus();
                    richTextBoxEd.Select(charIndex, 1);
                    richTextBoxEd.ScrollToCaret();
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
            catch (Exception ex)
            {

            }
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
            }
            catch (Exception ex)
            { }
        }

        private void paintButton_Click(object sender, EventArgs e)
        {
            if (lastAstNodes == null || lastAstNodes.Count == 0)
            {
                MessageBox.Show("Нет построенного AST. Сначала выполните анализ (Пуск).",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AstVisualizerForm visualizer = new AstVisualizerForm(lastAstNodes);
            visualizer.Show();

            if (visualizer != null && !visualizer.IsDisposed)
            {
                if (visualizer.WindowState == FormWindowState.Minimized)
                    visualizer.WindowState = FormWindowState.Normal;

                visualizer.BringToFront();
                visualizer.Activate();

                visualizer.UpdateAst(lastAstNodes);
            }
            else
            {
                visualizer = new AstVisualizerForm(lastAstNodes);
                visualizer.FormClosed += (s, args) => visualizer = null;
                visualizer.Show(this);
            }
        }

        private void jsonbutton_Click(object sender, EventArgs e)
        {
            TextBox astTextBox = GetAstTextBox(tabControl1.SelectedTab);
            if (!isJson)
            {
                if (lastAstNodes == null || lastAstNodes.Count == 0)
                {
                    MessageBox.Show("Нет построенного AST. Сначала выполните анализ (Пуск).",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                StringBuilder jsonBuilder = new StringBuilder();
                jsonBuilder.AppendLine("[");

                for (int i = 0; i < lastAstNodes.Count; i++)
                {
                    jsonBuilder.Append(AstPrinter.PrintToJson(lastAstNodes[i]));
                    if (i < lastAstNodes.Count - 1)
                        jsonBuilder.AppendLine(",");
                }

                jsonBuilder.AppendLine("]");

                astTextBox.Text = jsonBuilder.ToString();
                isJson = true;
            }
            else
            {
                if (lastAstNodes.Count > 0)
                {
                    StringBuilder allAst = new StringBuilder();

                    for (int i = 0; i < lastAstNodes.Count; i++)
                    {
                        if (i > 0)
                        {
                            allAst.AppendLine();
                            allAst.AppendLine();
                        }

                        allAst.Append(AstPrinter.PrintToTree(lastAstNodes[i]));
                    }

                    astTextBox.Text = allAst.ToString();
                }
                isJson = false;
            }
        }
    }
}
