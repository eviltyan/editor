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
        public InfoForm(string name)
        {
            InitializeComponent();
            this.Text = name;
            if (name == "Справка")
            {
                string url = "https://disk.yandex.ru/i/etYdKgavzt3-jg";
                richTextBox1.Text = "https://disk.yandex.ru/i/etYdKgavzt3-jg";
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception e)
                {

                }
            }
            else
            {
                richTextBox1.Text = "Сведения об авторе\r\nАвтор: студентка группы АВТ-314, Лабузова Виктория Витальевна\r\n  \r\nОписание проекта \r\nТекстовый редактор с возможностью редактирования текстовых документов.\r\nВ программе присутствуют функции создания, открытия, сохранения, закрытия файла, есть возможность работы с несколькими файлами одновременно.\r\nФункции правки включают: отмена, возрврат, вырезание, копирование, вставка, удаление, выделение всего текста.\r\nТакже есть возможность узнать о программе и ознакомиться с руководством пользователя. ";
            }
        }
    }
}
