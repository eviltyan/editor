using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace editor
{
    public partial class AstVisualizerForm : Form
    {
        private List<VectorDeclNode> astNodes;
        private Font nodeFont = new Font("Segoe UI", 10);
        private Font attrFont = new Font("Segoe UI", 9);
        private Pen linePen = new Pen(Color.FromArgb(100, 100, 100), 1.5f);
        private Brush nodeBgBrush = new SolidBrush(Color.FromArgb(240, 248, 255));
        private Brush nodeBorderBrush = new SolidBrush(Color.FromArgb(70, 130, 180));
        private Brush textBrush = new SolidBrush(Color.Black);
        private Brush attrTextBrush = new SolidBrush(Color.FromArgb(60, 60, 60));

        private Dictionary<AstNode, Rectangle> nodeRects = new Dictionary<AstNode, Rectangle>();
        private int horizontalSpacing = 80;
        private int verticalSpacing = 60;
        private int nodePadding = 12;
        private int arrowSize = 8;

        private Panel mainPanel;
        private PictureBox pictureBox;
        private Bitmap cachedBitmap;
        private bool needsRedraw = true;

        public AstVisualizerForm(List<VectorDeclNode> nodes)
        {
            InitializeComponent();

            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "AstVisualizerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

            this.astNodes = nodes;
            this.Text = "Визуализация AST";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.AutoScroll = true;
            mainPanel.BackColor = Color.White;
            mainPanel.Resize += MainPanel_Resize;
            this.Controls.Add(mainPanel);

            pictureBox = new PictureBox();
            pictureBox.Location = new Point(0, 0);
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox.BackColor = Color.White;
            mainPanel.Controls.Add(pictureBox);

            Button saveButton = new Button();
            saveButton.Text = "Сохранить как изображение";
            saveButton.Dock = DockStyle.Bottom;
            saveButton.Height = 40;
            saveButton.BackColor = Color.FromArgb(240, 240, 240);
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);

            this.Resize += AstVisualizerForm_Resize;
            this.Shown += AstVisualizerForm_Shown;
        }

        private void AstVisualizerForm_Shown(object sender, EventArgs e)
        {
            RedrawCache();
        }

        private void AstVisualizerForm_Resize(object sender, EventArgs e)
        {
            needsRedraw = true;

            if (this.IsHandleCreated && !this.Disposing)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!this.Disposing && this.IsHandleCreated)
                    {
                        RedrawCache();
                    }
                }));
            }
        }

        private void MainPanel_Resize(object sender, EventArgs e)
        {
            needsRedraw = true;

            if (this.IsHandleCreated && !this.Disposing)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!this.Disposing && this.IsHandleCreated)
                    {
                        RedrawCache();
                    }
                }));
            }
        }

        private void RedrawCache()
        {
            if (astNodes == null || astNodes.Count == 0) return;
            if (!needsRedraw && cachedBitmap != null) return;

            if (cachedBitmap != null)
            {
                cachedBitmap.Dispose();
                cachedBitmap = null;
            }

            using (Graphics measureGraphics = CreateGraphics())
            {
                nodeRects.Clear();
                int totalWidth = 800;
                int totalHeight = 100;
                int currentY = 30;

                foreach (var node in astNodes)
                {
                    var treeSize = CalculateTreeSize(node, measureGraphics);
                    totalWidth = Math.Max(totalWidth, treeSize.Width + 100);
                    currentY += treeSize.Height + 70;
                }
                totalHeight = currentY + 50;
                totalWidth = Math.Max(totalWidth, mainPanel.ClientSize.Width - 50);

                cachedBitmap = new Bitmap(totalWidth, totalHeight);

                using (Graphics g = Graphics.FromImage(cachedBitmap))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    nodeRects.Clear();
                    int startX = 50;
                    int startY = 30;
                    currentY = startY;

                    foreach (var node in astNodes)
                    {
                        var treeSize = CalculateTreeSize(node, g);

                        int panelWidth = Math.Max(mainPanel.ClientSize.Width, totalWidth);
                        int treeX = Math.Max(startX, (panelWidth - treeSize.Width) / 2);

                        DrawTree(node, g, treeX, currentY);

                        currentY += treeSize.Height + 70;
                    }
                }
            }

            pictureBox.Image = cachedBitmap;
            pictureBox.Size = cachedBitmap.Size;

            mainPanel.AutoScrollMinSize = cachedBitmap.Size;

            needsRedraw = false;
        }

        private Size CalculateTreeSize(AstNode node, Graphics g)
        {
            if (node == null) return new Size(0, 0);

            var nodeSize = MeasureNode(node, g);

            var children = node.GetChildren().ToList();
            if (children.Count == 0)
            {
                return new Size(nodeSize.Width, nodeSize.Height);
            }

            int totalWidth = 0;
            int maxHeight = 0;

            foreach (var child in children)
            {
                var childSize = CalculateTreeSize(child, g);
                totalWidth += childSize.Width;
                maxHeight = Math.Max(maxHeight, childSize.Height);
            }

            totalWidth += (children.Count - 1) * horizontalSpacing;
            totalWidth = Math.Max(nodeSize.Width, totalWidth);

            return new Size(totalWidth, nodeSize.Height + verticalSpacing + maxHeight);
        }

        private Size MeasureNode(AstNode node, Graphics g)
        {
            var nodeTypeSize = g.MeasureString(node.GetNodeType(), nodeFont);

            float attrHeight = 0;
            float maxWidth = nodeTypeSize.Width;

            foreach (var attr in node.GetAttributes())
            {
                string attrText = $"{attr.Key}: {FormatValue(attr.Value)}";
                var attrSize = g.MeasureString(attrText, attrFont);
                attrHeight += attrSize.Height + 2;
                maxWidth = Math.Max(maxWidth, attrSize.Width);
            }

            return new Size(
                (int)maxWidth + nodePadding * 2,
                (int)(nodeTypeSize.Height + attrHeight + nodePadding * 2)
            );
        }

        private string FormatValue(object value)
        {
            if (value is bool b) return b ? "True" : "False";
            if (value is string s) return $"\"{s}\"";
            return value?.ToString() ?? "null";
        }

        private Point DrawTree(AstNode node, Graphics g, int x, int y)
        {
            if (node == null) return new Point(x, y);

            var children = node.GetChildren().ToList();

            List<Point> childPositions = new List<Point>();
            List<Size> childSizes = new List<Size>();

            if (children.Count > 0)
            {
                foreach (var child in children)
                {
                    childSizes.Add(CalculateTreeSize(child, g));
                }

                int childrenTotalWidth = childSizes.Sum(s => s.Width);
                int startX = x + (CalculateTreeSize(node, g).Width - childrenTotalWidth - (children.Count - 1) * horizontalSpacing) / 2;
                int childY = y + MeasureNode(node, g).Height + verticalSpacing;

                int currentX = startX;
                for (int i = 0; i < children.Count; i++)
                {
                    int childCenterX = currentX + childSizes[i].Width / 2;
                    childPositions.Add(new Point(childCenterX, childY));
                    currentX += childSizes[i].Width + horizontalSpacing;
                }
            }

            var nodeSize = MeasureNode(node, g);
            int nodeX;

            if (children.Count > 0)
            {
                nodeX = childPositions.Min(p => p.X) + (childPositions.Max(p => p.X) - childPositions.Min(p => p.X)) / 2 - nodeSize.Width / 2;
            }
            else
            {
                nodeX = x;
            }

            var nodeRect = new Rectangle(nodeX, y, nodeSize.Width, nodeSize.Height);
            DrawNode(node, g, nodeRect);
            nodeRects[node] = nodeRect;

            if (children.Count > 0)
            {
                int nodeBottomCenter = nodeRect.X + nodeRect.Width / 2;
                int nodeBottom = nodeRect.Bottom;

                if (children.Count > 1)
                {
                    int minX = childPositions.Min(p => p.X);
                    int maxX = childPositions.Max(p => p.X);
                    int lineY = nodeBottom + verticalSpacing / 2;

                    g.DrawLine(linePen, nodeBottomCenter, nodeBottom, nodeBottomCenter, lineY);

                    g.DrawLine(linePen, minX, lineY, maxX, lineY);

                    for (int i = 0; i < childPositions.Count; i++)
                    {
                        g.DrawLine(linePen, childPositions[i].X, lineY, childPositions[i].X, childPositions[i].Y);
                    }
                }
                else if (children.Count == 1)
                {
                    g.DrawLine(linePen, nodeBottomCenter, nodeBottom, childPositions[0].X, childPositions[0].Y);
                }

                for (int i = 0; i < children.Count; i++)
                {
                    DrawArrow(g, childPositions[i].X, childPositions[i].Y, true);
                }

                for (int i = 0; i < children.Count; i++)
                {
                    DrawTree(children[i], g, childPositions[i].X - childSizes[i].Width / 2, childPositions[i].Y);
                }
            }

            return new Point(nodeX, y);
        }

        private void DrawArrow(Graphics g, int x, int y, bool pointingDown)
        {
            PointF[] arrowPoints;

            if (pointingDown)
            {
                y = y + 3;
                arrowPoints = new PointF[]
                {
                    new PointF(x, y - arrowSize / 2),
                    new PointF(x - arrowSize / 2, y - arrowSize),
                    new PointF(x + arrowSize / 2, y - arrowSize)
                };
            }
            else
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y + arrowSize / 2),
                    new PointF(x - arrowSize / 2, y + arrowSize),
                    new PointF(x + arrowSize / 2, y + arrowSize)
                };
            }

            using (Brush arrowBrush = new SolidBrush(linePen.Color))
            {
                g.FillPolygon(arrowBrush, arrowPoints);
            }
        }

        private void DrawNode(AstNode node, Graphics g, Rectangle rect)
        {
            g.FillRectangle(nodeBgBrush, rect);

            using (Pen borderPen = new Pen(nodeBorderBrush, 2))
            {
                g.DrawRectangle(borderPen, rect);
            }

            float currentY = rect.Y + nodePadding;

            using (var boldFont = new Font(nodeFont, FontStyle.Bold))
            {
                g.DrawString(node.GetNodeType(), boldFont, textBrush,
                    rect.X + nodePadding, currentY);
                currentY += boldFont.GetHeight() + 6;
            }

            foreach (var attr in node.GetAttributes())
            {
                string attrText = $"{attr.Key}: {FormatValue(attr.Value)}";
                g.DrawString(attrText, attrFont, attrTextBrush,
                    rect.X + nodePadding + 5, currentY);
                currentY += attrFont.GetHeight() + 2;
            }
        }

        public void UpdateAst(List<VectorDeclNode> nodes)
        {
            this.astNodes = nodes;
            needsRedraw = true;
            RedrawCache();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (cachedBitmap == null)
            {
                MessageBox.Show("Нет изображения для сохранения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                sfd.DefaultExt = "png";
                sfd.FileName = "AST_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        cachedBitmap.Save(sfd.FileName);
                        MessageBox.Show($"AST сохранено в {sfd.FileName}", "Сохранение",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}