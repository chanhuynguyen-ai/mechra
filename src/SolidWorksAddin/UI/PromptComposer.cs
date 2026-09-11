using System;
using System.Drawing;
using System.Windows.Forms;

namespace SwCursor.SolidWorksAddin.UI
{
    // Explicit rectangles separate the editor from its actions at every size.
    internal sealed class PromptComposer : Panel
    {
        public TextBox Editor { get; }
        public ProductButton SendButton { get; }
        private readonly Label _placeholder, _hint;
        private bool _layout;
        public event EventHandler PreferredHeightChanged;
        public int DesiredHeight { get; private set; } = 92;
        private int Px(int n) => Math.Max(1, (int)Math.Round(n * DeviceDpi / 96.0));
        public PromptComposer()
        {
            BackColor = ProductTheme.Surface;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Editor = new TextBox { Multiline = true, AcceptsReturn = true, WordWrap = true,
                BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.None, MaxLength = 4000,
                BackColor = ProductTheme.Surface, ForeColor = ProductTheme.Text,
                AccessibleName = "Nhập yêu cầu CAD, đơn vị mm" };
            _placeholder = ProductTheme.Label("Bạn muốn tạo hoặc sửa plate?", 9, ProductTheme.Muted);
            _placeholder.Name = "PromptPlaceholder"; _placeholder.TabStop = false;
            _placeholder.AutoSize = false; _placeholder.AutoEllipsis = true; _placeholder.Dock = DockStyle.None;
            _placeholder.Cursor = Cursors.IBeam; _placeholder.Click += (_, __) => Editor.Focus();
            _hint = ProductTheme.Label("Shift+Enter: xuống dòng", 7.5F, ProductTheme.Muted);
            _hint.AutoSize = false; _hint.AutoEllipsis = true; _hint.Dock = DockStyle.None;
            _hint.TextAlign = ContentAlignment.MiddleLeft;
            SendButton = new ProductButton("Gửi ↑", 64, true) { Enabled = false };
            Controls.AddRange(new Control[] { Editor, _placeholder, _hint, SendButton });
            _placeholder.BringToFront();
            Editor.GotFocus += (_, __) => UpdatePlaceholder(); Editor.LostFocus += (_, __) => UpdatePlaceholder();
            Editor.TextChanged += (_, __) => { UpdatePlaceholder(); PerformLayout(); };
            UpdatePlaceholder();
        }
        private void UpdatePlaceholder()
        {
            // The hint must be above the empty native TextBox, but never cover its caret.
            _placeholder.Visible = Editor.TextLength == 0 && !Editor.Focused;
            Invalidate();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e); if (_layout || Editor == null) return;
            _layout = true;
            int wanted;
            try {
                int pad = Px(10), buttonHeight = Math.Min(Px(32), Math.Max(0, Height - pad * 2));
                int actionY = Math.Max(pad, Height - pad - buttonHeight);
                int inputHeight = Math.Max(1, actionY - pad - Px(8));
                Editor.SetBounds(pad, pad, Math.Max(1, Width - pad * 2), inputHeight);
                _placeholder.Bounds = Editor.Bounds;
                int buttonWidth = Math.Min(Px(64), Math.Max(1, Width - pad * 2));
                SendButton.SetBounds(Math.Max(pad, Width - pad - buttonWidth), actionY, buttonWidth, buttonHeight);
                _hint.SetBounds(pad, actionY, Math.Max(0, SendButton.Left - pad - Px(8)), buttonHeight);
                _hint.Text = Width < Px(290) ? "Shift+Enter: dòng mới" : "Shift+Enter: xuống dòng";
                int lines = Editor.IsHandleCreated ? Editor.GetLineFromCharIndex(Editor.TextLength) + 1 : 1;
                int lineHeight = Math.Max(Px(18), TextRenderer.MeasureText("Ag", Editor.Font).Height);
                wanted = pad * 2 + Math.Max(2, Math.Min(4, lines)) * lineHeight + Px(8) + Px(32);
            } finally { _layout = false; }
            if (wanted != DesiredHeight) { DesiredHeight = wanted; PreferredHeightChanged?.Invoke(this, EventArgs.Empty); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if (Width < 2 || Height < 2) return;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(Editor.Focused ? ProductTheme.Accent : ProductTheme.Border))
            using (var path = ProductTheme.Round(new RectangleF(.5F, .5F, Width - 1, Height - 1), Px(9)))
                e.Graphics.DrawPath(pen, path);
        }
    }
}
