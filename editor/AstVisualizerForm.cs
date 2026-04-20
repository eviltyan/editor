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
        private Font nodeFont = new Font("Segoe UI", 9);
        private Font leafFont = new Font("Segoe UI", 9, FontStyle.Bold);
        private Pen linePen = new Pen(Color.FromArgb(100, 100, 100), 1.5f);
        private Brush nodeBgBrush = new SolidBrush(Color.FromArgb(240, 248, 255));
        private Brush leafBgBrush = new SolidBrush(Color.FromArgb(255, 255, 255));
        private Brush nodeBorderBrush = new SolidBrush(Color.FromArgb(70, 130, 180));
        private Brush leafBorderBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
        private Brush textBrush = new SolidBrush(Color.Black);

        private Dictionary<object, Rectangle> nodeRects = new Dictionary<object, Rectangle>();
        private int horizontalSpacing = 50;
        private int verticalSpacing = 70;
        private int nodePadding = 10;
        private int leafPadding = 8;
        private int arrowSize = 8;
        private int leafLevelY = 0;
        private int leftLeafAreaX = 30;
        private int treeStartX = 350;

        private Panel mainPanel;
        private PictureBox pictureBox;
        private Bitmap cachedBitmap;

        private class NodeLayout
        {
            public AstNode Node { get; set; }
            public Rectangle NodeRect { get; set; }
            public Rectangle ValueLeafRect { get; set; }
            public string ValueLeafText { get; set; }
            public List<NodeLayout> Children { get; set; } = new List<NodeLayout>();
            public int SubtreeWidth { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int LeafX { get; set; }
            public bool IsLeftLeaf { get; set; }
        }

        private List<NodeLayout> rootLayouts = new List<NodeLayout>();

        public AstVisualizerForm(List<VectorDeclNode> nodes)
        {
            InitializeComponent();
            this.astNodes = nodes;
            this.Text = "Визуализация AST";
            this.WindowState = FormWindowState.Maximized;
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
            if (this.IsHandleCreated && !this.Disposing && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!this.Disposing && !this.IsDisposed && this.IsHandleCreated)
                        RedrawCache();
                }));
            }
        }

        private void MainPanel_Resize(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.Disposing && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!this.Disposing && !this.IsDisposed && this.IsHandleCreated)
                        RedrawCache();
                }));
            }
        }

        public void UpdateAst(List<VectorDeclNode> nodes)
        {
            this.astNodes = nodes;
            RedrawCache();
        }

        private string GetValueLeafText(AstNode node)
        {
            if (node is VectorDeclNode vec)
                return $"\"{vec.Name}\"";
            if (node is FuncCallNode func)
                return $"\"{func.FunctionName}\"";
            if (node is NumberLiteralNode num)
                return num.Value;
            if (node is CharacterLiteralNode str)
                return $"\"{str.Value}\"";
            if (node is LogicalLiteralNode boolNode)
                return boolNode.Value ? "TRUE" : "FALSE";
            if (node is NullLiteralNode)
                return "NULL";
            return "";
        }

        private Size MeasureNode(AstNode node, Graphics g)
        {
            var nodeTypeSize = g.MeasureString(node.GetNodeType(), nodeFont);

            float maxWidth = nodeTypeSize.Width;
            float totalHeight = nodeTypeSize.Height + 4;

            foreach (var attr in node.GetAttributes())
            {
                string attrText = $"{attr.Key}: {FormatValue(attr.Value)}";
                var attrSize = g.MeasureString(attrText, leafFont);
                totalHeight += attrSize.Height + 2;
                maxWidth = Math.Max(maxWidth, attrSize.Width);
            }

            return new Size((int)maxWidth + nodePadding * 2, (int)totalHeight + nodePadding * 2);
        }

        private Size MeasureLeaf(string text, Graphics g)
        {
            var size = g.MeasureString(text, leafFont);
            return new Size((int)size.Width + leafPadding * 2, (int)size.Height + leafPadding * 2);
        }

        private string FormatValue(object value)
        {
            if (value is bool b) return b ? "True" : "False";
            if (value is string s) return $"\"{s}\"";
            return value?.ToString() ?? "null";
        }

        private NodeLayout BuildLayout(AstNode node, Graphics g)
        {
            if (node == null) return null;

            var layout = new NodeLayout();
            layout.Node = node;
            layout.ValueLeafText = GetValueLeafText(node);
            layout.IsLeftLeaf = (node is VectorDeclNode || node is FuncCallNode);

            var nodeSize = MeasureNode(node, g);
            layout.NodeRect = new Rectangle(0, 0, nodeSize.Width, nodeSize.Height);

            var leafSize = MeasureLeaf(layout.ValueLeafText, g);
            layout.ValueLeafRect = new Rectangle(0, 0, leafSize.Width, leafSize.Height);

            foreach (var child in node.GetChildren())
            {
                var childLayout = BuildLayout(child, g);
                if (childLayout != null)
                    layout.Children.Add(childLayout);
            }

            int childrenWidth = 0;
            foreach (var child in layout.Children)
                childrenWidth += child.SubtreeWidth;
            if (layout.Children.Count > 1)
                childrenWidth += (layout.Children.Count - 1) * horizontalSpacing;

            layout.SubtreeWidth = Math.Max(layout.NodeRect.Width, childrenWidth);

            return layout;
        }

        private void CalculatePositions(NodeLayout layout, int x, int y, List<NodeLayout> allLeaves)
        {
            if (layout == null) return;

            layout.X = x;
            layout.Y = y;

            int centerX = x + layout.SubtreeWidth / 2;
            layout.NodeRect = new Rectangle(
                centerX - layout.NodeRect.Width / 2,
                y,
                layout.NodeRect.Width,
                layout.NodeRect.Height);

            allLeaves.Add(layout);

            if (layout.Children.Count > 0)
            {
                int childrenTotalWidth = 0;
                foreach (var child in layout.Children)
                    childrenTotalWidth += child.SubtreeWidth;
                childrenTotalWidth += (layout.Children.Count - 1) * horizontalSpacing;

                int childStartX = centerX - childrenTotalWidth / 2;
                int childY = y + layout.NodeRect.Height + verticalSpacing;

                foreach (var child in layout.Children)
                {
                    CalculatePositions(child, childStartX, childY, allLeaves);
                    childStartX += child.SubtreeWidth + horizontalSpacing;
                }
            }
        }

        private void AssignLeafPositions(List<NodeLayout> allLeaves, Graphics g)
        {
            var leftLeaves = allLeaves.Where(l => l.IsLeftLeaf).ToList();
            var rightLeaves = allLeaves.Where(l => !l.IsLeftLeaf).OrderBy(l => l.NodeRect.X).ToList();

            int leftX = leftLeafAreaX;
            int maxLeftLeafHeight = 0;
            foreach (var leaf in leftLeaves)
            {
                leaf.LeafX = leftX;
                leaf.ValueLeafRect = new Rectangle(
                    leftX,
                    leafLevelY,
                    leaf.ValueLeafRect.Width,
                    leaf.ValueLeafRect.Height);
                leftX += leaf.ValueLeafRect.Width + horizontalSpacing;
                maxLeftLeafHeight = Math.Max(maxLeftLeafHeight, leaf.ValueLeafRect.Height);
            }

            int rightX = Math.Max(leftX + 50, 500);
            foreach (var leaf in rightLeaves)
            {
                leaf.LeafX = rightX;
                leaf.ValueLeafRect = new Rectangle(
                    rightX,
                    leafLevelY,
                    leaf.ValueLeafRect.Width,
                    leaf.ValueLeafRect.Height);
                rightX += leaf.ValueLeafRect.Width + horizontalSpacing;
            }
        }

        private void DrawLayout(NodeLayout layout, Graphics g)
        {
            if (layout == null) return;

            DrawNodeBox(layout.Node, layout.NodeRect, g);
            nodeRects[layout.Node] = layout.NodeRect;

            int nodeBottomX = layout.NodeRect.X + layout.NodeRect.Width / 2;
            int nodeBottomY = layout.NodeRect.Bottom;
            int leafTopX = layout.ValueLeafRect.X + layout.ValueLeafRect.Width / 2;
            int leafTopY = layout.ValueLeafRect.Y;

            if (layout.IsLeftLeaf)
            {
                int leftX = layout.ValueLeafRect.X + layout.ValueLeafRect.Width + 10;
                int midY1 = nodeBottomY + 30;
                int midY2 = leafTopY - 30;

                g.DrawLine(linePen, nodeBottomX, nodeBottomY, nodeBottomX, midY1);
                g.DrawLine(linePen, nodeBottomX, midY1, leftX, midY1);
                g.DrawLine(linePen, leftX, midY1, leftX, midY2);
                g.DrawLine(linePen, leftX, midY2, leafTopX, midY2);
                g.DrawLine(linePen, leafTopX, midY2, leafTopX, leafTopY);
            }
            else
            {
                int midY = nodeBottomY + (leafTopY - nodeBottomY) / 2;
                g.DrawLine(linePen, nodeBottomX, nodeBottomY, nodeBottomX, midY);
                g.DrawLine(linePen, nodeBottomX, midY, leafTopX, midY);
                g.DrawLine(linePen, leafTopX, midY, leafTopX, leafTopY);
            }

            DrawArrow(g, leafTopX, leafTopY, true);

            DrawValueLeaf(layout.ValueLeafText, layout.ValueLeafRect, g);
            nodeRects[layout.ValueLeafText] = layout.ValueLeafRect;
            if (layout.Children.Count > 0)
            {
                var childTops = new List<Point>();
                foreach (var child in layout.Children)
                {
                    childTops.Add(new Point(
                        child.NodeRect.X + child.NodeRect.Width / 2,
                        child.NodeRect.Y));
                }

                if (layout.Children.Count == 1)
                {
                    g.DrawLine(linePen, nodeBottomX, nodeBottomY, childTops[0].X, childTops[0].Y);
                    DrawArrow(g, childTops[0].X, childTops[0].Y, true);
                }
                else
                {
                    int minX = childTops.Min(p => p.X);
                    int maxX = childTops.Max(p => p.X);
                    int lineY = nodeBottomY + verticalSpacing / 2;

                    g.DrawLine(linePen, nodeBottomX, nodeBottomY, nodeBottomX, lineY);
                    g.DrawLine(linePen, minX, lineY, maxX, lineY);

                    foreach (var childTop in childTops)
                    {
                        g.DrawLine(linePen, childTop.X, lineY, childTop.X, childTop.Y);
                        DrawArrow(g, childTop.X, childTop.Y, true);
                    }
                }

                foreach (var child in layout.Children)
                {
                    DrawLayout(child, g);
                }
            }
        }

        private void DrawNodeBox(AstNode node, Rectangle rect, Graphics g)
        {
            using (Brush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, new Rectangle(rect.X + 3, rect.Y + 3, rect.Width, rect.Height));
            }

            g.FillRectangle(nodeBgBrush, rect);

            using (Pen borderPen = new Pen(nodeBorderBrush, 2))
            {
                g.DrawRectangle(borderPen, rect);
            }

            float currentY = rect.Y + nodePadding;

            using (var boldFont = new Font(nodeFont, FontStyle.Bold))
            {
                g.DrawString(node.GetNodeType(), boldFont, textBrush, rect.X + nodePadding, currentY);
                currentY += boldFont.GetHeight() + 4;
            }

            foreach (var attr in node.GetAttributes())
            {
                string attrText = $"{attr.Key}: {FormatValue(attr.Value)}";
                g.DrawString(attrText, leafFont, textBrush, rect.X + nodePadding + 10, currentY);
                currentY += leafFont.GetHeight() + 2;
            }
        }

        private void DrawValueLeaf(string text, Rectangle rect, Graphics g)
        {
            g.FillRectangle(leafBgBrush, rect);

            using (Pen borderPen = new Pen(leafBorderBrush, 1))
            {
                g.DrawRectangle(borderPen, rect);
            }

            var textSize = g.MeasureString(text, leafFont);
            float textX = rect.X + (rect.Width - textSize.Width) / 2;
            float textY = rect.Y + (rect.Height - textSize.Height) / 2;

            g.DrawString(text, leafFont, textBrush, textX, textY);
        }

        private void DrawArrow(Graphics g, int x, int y, bool pointingDown)
        {
            PointF[] arrowPoints;

            if (pointingDown)
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y - 2),
                    new PointF(x - arrowSize / 2, y - arrowSize - 2),
                    new PointF(x + arrowSize / 2, y - arrowSize - 2)
                };
            }
            else
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y + 2),
                    new PointF(x - arrowSize / 2, y + arrowSize + 2),
                    new PointF(x + arrowSize / 2, y + arrowSize + 2)
                };
            }

            using (Brush arrowBrush = new SolidBrush(linePen.Color))
            {
                g.FillPolygon(arrowBrush, arrowPoints);
            }
        }

        private void RedrawCache()
        {
            if (astNodes == null || astNodes.Count == 0) return;
            if (this.Disposing || this.IsDisposed) return;

            if (cachedBitmap != null)
            {
                cachedBitmap.Dispose();
                cachedBitmap = null;
            }

            rootLayouts.Clear();
            nodeRects.Clear();

            using (Graphics g = this.CreateGraphics())
            {
                foreach (var node in astNodes)
                {
                    var layout = BuildLayout(node, g);
                    if (layout != null)
                        rootLayouts.Add(layout);
                }

                var allLeaves = new List<NodeLayout>();
                int currentY = 30;

                foreach (var root in rootLayouts)
                {
                    CalculatePositions(root, treeStartX, currentY, allLeaves);
                    int subtreeHeight = CalculateHeight(root);
                    currentY += subtreeHeight + 80;
                }

                int maxNodeBottom = 0;
                foreach (var leaf in allLeaves)
                {
                    maxNodeBottom = Math.Max(maxNodeBottom, leaf.NodeRect.Bottom);
                }

                leafLevelY = maxNodeBottom + verticalSpacing;

                AssignLeafPositions(allLeaves, g);

                int maxWidth = 800;
                int totalHeight = leafLevelY + 80;

                foreach (var leaf in allLeaves)
                {
                    maxWidth = Math.Max(maxWidth, leaf.ValueLeafRect.Right + 50);
                }

                foreach (var root in rootLayouts)
                {
                    maxWidth = Math.Max(maxWidth, root.X + root.SubtreeWidth + 50);
                }

                maxWidth = Math.Max(maxWidth, mainPanel.ClientSize.Width - 50);

                cachedBitmap = new Bitmap(maxWidth, totalHeight);

                using (Graphics g2 = Graphics.FromImage(cachedBitmap))
                {
                    g2.Clear(Color.White);
                    g2.SmoothingMode = SmoothingMode.AntiAlias;
                    g2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    foreach (var root in rootLayouts)
                    {
                        DrawLayout(root, g2);
                    }
                }
            }

            pictureBox.Image = cachedBitmap;
            pictureBox.Size = cachedBitmap.Size;
            mainPanel.AutoScrollMinSize = cachedBitmap.Size;
        }

        private int CalculateHeight(NodeLayout layout)
        {
            if (layout == null) return 0;

            int maxChildHeight = 0;
            foreach (var child in layout.Children)
                maxChildHeight = Math.Max(maxChildHeight, CalculateHeight(child));

            if (layout.Children.Count > 0)
                return layout.NodeRect.Height + verticalSpacing + maxChildHeight;
            else
                return layout.NodeRect.Height;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (cachedBitmap == null)
            {
                MessageBox.Show("Нет изображения для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        MessageBox.Show($"AST сохранено в {sfd.FileName}", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}