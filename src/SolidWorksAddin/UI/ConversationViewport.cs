using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SwCursor.SolidWorksAddin.UI
{
    // No native AutoScroll: one explicit offset and one dark scrollbar own the viewport.
    internal sealed class ConversationViewport : Panel
    {
        private readonly List<StackCard> _cards = new List<StackCard>();
        private readonly DarkScrollBar _bar = new DarkScrollBar();
        private readonly ProductButton _latest = new ProductButton("Tin mới ↓", 110);
        private int _offset, _contentHeight;
        private bool _layout, _queued, _unread;
        public IReadOnlyList<StackCard> Cards => _cards;
        public int ScrollOffset => _offset;
        public int MaximumOffset => Math.Max(0, _contentHeight - ClientSize.Height);
        public bool HasUnread => _unread;
        private int Px(int n) => Math.Max(1, (int)Math.Round(n * DeviceDpi / 96.0));

        public ConversationViewport()
        {
            AutoScroll = false; BackColor = ProductTheme.Canvas; TabStop = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _bar.ScrollRequested += SetOffset;
            _latest.Visible = false; _latest.Click += (_, __) => { _unread = false; SetOffset(MaximumOffset); };
            Controls.Add(_bar); Controls.Add(_latest);
        }
        public void AddCard(StackCard card, bool reveal = false)
        {
            bool follow = _cards.Count == 0 || MaximumOffset - _offset <= Px(16);
            card.AutoSize = false; card.Dock = DockStyle.None; card.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _cards.Add(card); Controls.Add(card); WireChildren(card);
            card.Layout += (_, __) => QueueReflow();
            Reflow();
            if (reveal || follow) Reveal(card);
            else { _unread = true; PlaceChrome(); }
        }
        public void RemoveCard(StackCard card)
        {
            _cards.Remove(card); Controls.Remove(card); Reflow();
        }
        public void ClearCards()
        {
            foreach (var card in _cards.ToArray()) { Controls.Remove(card); card.Dispose(); }
            _cards.Clear(); _offset = _contentHeight = 0; _unread = false; Reflow();
        }
        private void WireChildren(Control c)
        {
            c.MouseWheel += ChildWheel;
            c.Enter += (_, __) => EnsureVisible(c);
            foreach (Control child in c.Controls) WireChildren(child);
        }
        private void ChildWheel(object sender, MouseEventArgs e)
        {
            var handled = e as HandledMouseEventArgs;
            if (handled != null && handled.Handled) return;
            SetOffset(_offset - e.Delta * Px(42) / 120);
            if (handled != null) handled.Handled = true;
        }
        protected override void OnMouseWheel(MouseEventArgs e) { ChildWheel(this, e); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); Reflow(); }
        private void QueueReflow()
        {
            if (_layout || _queued || !IsHandleCreated || IsDisposed || Disposing) return;
            _queued = true;
            BeginInvoke((Action)(() => { _queued = false; if (!IsDisposed && !Disposing) Reflow(); }));
        }
        public void Reflow()
        {
            if (_layout || _bar == null || IsDisposed) return;
            _layout = true;
            try {
                // Keep the first visible card anchored when text wraps or details expand.
                StackCard anchor = _cards.FirstOrDefault(c => c.Bottom > 0);
                int anchorY = anchor == null ? 0 : anchor.Top;
                int anchoredTop = -1;
                int width = Math.Max(1, ClientSize.Width - Px(13));
                int top = Px(4);
                foreach (StackCard card in _cards)
                {
                    card.SuspendLayout();
                    card.MaximumSize = new Size(width, 0); card.MinimumSize = Size.Empty; card.Width = width;
                    card.ResumeLayout(true);
                    int height = Math.Max(Px(40), card.GetPreferredSize(new Size(width, 0)).Height);
                    if (card == anchor) anchoredTop = top;
                    card.SetBounds(0, top - _offset, width, height);
                    top += height + Px(10);
                }
                _contentHeight = top;
                if (anchoredTop >= 0) _offset = anchoredTop - anchorY;
                _offset = Math.Max(0, Math.Min(_offset, MaximumOffset));
                PositionCards(); PlaceChrome();
            } finally { _layout = false; }
        }
        private void PositionCards()
        {
            int top = Px(4) - _offset;
            foreach (var card in _cards) { card.Top = top; top += card.Height + Px(10); }
        }
        public void SetOffset(int value)
        {
            _offset = Math.Max(0, Math.Min(value, MaximumOffset));
            if (MaximumOffset - _offset <= Px(4)) _unread = false;
            PositionCards(); PlaceChrome();
        }
        private void PlaceChrome()
        {
            _bar.SetBounds(Math.Max(0, ClientSize.Width - Px(10)), 0, Px(10), ClientSize.Height);
            _bar.SetRange(_contentHeight, ClientSize.Height, _offset);
            _bar.Visible = MaximumOffset > 0; _bar.BringToFront();
            _latest.Size = new Size(Math.Min(Px(110), ClientSize.Width), Px(30));
            _latest.Location = new Point(Math.Max(0, (ClientSize.Width - _latest.Width) / 2), Math.Max(0, ClientSize.Height - _latest.Height - Px(8)));
            _latest.Visible = _unread; _latest.BringToFront();
        }
        private void Reveal(StackCard card)
        {
            int top = card.Top + _offset;
            SetOffset(card.Height > ClientSize.Height ? top : top + card.Height + Px(10) - ClientSize.Height);
        }
        private void EnsureVisible(Control c)
        {
            if (!c.IsHandleCreated || !IsHandleCreated) return;
            var bounds = RectangleToClient(c.RectangleToScreen(c.ClientRectangle));
            if (bounds.Top < 0) SetOffset(_offset + bounds.Top - Px(4));
            else if (bounds.Bottom > ClientSize.Height) SetOffset(_offset + bounds.Bottom - ClientSize.Height + Px(4));
        }
    }

    internal sealed class DarkScrollBar : Control
    {
        private int _total, _view, _value, _grab;
        private bool _drag;
        public event Action<int> ScrollRequested;
        private int Maximum => Math.Max(0, _total - _view);
        public DarkScrollBar()
        {
            BackColor = ProductTheme.Canvas; TabStop = true;
            AccessibleRole = AccessibleRole.ScrollBar; AccessibleName = "Cuộn hội thoại";
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public void SetRange(int total, int view, int value) { _total = total; _view = view; _value = value; Invalidate(); }
        private Rectangle Thumb
        {
            get {
                int height = Math.Min(Height, Math.Max((int)(24 * DeviceDpi / 96F), (int)((long)Height * _view / Math.Max(1, _total))));
                int top = Maximum == 0 ? 0 : (int)((long)(Height - height) * _value / Maximum);
                return new Rectangle(Width / 3, top, Math.Max(2, Width / 3), height);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (Maximum == 0 || Height == 0) return;
            using (var brush = new SolidBrush(_drag || Focused ? ProductTheme.Muted : Color.FromArgb(67, 73, 82)))
                e.Graphics.FillRectangle(brush, Thumb);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return;
            Focus(); Rectangle thumb = Thumb;
            if (e.Y >= thumb.Top && e.Y < thumb.Bottom) { _grab = e.Y - thumb.Top; _drag = true; Capture = true; }
            else ScrollRequested?.Invoke(_value + (e.Y < thumb.Top ? -_view : _view));
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_drag) ScrollRequested?.Invoke((int)((long)(e.Y - _grab) * Maximum / Math.Max(1, Height - Thumb.Height)));
        }
        protected override void OnMouseUp(MouseEventArgs e) { _drag = false; Capture = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { if (!Capture) _drag = false; base.OnMouseCaptureChanged(e); }
        protected override bool IsInputKey(Keys keyData)
        { return (keyData & Keys.KeyCode) == Keys.Up || (keyData & Keys.KeyCode) == Keys.Down || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            int next = _value;
            switch (e.KeyCode) {
                case Keys.Up: next -= 40; break; case Keys.Down: next += 40; break;
                case Keys.PageUp: next -= _view; break; case Keys.PageDown: next += _view; break;
                case Keys.Home: next = 0; break; case Keys.End: next = Maximum; break;
                default: base.OnKeyDown(e); return;
            }
            e.Handled = true; e.SuppressKeyPress = true; ScrollRequested?.Invoke(next);
        }
    }
}
