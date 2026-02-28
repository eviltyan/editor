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
                string url = "https://github.com/eviltyan/editor";
                richTextBox1.Text = "https://github.com/eviltyan/editor";
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
                richTextBox1.Text = "Автор:Лабузова Виктория, АВТ-314\r\nОписание проекта: текстовый редактор с возможностью редактирования текстовых документов.";
            }
        }
    }
}
