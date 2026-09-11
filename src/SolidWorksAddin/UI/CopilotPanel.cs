using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SwCursor.SolidWorksAddin.Services;

namespace SwCursor.SolidWorksAddin.UI
{
    public sealed class CopilotPanel : UserControl
    {
        private readonly ModelContextService _context;
        private readonly AgentClient _agent;
        private readonly CadExecutor _executor;
        private readonly RichTextBox _conversation;
        private readonly TextBox _input;
        private readonly Label _status;
        private readonly TextBox _planText;
        private readonly Panel _planPanel;
        private readonly Button _send, _check, _apply, _cancel, _reset, _export;
        private CadPlan _pending;
        private ModelContextSnapshot _reviewedContext;
        private bool _busy;

        public CopilotPanel(ModelContextService context, AgentClient agent, CadExecutor executor)
        {
            _context = context; _agent = agent; _executor = executor;
            Dock = DockStyle.Fill;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(17, 18, 20);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9F);
            var title = new Label { Text = "Mechra  /  Native CAD", Dock = DockStyle.Top, Height = 44,
                Padding = new Padding(12, 12, 0, 0), ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            _status = new Label { Text = "Ready - v0.2.0-dev.2", Dock = DockStyle.Top, Height = 30,
                Padding = new Padding(12, 5, 4, 0), ForeColor = Color.FromArgb(125, 206, 171) };
            var tools = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6), WrapContents = true };
            _check = MakeButton("Model", 65); _check.Click += (_, __) => ShowModelContext();
            _reset = MakeButton("New chat", 78); _reset.Click += (_, __) => ResetChat();
            _export = MakeButton("Save log", 76); _export.Click += (_, __) => ExportLog();
            tools.Controls.AddRange(new Control[] { _check, _reset, _export });
            _conversation = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(22, 23, 26), ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9.5F), DetectUrls = false, ScrollBars = RichTextBoxScrollBars.Vertical };

            _planPanel = new Panel { Dock = DockStyle.Bottom, Height = 195, Padding = new Padding(10),
                BackColor = Color.FromArgb(29, 32, 37), Visible = false };
            _planText = new TextBox { Dock = DockStyle.Fill, ForeColor = Color.White, ReadOnly = true, Multiline = true,
                BackColor = Color.FromArgb(29, 32, 37), BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical };
            var planActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, WrapContents = false };
            _apply = MakeButton("Apply plan", 106); _apply.BackColor = Color.FromArgb(39, 99, 77);
            _apply.Click += (_, __) => ApplyPlan();
            _cancel = MakeButton("Cancel", 74); _cancel.Click += (_, __) => { ClearPlan(); Status("Plan cancelled"); };
            planActions.Controls.AddRange(new Control[] { _apply, _cancel });
            _planPanel.Controls.Add(_planText); _planPanel.Controls.Add(planActions);

            var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 102, Padding = new Padding(8) };
            _input = new TextBox { Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true,
                BackColor = Color.FromArgb(31, 33, 37), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10F), MaxLength = 4000 };
            _input.KeyDown += async (_, e) => {
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; await SendAsync(); }
            };
            _send = MakeButton("Send", 65); _send.Dock = DockStyle.Right;
            _send.Click += async (_, __) => await SendAsync();
            inputPanel.Controls.Add(_input); inputPanel.Controls.Add(_send);
            Controls.Add(_conversation); Controls.Add(_planPanel); Controls.Add(inputPanel);
            Controls.Add(tools); Controls.Add(_status); Controls.Add(title);
            Welcome();
        }

        private static Button MakeButton(string text, int width) => new Button { Text = text, Width = width, Height = 28,
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(43, 45, 50), ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 }, Margin = new Padding(3), UseVisualStyleBackColor = false };

        private void Welcome()
        {
            Append("MECHRA", "Tạo và sửa plate native trong SOLIDWORKS.\n"
                + "1. Mở Part trống.\n2. Nhập: Tạo plate 100 x 60 x 5 mm.\n"
                + "3. Xem kế hoạch và bấm Apply plan.\n4. Nhập: Đổi chiều dày thành 8 mm.\n\n"
                + "v0.2 dùng bộ lập kế hoạch xác định; chưa kết nối mô hình AI. Enter để gửi, Shift+Enter để xuống dòng.");
        }

        private async Task SendAsync()
        {
            string text = _input.Text.Trim();
            if (_busy || text.Length == 0) return;
            ClearPlan(); _input.Clear(); Append("YOU", text); SetBusy(true); Status("Analyzing request...");
            try {
                ModelContextSnapshot before = _context.Capture();
                AgentReply reply = await _agent.ChatAsync(text, before);
                if (IsDisposed || Disposing) return;
                Append("MECHRA", reply.message ?? string.Empty);
                if (reply.action == "execute_cad_plan")
                {
                    string error = CadValidation.ValidatePlan(reply.plan);
                    if (error != null || !reply.requires_confirmation) throw new InvalidOperationException(error ?? "Plan review is required.");
                    if (!ModelRevision.Same(before, _context.Capture()))
                    { Status("Model changed - send the request again"); Append("MODEL", "Part đã thay đổi trong lúc lập kế hoạch. Hãy gửi lại yêu cầu."); return; }
                    _pending = reply.plan; _reviewedContext = before;
                    CadOperation op = reply.plan.operations[0];
                    string dimensions = op.kind == "create_plate"
                        ? string.Format(CultureInfo.InvariantCulture, "Rộng: {0} mm | Cao: {1} mm | Dày: {2} mm\r\nMặt phẳng: tham chiếu đầu tiên", op.inputs["width_mm"], op.inputs["height_mm"], op.inputs["thickness_mm"])
                        : string.Format(CultureInfo.InvariantCulture, "Chiều dày mới: {0} mm\r\nGiữ chiều dài và chiều rộng hiện tại.", op.inputs["thickness_mm"]);
                    _planText.Text = "REVIEW PLAN\r\n" + dimensions + "\r\nPart: " + before.document_title
                        + "\r\n" + reply.message;
                    Append("PLAN", reply.plan.summary + "\n" + dimensions);
                    _planPanel.Visible = true; Status("Plan ready - review dimensions and apply");
                }
                else Status("Waiting for your next message");
            } catch (Exception ex) { Append("ERROR", ex.Message); Status("Request failed - check the local agent"); }
            finally { if (!IsDisposed && !Disposing) SetBusy(false); }
        }

        private void ApplyPlan()
        {
            if (_busy || _pending == null) return;
            CadPlan plan = _pending; ModelContextSnapshot before = _reviewedContext;
            ClearPlan(); SetBusy(true); Status("Executing / rebuilding / verifying...");
            try {
                // Mutations and local verification finish synchronously; no COM work runs in Task.Run.
                CadExecutionResult result = _executor.Execute(plan, before);
                Append(result.success ? "VERIFIED" : "FAILED", result.message);
                if (result.steps != null && result.steps.Count > 0) Append("STEPS", string.Join("\n", result.steps));
                if (result.local_verification != null) Append("MEASUREMENTS", string.Join("\n", result.local_verification.checks));
                Status(result.success ? "Verified - native Part is editable" : result.rollback_verified ? "Failed - rollback verified" : "Stopped - inspect the message");
            } catch (Exception ex) { Append("ERROR", ex.Message); Status("Stopped - inspect the Part"); }
            finally { SetBusy(false); }
        }

        private void ClearPlan() { _pending = null; _reviewedContext = null; _planPanel.Visible = false; }
        private void ResetChat() { ClearPlan(); _agent.ResetSession(); _conversation.Clear(); _input.Clear(); Welcome(); Status("New conversation ready"); }
        private void ShowModelContext()
        {
            try { var c = _context.Capture(); Append("MODEL", c.document_type == "none" ? "Chưa có tài liệu đang mở."
                : c.document_title + " / " + c.document_type + " / " + c.configuration + "\n" + string.Join("\n", c.features.Select(f => f.name + " (" + f.type_name + ")"))); }
            catch (Exception ex) { Append("ERROR", ex.Message); }
        }
        private void ExportLog()
        {
            using (var dialog = new SaveFileDialog { Filter = "Text log (*.txt)|*.txt", FileName = "Mechra-session.txt" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try { File.WriteAllText(dialog.FileName, _conversation.Text, System.Text.Encoding.UTF8); }
                    catch (Exception ex) { Append("ERROR", ex.Message); }
        }
        private void SetBusy(bool busy)
        {
            _busy = busy;
            _send.Enabled = _input.Enabled = _check.Enabled = _reset.Enabled = _export.Enabled = !busy;
            _apply.Enabled = _cancel.Enabled = !busy;
        }
        private void Status(string text) { if (!IsDisposed) { _status.Text = text; _status.Refresh(); } }
        private void Append(string role, string text)
        {
            if (IsDisposed || Disposing) return;
            _conversation.SelectionStart = _conversation.TextLength;
            _conversation.SelectionColor = role == "ERROR" || role == "FAILED" ? Color.Salmon : Color.FromArgb(125, 206, 171);
            _conversation.AppendText(role + "\n");
            _conversation.SelectionColor = Color.Gainsboro;
            _conversation.AppendText(text + "\n\n"); _conversation.ScrollToCaret();
        }
    }
}
