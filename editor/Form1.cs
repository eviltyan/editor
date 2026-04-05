using System.DirectoryServices;
using System.Text;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace editor
{
    public partial class Form1 : Form
    {
        private int newDocumentCounter = 2;
        private Dictionary<TabPage, DocumentInfo> documentInfo = new Dictionary<TabPage, DocumentInfo>();

        private Font tabFont;
        private Dictionary<TabPage, Rectangle> closeButtons = new Dictionary<TabPage, Rectangle>();

        private List<SearchResult> currentResults;

        public Form1()
        {
            InitializeComponent();

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

            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ReadOnly = true;
            dataGridView.BackColor = Color.White;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView.AllowUserToAddRows = false;

            dataGridView.Columns.Add("Substring", "Найденная подстрока");
            dataGridView.Columns.Add("Line", "Строка");
            dataGridView.Columns.Add("Position", "Позиция в строке");
            dataGridView.Columns.Add("Length", "Длина");
            dataGridView.Columns["Substring"].Width = 200;
            dataGridView.Columns["Line"].Width = 80;
            dataGridView.Columns["Position"].Width = 100;
            dataGridView.Columns["Length"].Width = 60;

            dataGridView.SelectionChanged += DataGridView_SelectionChanged;

            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

            label.Text = "";
            labelSt.Text = "Готов к работе";

            LoadSampleText();
            LoadSearchPatterns();
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

            SplitContainer splitContainer2 = new SplitContainer();
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Orientation = Orientation.Horizontal;
            splitContainer2.SplitterDistance = splitContainer2.Height / 5 * 4;

            DataGridView dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ReadOnly = true;
            dataGridView.BackColor = Color.White;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView.AllowUserToAddRows = false;

            dataGridView.Columns.Add("Substring", "Найденная подстрока");
            dataGridView.Columns.Add("Line", "Строка");
            dataGridView.Columns.Add("Position", "Позиция в строке");
            dataGridView.Columns.Add("Length", "Длина");
            dataGridView.Columns["Substring"].Width = 200;
            dataGridView.Columns["Line"].Width = 80;
            dataGridView.Columns["Position"].Width = 100;
            dataGridView.Columns["Length"].Width = 60;

            dataGridView.SelectionChanged += DataGridView_SelectionChanged;

            SplitContainer splitContainer3 = new SplitContainer();
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Orientation = Orientation.Vertical;
            splitContainer3.SplitterDistance = 50;
            splitContainer3.Height = 50;

            Label label = new Label();
            label.Text = "";
            label.Dock = DockStyle.Fill;
            Label labelSt = new Label();
            labelSt.Text = "Готов к работе";
            labelSt.Dock = DockStyle.Fill;

            splitContainer.Panel1.Controls.Add(richTextBoxEdit);
            splitContainer.Panel2.Controls.Add(splitContainer2);
            
            splitContainer2.Panel1.Controls.Add(dataGridView);
            splitContainer2.Panel2.Controls.Add(splitContainer3);

            splitContainer3.Panel1.Controls.Add(label);
            splitContainer3.Panel2.Controls.Add(labelSt);

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
                    SplitContainer splitD = splitReadOnly.Panel2.Controls[0] as SplitContainer;
                    DataGridView dataGridView = splitD.Panel1.Controls[0] as DataGridView;
                    SplitContainer splitlabel = splitD.Panel2.Controls[0] as SplitContainer;
                    Label label = splitlabel.Panel1.Controls[0] as Label;
                    Label labelSt = splitlabel.Panel2.Controls[0] as Label;

                    dataGridView.Rows.Clear();

                    dataGridView.SelectionChanged += DataGridView_SelectionChanged;

                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                    label.Text = "";
                    labelSt.Text = "Готов к работе";
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
            Start();
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
            ClearHighlights();
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

        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Start();
        }
     
        private void LoadSearchPatterns()
        {
            Dictionary<string, SearchPatternInfo> patterns = new Dictionary<string, SearchPatternInfo>
            {
                {
                    "SSN (США)",
                    new SearchPatternInfo
                    {
                        Pattern = @"\d{3}-\d{2}-\d{4}",
                        Description = "Номера социального страхования США в формате XXX-XX-XXXX"
                    }
                },
                {
                    "MasterCard",
                    new SearchPatternInfo
                    {
                        //Pattern = @"(5[1-5]\d{2}|222[1-9]|22[3-9]\d|2[3-6]\d{2}|27[01]\d|2720)\d{12}",
                        Pattern = @"(5[1-5]\d{2}|222[1-9]|22[3-9]\d|2[3-6]\d{2}|27[01]\d|2720)\d{12}",
                        Description = "Номера карт MasterCard (16 цифр)"
                    }
                },
                {
                    "Даты (MM/DD/YYYY)",
                    new SearchPatternInfo
                    {
                        Pattern = @"(?:(?:0[13578]|1[02])/(?:0[1-9]|[12]\d|3[01])|(?:0[469]|11)/(?:0[1-9]|[12]\d|30)|02/(?:0[1-9]|1\d|2[0-8]))/\d{4}|02/29/(?:(?:\d{2}(?:0[48]|[2468][048]|[13579][26]))|(?:[02468][048]00|[13579][26]00))",
                        Description = "Даты в формате MM/DD/YYYY"
                    }
                },
                {
                    "Автомат",
                    new SearchPatternInfo
                    {
                        Pattern = "",
                        Description = "Номера карт MasterCard (16 цифр)"
                    }
                }
            };

            comboBox.DataSource = new BindingSource(patterns, null);
            comboBox.DisplayMember = "Key";
            comboBox.ValueMember = "Value";
        }

        private void LoadSampleText()
        {
            string filePath = "../../../../test.txt";
            string sampleText;
            using (StreamReader reader = new StreamReader(filePath, true))
            {
                sampleText = reader.ReadToEnd();
            }
            richTextBoxEdit.Text = sampleText;
        }

        private void Start()
        {
            TabPage currentPage = tabControl1.SelectedTab;
            RichTextBox richTextBoxEdit = GetEditRichTextBox(currentPage);
            SplitContainer splitReadOnly = currentPage.Controls[0] as SplitContainer;
            SplitContainer split = splitReadOnly.Panel2.Controls[0] as SplitContainer;
            DataGridView dataGridView = split.Panel1.Controls[0] as DataGridView;
            SplitContainer splitlabel = split.Panel2.Controls[0] as SplitContainer;
            Label label = splitlabel.Panel1.Controls[0] as Label;
            Label labelSt = splitlabel.Panel2.Controls[0] as Label;

            dataGridView.Rows.Clear();

            string text = richTextBoxEdit.Text;

            ClearHighlights();

            if (string.IsNullOrWhiteSpace(text))
            {
                labelSt.Text = "Ошибка: Нет данных для поиска";
                MessageBox.Show("Нет данных для поиска. Введите текст в редактор.",
                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedItem = comboBox.SelectedItem as KeyValuePair<string, SearchPatternInfo>?;
            if (!selectedItem.HasValue)
            {
                labelSt.Text = "Ошибка: Выберите тип поиска";
                return;
            }

            string patternName = selectedItem.Value.Key;
            SearchPatternInfo patternInfo = selectedItem.Value.Value;

            try
            {
                PerformSearch(text, patternInfo.Pattern, patternName);
                labelSt.Text = $"Поиск завершен. Тип: {patternName}";
            }
            catch (Exception ex)
            {
                labelSt.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка при выполнении поиска: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformSearch(string text, string pattern, string patternName)
        {
            TabPage currentPage = tabControl1.SelectedTab;
            SplitContainer splitReadOnly = currentPage.Controls[0] as SplitContainer;
            SplitContainer split = splitReadOnly.Panel2.Controls[0] as SplitContainer;
            DataGridView dataGridView = split.Panel1.Controls[0] as DataGridView;
            SplitContainer splitlabel = split.Panel2.Controls[0] as SplitContainer;
            Label label = splitlabel.Panel1.Controls[0] as Label;
            Label labelSt = splitlabel.Panel2.Controls[0] as Label;

            currentResults = new List<SearchResult>();
            int totalMatches = 0;
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (patternName == "Автомат")
            {
                totalMatches = SearchMasterCardAutomaton(lines, text);
            }
            else
            {
                RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline;
                Regex regex = new Regex(pattern, options);

                for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    string line = lines[lineNumber];
                    MatchCollection matches = regex.Matches(line);

                    foreach (Match match in matches)
                    {
                        totalMatches++;
                        int globalPosition = GetGlobalPosition(text, lineNumber, match.Index);

                        var result = new SearchResult
                        {
                            Substring = match.Value,
                            LineNumber = lineNumber + 1,
                            PositionInLine = match.Index + 1,
                            GlobalPosition = globalPosition,
                            Length = match.Length,
                            LineText = line
                        };

                        currentResults.Add(result);

                        dataGridView.Rows.Add(
                            result.Substring,
                            result.LineNumber,
                            result.PositionInLine,
                            result.Length
                        );
                    }
                }
            }

            label.Text = $"Найдено: {totalMatches}";

            if (totalMatches == 0)
            {
                labelSt.Text = $"По типу '{patternName}' совпадений не найдено";
                MessageBox.Show("Совпадений не найдено.", "Результат поиска",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                labelSt.Text = $"Найдено {totalMatches} совпадений по типу '{patternName}'";
            }
        }

        private int GetGlobalPosition(string text, int lineNumber, int positionInLine)
        {
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int globalPos = 0;

            for (int i = 0; i < lineNumber; i++)
            {
                globalPos += lines[i].Length;
                if (i < lines.Length - 1)
                {
                    if (text.Contains("\r\n"))
                        globalPos += 2;
                    else
                        globalPos += 1;
                }
            }

            return globalPos + positionInLine;
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            TabPage currentPage = tabControl1.SelectedTab;
            SplitContainer splitReadOnly = currentPage.Controls[0] as SplitContainer;
            SplitContainer split = splitReadOnly.Panel2.Controls[0] as SplitContainer;
            DataGridView dataGridView = split.Panel1.Controls[0] as DataGridView;
            SplitContainer splitlabel = split.Panel2.Controls[0] as SplitContainer;
            Label label = splitlabel.Panel1.Controls[0] as Label;
            Label labelSt = splitlabel.Panel2.Controls[0] as Label;
            if (dataGridView.SelectedRows.Count == 0 || currentResults == null)
                return;

            int selectedIndex = dataGridView.SelectedRows[0].Index;
            if (selectedIndex >= currentResults.Count)
                return;

            SearchResult result = currentResults[selectedIndex];

            RichTextBox richTextBoxEdit = GetEditRichTextBox(tabControl1.SelectedTab);

            ClearHighlights();

            richTextBoxEdit.Select(result.GlobalPosition, result.Length);
            richTextBoxEdit.SelectionBackColor = Color.Yellow;
            richTextBoxEdit.SelectionColor = Color.Black;
            richTextBoxEdit.ScrollToCaret();

            labelSt.Text = $"Выделен фрагмент: \"{result.Substring}\" [строка {result.LineNumber}, позиция {result.PositionInLine}]";
        }

        private void ClearHighlights()
        {
            RichTextBox richTextBoxEdit = GetEditRichTextBox(tabControl1.SelectedTab);

            richTextBoxEdit.SelectAll();
            richTextBoxEdit.SelectionBackColor = Color.White;
            richTextBoxEdit.DeselectAll();
        }

        private int SearchMasterCardAutomaton(string[] lines, string fullText)
        {
            MasterCardAutomaton automaton = new MasterCardAutomaton();
            int totalMatches = 0;
            int globalPos = 0;

            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                string line = lines[lineNum];

                for (int i = 0; i < line.Length; i++)
                {
                    if (automaton.ProcessChar(line[i], globalPos + i, out string match, out int matchStart, out int matchLength))
                    {
                        totalMatches++;

                        var result = new SearchResult
                        {
                            Substring = match,
                            LineNumber = lineNum + 1,
                            PositionInLine = i - matchLength + 2,
                            GlobalPosition = matchStart,
                            Length = matchLength,
                            LineText = line
                        };

                        currentResults.Add(result);
                        dataGridView.Rows.Add(result.Substring, result.LineNumber, result.PositionInLine, result.Length);
                    }
                }

                globalPos += line.Length;
                if (lineNum < lines.Length - 1)
                {
                    if (fullText.Contains("\r\n"))
                        globalPos += 2;
                    else
                        globalPos += 1;
                }
            }

            automaton.ProcessChar('\0', globalPos, out string finalMatch, out int finalStart, out int finalLength);
            if (finalMatch != null)
            {
                totalMatches++;
            }

            return totalMatches;
        }
    }
}