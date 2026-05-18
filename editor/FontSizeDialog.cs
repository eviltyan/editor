using System;
using System.Windows.Forms;

namespace editor
{
    public partial class FontSizeDialog : Form
    {
        private TrackBar trackBarEdit;
        private TrackBar trackBarGrid;
        private Label labelEditValue;
        private Label labelGridValue;
        private Button btnOK;
        private Button btnCancel;

        private Label lblEdit;
        private Label lblGrid;

        private float editBoxSize;
        private float dataGridViewSize;

        public float EditBoxFontSize { get; private set; }
        public float DataGridViewFontSize { get; private set; }

        public FontSizeDialog(float currentEditSize, float currentGridSize)
        {
            InitializeComponent();

            editBoxSize = currentEditSize;
            dataGridViewSize = currentGridSize;

            trackBarEdit.Value = (int)(currentEditSize * 10);
            trackBarGrid.Value = (int)(currentGridSize * 10);
            labelEditValue.Text = $"{currentEditSize:F1}x";
            labelGridValue.Text = $"{currentGridSize:F1}x";

            LocalizationManager.LanguageChanged += (s, e) => UpdateLanguage();
            UpdateLanguage();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(420, 290);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblEdit = new Label();
            lblEdit.Location = new Point(20, 20);
            lblEdit.Size = new Size(220, 25);

            trackBarEdit = new TrackBar();
            trackBarEdit.Location = new Point(20, 50);
            trackBarEdit.Size = new Size(300, 45);
            trackBarEdit.Minimum = 5;
            trackBarEdit.Maximum = 20;
            trackBarEdit.TickFrequency = 1;
            trackBarEdit.Value = 10;
            trackBarEdit.Scroll += TrackBarEdit_Scroll;

            labelEditValue = new Label();
            labelEditValue.Location = new Point(330, 55);
            labelEditValue.Size = new Size(60, 25);
            labelEditValue.Text = "1.0x";

            lblGrid = new Label();
            lblGrid.Location = new Point(20, 110);
            lblGrid.Size = new Size(240, 25);

            trackBarGrid = new TrackBar();
            trackBarGrid.Location = new Point(20, 140);
            trackBarGrid.Size = new Size(300, 45);
            trackBarGrid.Minimum = 5;
            trackBarGrid.Maximum = 20;
            trackBarGrid.TickFrequency = 1;
            trackBarGrid.Value = 8;
            trackBarGrid.Scroll += TrackBarGrid_Scroll;

            labelGridValue = new Label();
            labelGridValue.Location = new Point(330, 145);
            labelGridValue.Size = new Size(60, 25);
            labelGridValue.Text = "0.8x";

            btnOK = new Button();
            btnOK.Location = new Point(200, 210);
            btnOK.Size = new Size(90, 35);
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button();
            btnCancel.Location = new Point(300, 210);
            btnCancel.Size = new Size(90, 35);
            btnCancel.Click += BtnCancel_Click;

            this.Controls.AddRange(new Control[] {
                lblEdit, trackBarEdit, labelEditValue,
                lblGrid, trackBarGrid, labelGridValue,
                btnOK, btnCancel
            });
        }

        private void UpdateLanguage()
        {
            this.Text = LocalizationManager.GetString("fontSize");

            lblEdit.Text = LocalizationManager.GetString("fontSizeEditLabel");
            lblGrid.Text = LocalizationManager.GetString("fontSizeGridLabel");

            btnOK.Text = LocalizationManager.GetString("apply");
            btnCancel.Text = LocalizationManager.GetString("cancel");
        }

        private void TrackBarEdit_Scroll(object sender, EventArgs e)
        {
            float value = trackBarEdit.Value / 10f;
            labelEditValue.Text = $"{value:F1}x";
            editBoxSize = value;
        }

        private void TrackBarGrid_Scroll(object sender, EventArgs e)
        {
            float value = trackBarGrid.Value / 10f;
            labelGridValue.Text = $"{value:F1}x";
            dataGridViewSize = value;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            EditBoxFontSize = editBoxSize;
            DataGridViewFontSize = dataGridViewSize;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}