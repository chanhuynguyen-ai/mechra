using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SwCursor.SolidWorksAddin.UI
{
    internal static class ProductTheme
    {
        public static readonly Color Canvas = Color.FromArgb(9, 10, 12);
        public static readonly Color Surface = Color.FromArgb(17, 19, 23);
        public static readonly Color Raised = Color.FromArgb(25, 28, 33);
        public static readonly Color Border = Color.FromArgb(43, 47, 55);
        public static readonly Color Text = Color.FromArgb(237, 240, 244);
        public static readonly Color Muted = Color.FromArgb(151, 160, 175);
        public static readonly Color Accent = Color.FromArgb(166, 203, 220);
        public static readonly Color Success = Color.FromArgb(115, 216, 174);
        public static readonly Color Warning = Color.FromArgb(242, 181, 137);

        public static GraphicsPath Round(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            if (d <= 0) return path;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        public static Label Label(string text, float size = 9.5F, Color? color = null, bool bold = false)
        {
            var font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            var label = new Label { Text = text, AutoSize = true, Dock = DockStyle.Fill,
                ForeColor = color ?? Text, BackColor = Color.Transparent, Font = font,
                UseMnemonic = false, Margin = new Padding(0, 0, 0, 9) };
            label.Disposed += (_, __) => font.Dispose();
            return label;
        }
    }

    internal sealed class ProductButton : Button
    {
        public bool Primary { get; set; }
        private bool _hover;
        public ProductButton(string text, int width = 110, bool primary = false)
        {
            Text = text; Width = width; Height = 34; Primary = primary;
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = ProductTheme.Surface; ForeColor = ProductTheme.Text;
            Cursor = Cursors.Hand; Margin = new Padding(0, 0, 8, 0);
            AccessibleName = text; UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? ProductTheme.Canvas : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = !Enabled ? ProductTheme.Surface : Primary ? (_hover ? Color.White : ProductTheme.Text)
                : (_hover ? ProductTheme.Border : ProductTheme.Raised);
            using (var path = ProductTheme.Round(new RectangleF(0.5F, 0.5F, Width - 1, Height - 1), 8 * DeviceDpi / 96F))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(Primary && Enabled ? fill : ProductTheme.Border))
            { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path); }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
                !Enabled ? ProductTheme.Muted : Primary ? ProductTheme.Canvas : ProductTheme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,
                Rectangle.Inflate(ClientRectangle, -5, -5), Primary ? ProductTheme.Canvas : ProductTheme.Text, fill);
        }
    }

    internal sealed class StackCard : TableLayoutPanel
    {
        public Color LineColor { get; set; } = ProductTheme.Border;
        public StackCard()
        {
            ColumnCount = 1; ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16); Margin = new Padding(0, 0, 0, 14);
            BackColor = ProductTheme.Surface;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public void Add(Control control)
        {
            int row = RowCount++; RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(control, 0, row);
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            int available = Math.Max(1, ClientSize.Width - Padding.Horizontal);
            foreach (Control child in Controls)
                if (child is Label) child.MaximumSize = new Size(Math.Max(1, available - child.Margin.Horizontal), 0);
            base.OnLayout(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(LineColor))
            using (var path = ProductTheme.Round(new RectangleF(0.5F, 0.5F, Width - 1, Height - 1), 10 * DeviceDpi / 96F))
                e.Graphics.DrawPath(pen, path);
        }
    }

    internal sealed class DetailPreview : Control
    {
        public DetailPreview()
        {
            ForeColor = ProductTheme.Muted; BackColor = ProductTheme.Surface; TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = Math.Max(1, proposedSize.Width);
            int limit = (int)Math.Round(136 * DeviceDpi / 96F);
            int height = TextRenderer.MeasureText(Text, Font, new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
            return new Size(width, Math.Min(limit, height));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class WireMark : Control
    {
        public WireMark() { Size = new Size(36, 36); TabStop = false; SetStyle(ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float x = Width / 2F, y = Height / 2F, s = Math.Min(Width, Height) * .35F;
            PointF a = new PointF(x, y - s), b = new PointF(x + s, y - s / 2),
                c = new PointF(x + s, y + s / 2), d = new PointF(x, y + s),
                f = new PointF(x - s, y + s / 2), g = new PointF(x - s, y - s / 2), m = new PointF(x, y);
            using (var pen = new Pen(ProductTheme.Accent, 1.4F * DeviceDpi / 96F))
            { e.Graphics.DrawPolygon(pen, new[] { a, b, c, d, f, g });
              e.Graphics.DrawLine(pen, g, m); e.Graphics.DrawLine(pen, b, m); e.Graphics.DrawLine(pen, d, m); }
        }
    }
}
