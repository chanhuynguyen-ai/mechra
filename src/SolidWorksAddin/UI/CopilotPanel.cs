using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SwCursor.SolidWorksAddin.Services;

namespace SwCursor.SolidWorksAddin.UI
{
    public sealed class CopilotPanel : UserControl
    {
        private readonly ModelContextService _context;
        private readonly AgentClient _agent;
        private readonly RichTextBox _conversation;
        private readonly TextBox _input;
        private readonly Button _send;
        private readonly Button _check;

        public CopilotPanel(ModelContextService context, AgentClient agent)
        {
            _context = context;
            _agent = agent;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 18, 18);
            ForeColor = Color.Gainsboro;

            var title = new Label
            {
                Text = "Mechra",
                Dock = DockStyle.Top,
                Height = 42,
                Padding = new Padding(12, 12, 0, 0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White
            };

            _check = CreateButton("Check model");
            _check.Dock = DockStyle.Top;
            _check.Height = 36;
            _check.Click += (_, __) => ShowModelContext();

            _conversation = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9.5F),
                Text = "Mechra v0.1.0 ready. Open a SOLIDWORKS document and press Check model.\n\n"
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(8) };
            _input = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F)
            };
            _send = CreateButton("Send");
            _send.Dock = DockStyle.Right;
            _send.Width = 72;
            _send.Click += async (_, __) => await SendAsync();
            bottom.Controls.Add(_input);
            bottom.Controls.Add(_send);

            Controls.Add(_conversation);
            Controls.Add(bottom);
            Controls.Add(_check);
            Controls.Add(title);
        }

        private static Button CreateButton(string text) => new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(38, 38, 38),
            ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Segoe UI", 9F)
        };

        private void ShowModelContext()
        {
            var ctx = _context.Capture();
            Append("SYSTEM", ctx.document_type == "none"
                ? "No active document."
                : $"{ctx.document_type}: {ctx.document_title} — {ctx.features.Count} top-level features");
        }

        private async Task SendAsync()
        {
            var text = _input.Text.Trim();
            if (text.Length == 0) return;
            _input.Clear();
            Append("YOU", text);
            _send.Enabled = false;
            try
            {
                var result = await _agent.ChatAsync(text, _context.Capture());
                if (IsHandleCreated) BeginInvoke(new Action(() => Append("AGENT", result)));
            }
            catch (Exception ex)
            {
                if (IsHandleCreated) BeginInvoke(new Action(() => Append("ERROR", ex.Message)));
            }
            finally
            {
                if (IsHandleCreated) BeginInvoke(new Action(() => _send.Enabled = true));
            }
        }

        private void Append(string role, string text)
        {
            _conversation.AppendText($"{role}\n{text}\n\n");
            _conversation.ScrollToCaret();
        }
    }
}
