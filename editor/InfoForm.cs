using System;
using System.Drawing;
using System.Windows.Forms;

namespace editor
{
    public partial class InfoForm : Form
    {
        private static InfoForm _instance = null;
        private RichTextBox richTextBox1;

        public InfoForm(string name)
        {
            InitializeComponent();
            this.Text = name;

            LocalizationManager.LanguageChanged += (s, e) => UpdateContent();
            UpdateContent();

            this.FormClosed += (s, e) => _instance = null;
        }

        public static void ShowInstance(string name)
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new InfoForm(name);
                _instance.Show();
            }
            else
            {
                if (_instance.WindowState == FormWindowState.Minimized)
                    _instance.WindowState = FormWindowState.Normal;

                _instance.Show();
                _instance.BringToFront();
                _instance.Activate();
            }
        }

        public static void UpdateCurrentLanguage()
        {
            if (_instance != null && !_instance.IsDisposed)
            {
                _instance.UpdateContent();
            }
        }

        private void InitializeComponent()
        {
            richTextBox1 = new RichTextBox();
            SuspendLayout();

            richTextBox1.BackColor = SystemColors.Window;
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(0, 0);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(800, 550);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 550);
            Controls.Add(richTextBox1);
            Name = "InfoForm";
            StartPosition = FormStartPosition.CenterParent;
            ResumeLayout(false);
        }

        private void UpdateContent()
        {
            if (richTextBox1 == null || richTextBox1.IsDisposed) return;

            try
            {
                this.Text = LocalizationManager.GetString("about");

                richTextBox1.Clear();
                richTextBox1.WordWrap = true;
                richTextBox1.Font = new Font("Times New Roman", 12f);

                string lang = LocalizationManager.CurrentLanguage;

                if (lang == "ru")
                {
                    ShowRussianContent();
                }
                else
                {
                    ShowEnglishContent();
                }

                richTextBox1.Focus();
                richTextBox1.Select(0, 0);
                richTextBox1.ScrollToCaret();
            }
            catch (ObjectDisposedException) { }
        }

        private void ShowRussianContent()
        {
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.SelectionFont = new Font("Times New Roman", 16f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Информация о программе\r\n");
            richTextBox1.AppendText("\r\n");

            richTextBox1.SelectionAlignment = HorizontalAlignment.Left;

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("1. Сведения о разработке\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 10f);
            richTextBox1.SelectionColor = Color.Gray;
            richTextBox1.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Программу выполнила студентка 3 курса факультета АВТФ Лабузова Виктория, группа АВТ-314.\r\n\r\n");
            richTextBox1.AppendText("Программа написана в рамках первой лабораторной работы ");
            richTextBox1.AppendText("по дисциплине \"Теория формальных языков и компиляторов\".\r\n");
            richTextBox1.AppendText("Программа доработана в рамках курсовой работы.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("2. Техническое задание\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 10f);
            richTextBox1.SelectionColor = Color.Gray;
            richTextBox1.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Разработать приложение – текстовый редактор, дополненный функциями языкового процессора.\r\n");
            richTextBox1.AppendText("Приложение имеет графический интерфейс пользователя.\r\n");
            richTextBox1.AppendText("Язык реализации: C#.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3. Элементы текстового редактора\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 10f);
            richTextBox1.SelectionColor = Color.Gray;
            richTextBox1.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.1. Основное меню программы\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Пункт меню \"Текст\". При вызове команд этого меню открываются HTML-страницы");
            richTextBox1.AppendText(" с соответствующей информацией по курсовой работе \"Объявление векторов в языке R\".\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.2. Панель инструментов\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Панель инструментов содержит кнопки вызова часто используемых пунктов меню:\r\n\r\n");

            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 20;
            richTextBox1.AppendText("Файл – Создать, Открыть, Сохранить, Сохранить как, Выход\r\n");
            richTextBox1.AppendText("Правка – Отменить, Вернуть, Вырезать, Копировать, Вставить, Отменить все изменения, Выделить всё\r\n");
            richTextBox1.AppendText("Настройки – Изменение размера текста, Язык (Русский/English)\r\n");
            richTextBox1.AppendText("Пуск – Запуск анализатора текста\r\n");
            richTextBox1.AppendText("Справка - Вызов справки, О программе\r\n");
            richTextBox1.SelectionBullet = false;
            richTextBox1.AppendText("\r");
            richTextBox1.SelectionIndent = 0;

            richTextBox1.AppendText("Приложение имеет справочную систему, запускаемую командой \"Вызов справки\".\r\n");
            richTextBox1.AppendText("Справка содержит описание всех реализованных функций меню.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.3. Окно/область ввода/редактирования текста\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Область редактирования представляет текстовый редактор.\r\n");
            richTextBox1.AppendText("Команды меню \"Файл\", \"Правка\" и \"Вид\" работают с содержимым этой области.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.4. Окно/область отображения результатов\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("В область отображения результатов выводятся сообщения и результаты работы языкового процессора.\r\n");
            richTextBox1.AppendText("В этой области ввод текста запрещен.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.5. Интерфейс с вкладками\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Интерфейс имеет вкладки и позволяет одновременно работать с несколькими текстами.\r\n");
        }

        private void ShowEnglishContent()
        {
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.SelectionFont = new Font("Times New Roman", 16f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Program Information\r\n");
            richTextBox1.AppendText("\r\n");

            richTextBox1.SelectionAlignment = HorizontalAlignment.Left;

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("1. Development Information\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 10f);
            richTextBox1.SelectionColor = Color.Gray;
            richTextBox1.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Program developed by Victoria Labuzova, 3rd year student of AVTF faculty, group AVT-314.\r\n\r\n");
            richTextBox1.AppendText("The program was created as part of the first laboratory work ");
            richTextBox1.AppendText("in the discipline \"Theory of Formal Languages and Compilers\".\r\n");
            richTextBox1.AppendText("The program was enhanced as part of a course project.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("2. Technical Specification\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 10f);
            richTextBox1.SelectionColor = Color.Gray;
            richTextBox1.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Develop a text editor application with language processor functions.\r\n");
            richTextBox1.AppendText("The application has a graphical user interface.\r\n");
            richTextBox1.AppendText("Implementation language: C#.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3. Text Editor Elements\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 10f);
            richTextBox1.SelectionColor = Color.Gray;
            richTextBox1.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.1. Main Menu\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("The \"Text\" menu item opens HTML pages with information about the course project ");
            richTextBox1.AppendText("\"Vector Declaration in R Language\".\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.2. Toolbar\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("The toolbar contains buttons for frequently used menu items:\r\n\r\n");

            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 20;
            richTextBox1.AppendText("File – New, Open, Save, Save As, Exit\r\n");
            richTextBox1.AppendText("Edit – Undo, Redo, Cut, Copy, Paste, Undo All, Select All\r\n");
            richTextBox1.AppendText("Settings – Font Size, Language (Russian/English)\r\n");
            richTextBox1.AppendText("Run – Start text analyzer\r\n");
            richTextBox1.AppendText("Help – Help, About\r\n");
            richTextBox1.SelectionBullet = false;
            richTextBox1.AppendText("\r");
            richTextBox1.SelectionIndent = 0;

            richTextBox1.AppendText("The application has a help system accessible via the \"Help\" command.\r\n");
            richTextBox1.AppendText("The help contains descriptions of all implemented menu functions.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.3. Text Input/Editing Area\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("The editing area is a text editor.\r\n");
            richTextBox1.AppendText("Menu commands \"File\", \"Edit\" work with the content of this area.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.4. Results Display Area\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Messages and results of the language processor are displayed in the results area.\r\n");
            richTextBox1.AppendText("Text input is prohibited in this area.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.AppendText("3.5. Tab Interface\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("The interface has tabs and allows working with multiple texts simultaneously.\r\n");
        }
    }
}