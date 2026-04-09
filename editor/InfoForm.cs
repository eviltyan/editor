using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace editor
{
    public partial class InfoForm : Form
    {
        private static InfoForm _instance = null;

        public InfoForm(string name)
        {
            InitializeComponent();
            this.Text = name;
            ShowCourseworkInfo();
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

        private void ShowCourseworkInfo()
        {
            richTextBox1.Clear();
            richTextBox1.WordWrap = true;

            richTextBox1.Font = new Font("Times New Roman", 12f);

            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.SelectionFont = new Font("Times New Roman", 16f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black ;
            richTextBox1.AppendText("Информация о программе\r\n");

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
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("3.1. Основное меню программы\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Пункт меню \"Текст\". При вызове команд этого меню открываются HTML-страницы");
            richTextBox1.AppendText("с соответствующей информацией по курсовой работе \"Объявление векторов в языке R\".\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("3.2. Панель инструментов\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Панель инструментов содержит кнопки вызова часто используемых пунктов меню:\r\n\r\n");

            richTextBox1.SelectionBullet = true;
            richTextBox1.SelectionIndent = 20;
            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Файл – Создать, Открыть, Сохранить, Сохранить как, Выход\r\n");
            richTextBox1.AppendText("Правка – Отменить, Вернуть, Вырезать, Копировать, Вставить, Отменить все изменения, Выделить всё\r\n");
            richTextBox1.AppendText("Пуск – Запуск анализатора текста\r\n");
            richTextBox1.AppendText("Справка - Вызов справки, О программе\r\n");
            richTextBox1.SelectionBullet = false;
            richTextBox1.AppendText("\r");
            richTextBox1.SelectionIndent = 0;

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.AppendText("Приложение имеет справочную систему, запускаемую командой \"Вызов справки\".\r\n");
            richTextBox1.AppendText("Справка содержит описание всех реализованных функций меню.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("3.3. Окно/область ввода/редактирования текста\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Область редактирования представляет текстовый редактор.\r\n");
            richTextBox1.AppendText("Команды меню \"Файл\", \"Правка\" и \"Вид\" работают с содержимым этой области.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("3.4. Окно/область отображения результатов\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("В область отображения результатов выводятся сообщения и результаты работы языкового процессора.\r\n");
            richTextBox1.AppendText("В этой области ввод текста запрещен.\r\n\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f, FontStyle.Bold);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("3.5. Интерфейс с вкладками\r\n");

            richTextBox1.SelectionFont = new Font("Times New Roman", 12f);
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText("Интерфейс имеет вкладки и позволяет одновременно работать с несколькими текстами.\r\n");

            richTextBox1.Focus();
            richTextBox1.Select(0, 0);
            richTextBox1.ScrollToCaret();
        }
    }
}
