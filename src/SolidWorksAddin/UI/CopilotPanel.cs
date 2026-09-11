using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SwCursor.SolidWorksAddin.Services;

namespace SwCursor.SolidWorksAddin.UI
{
    public sealed class CopilotPanel : UserControl
    {
        private const string Release = "0.2.0-dev.5";
        private readonly ModelContextService _context;
        private readonly AgentClient _agent;
        private readonly CadExecutor _executor;
        private readonly FlowLayoutPanel _feed;
        private readonly TextBox _input;
        private readonly Label _status, _document, _placeholder;
        private readonly ProductButton _send, _check, _reset, _export;
        private readonly ToolTip _tips = new ToolTip();
        private readonly StringBuilder _log = new StringBuilder();
        private ProductButton _apply, _cancel;
        private Label _planState;
        private StackCard _welcome;
        private CadPlan _pending;
        private ModelContextSnapshot _reviewedContext;
        private bool _busy, _sizing;

        public CopilotPanel(ModelContextService context, AgentClient agent, CadExecutor executor)
        {
            _context = context; _agent = agent; _executor = executor;
            Dock = DockStyle.Fill; AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            BackColor = ProductTheme.Canvas; ForeColor = ProductTheme.Text;
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(12, 0, 12, 0);

            var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 114, ColumnCount = 3,
                RowCount = 3, Padding = new Padding(0, 14, 0, 8) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            header.Controls.Add(new WireMark { Dock = DockStyle.Fill }, 0, 0);
            var brand = ProductTheme.Label("Mechra", 17, ProductTheme.Text, true); brand.Margin = new Padding(4, 0, 0, 0);
            header.Controls.Add(brand, 1, 0);
            _reset = new ProductButton("+", 34) { Dock = DockStyle.Fill, Margin = new Padding(2, 0, 0, 4), AccessibleName = "Cuộc trò chuyện mới" };
            _reset.Click += (_, __) => ResetChat(); _tips.SetToolTip(_reset, "Cuộc trò chuyện mới");
            header.Controls.Add(_reset, 2, 0);
            var mode = ProductTheme.Label("NATIVE CAD   /   LOCAL PLANNER", 7.8F, ProductTheme.Muted);
            mode.Margin = new Padding(4, 0, 0, 0); header.Controls.Add(mode, 1, 1); header.SetColumnSpan(mode, 2);
            _document = ProductTheme.Label("Chưa kiểm tra Part", 9, ProductTheme.Muted);
            _document.AutoSize = false; _document.AutoEllipsis = true; _document.TextAlign = ContentAlignment.MiddleLeft;
            _document.Margin = new Padding(0, 4, 0, 0);
            header.Controls.Add(_document, 0, 2); header.SetColumnSpan(_document, 3);
            _tips.SetToolTip(_document, "Tài liệu tại lần kiểm tra gần nhất. Mechra kiểm tra lại trước khi thực thi.");

            var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 181, ColumnCount = 1, RowCount = 3 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            _check = new ProductButton("Check Part", 102); _check.Click += (_, __) => ShowModelContext();
            _export = new ProductButton("Save log", 88); _export.Click += (_, __) => ExportLog();
            toolbar.Controls.AddRange(new Control[] { _check, _export }); bottom.Controls.Add(toolbar, 0, 0);

            var composer = new StackCard { Dock = DockStyle.Fill, AutoSize = false, Margin = Padding.Empty, Padding = new Padding(12, 10, 12, 8) };
            var editor = new Panel { Dock = DockStyle.Fill, Height = 45, Margin = Padding.Empty };
            _input = new TextBox { Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true,
                BackColor = ProductTheme.Surface, ForeColor = ProductTheme.Text, BorderStyle = BorderStyle.None,
                MaxLength = 4000, ScrollBars = ScrollBars.Vertical, AccessibleName = "Yêu cầu tạo hoặc sửa plate, đơn vị mm" };
            _placeholder = ProductTheme.Label("Mô tả plate bạn muốn tạo...", 9.5F, ProductTheme.Muted);
            _placeholder.Dock = DockStyle.Top; _placeholder.Cursor = Cursors.IBeam; _placeholder.Click += (_, __) => _input.Focus();
            editor.Controls.Add(_input); editor.Controls.Add(_placeholder);
            _input.TextChanged += (_, __) => { _placeholder.Visible = _input.TextLength == 0; if (_send != null) _send.Enabled = !_busy && _input.Text.Trim().Length > 0; };
            _input.KeyDown += async (_, e) => {
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; await SendAsync(); }
            };
            composer.Add(editor);
            var composerActions = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 32, ColumnCount = 2, Margin = Padding.Empty };
            composerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            composerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
            var hint = ProductTheme.Label("Shift + Enter xuống dòng", 7.5F, ProductTheme.Muted); hint.Margin = new Padding(0, 8, 0, 0);
            composerActions.Controls.Add(hint, 0, 0);
            _send = new ProductButton("Gửi ↑", 68, true) { Dock = DockStyle.Fill, Enabled = false, Margin = Padding.Empty };
            _send.Click += async (_, __) => await SendAsync(); composerActions.Controls.Add(_send, 1, 0);
            composer.Add(composerActions); bottom.Controls.Add(composer, 0, 1);
            _status = ProductTheme.Label("Sẵn sàng  ·  " + Release, 7.8F, ProductTheme.Muted);
            _status.AutoSize = false; _status.AutoEllipsis = true; _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Margin = Padding.Empty; bottom.Controls.Add(_status, 0, 2);

            _feed = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true, Padding = new Padding(0, 8, 0, 4), BackColor = ProductTheme.Canvas };
            _feed.SizeChanged += (_, __) => ResizeCards();
            Controls.Add(_feed); Controls.Add(bottom); Controls.Add(header);
            Welcome();
        }

        private int Px(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
        private void ResizeCards()
        {
            if (_sizing || IsDisposed) return;
            _sizing = true;
            try {
                int width = Math.Max(Px(150), _feed.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - Px(2));
                foreach (Control card in _feed.Controls)
                { card.MaximumSize = new Size(width, 0); card.MinimumSize = new Size(width, 0); card.Width = width; }
            } finally { _sizing = false; }
        }
        private void AddCard(StackCard card)
        {
            _feed.Controls.Add(card); ResizeCards();
            _feed.PerformLayout(); _feed.ScrollControlIntoView(card);
        }
        private StackCard Card(string eyebrow, Color? accent = null)
        {
            var card = new StackCard { Padding = new Padding(Px(14)), Margin = new Padding(0, 0, 0, Px(12)) };
            card.Add(ProductTheme.Label(eyebrow, 8, accent ?? ProductTheme.Muted, true)); return card;
        }
        private void Welcome()
        {
            _welcome = Card("THIẾT KẾ CÙNG MECHRA", ProductTheme.Accent);
            _welcome.Add(new WireMark { Size = new Size(Px(58), Px(58)), Margin = new Padding(0, Px(8), 0, Px(14)) });
            _welcome.Add(ProductTheme.Label("Từ ý tưởng\nđến Part native.", 20, ProductTheme.Text, true));
            _welcome.Add(ProductTheme.Label("Tạo plate có kích thước điều khiển, sửa chiều dày và kiểm chứng sau rebuild.", 10, ProductTheme.Muted));
            _welcome.Add(ProductTheme.Label("01  Mô tả    →    02  Xem kế hoạch    →    03  Áp dụng", 8, ProductTheme.Accent));
            var create = new ProductButton("Tạo plate 100 × 60 × 5 mm", 250) { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 8) };
            create.Click += (_, __) => Draft("Tạo plate 100 x 60 x 5 mm"); _welcome.Add(create);
            var edit = new ProductButton("Đổi chiều dày thành 8 mm", 250) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
            edit.Click += (_, __) => Draft("Đổi chiều dày thành 8 mm"); _welcome.Add(edit);
            _welcome.Add(ProductTheme.Label("Bắt đầu với Part trống. Bản này dùng bộ lập kế hoạch cục bộ; chưa kết nối mô hình AI.", 8.5F, ProductTheme.Muted));
            AddCard(_welcome); Log("SESSION", "Mechra " + Release + " | Native CAD / deterministic local planner");
        }
        private void Draft(string text) { if (_busy) return; _input.Text = text; _input.Focus(); _input.SelectionStart = text.Length; }
        private void RemoveWelcome()
        {
            if (_welcome == null) return;
            _feed.Controls.Remove(_welcome); _welcome.Dispose(); _welcome = null;
        }
        private void Message(string role, string text, bool problem = false)
        {
            if (IsDisposed || Disposing) return;
            Log(role, text);
            var card = Card(role, problem ? ProductTheme.Warning : role == "BẠN" ? ProductTheme.Muted : ProductTheme.Accent);
            if (role == "BẠN") card.BackColor = ProductTheme.Raised;
            card.Add(ProductTheme.Label(text)); AddCard(card);
        }
        private void Log(string role, string text) => _log.AppendLine(DateTime.Now.ToString("O") + " | " + role).AppendLine(text).AppendLine();
        private void ContextLabel(ModelContextSnapshot c)
        {
            _document.Text = string.IsNullOrWhiteSpace(c.document_title) ? "Chưa có Part đang mở" : c.document_title + "  /  " + c.configuration;
        }

        private async Task SendAsync()
        {
            string text = _input.Text.Trim();
            if (_busy || text.Length == 0) return;
            ClearPlan("Đã thay bằng yêu cầu mới"); RemoveWelcome(); _input.Clear(); Message("BẠN", text);
            SetBusy(true); Status("Đang lập kế hoạch...");
            try {
                ModelContextSnapshot before = _context.Capture(); ContextLabel(before);
                AgentReply reply = await _agent.ChatAsync(text, before);
                if (IsDisposed || Disposing) return;
                if (reply.action == "execute_cad_plan")
                {
                    string error = CadValidation.ValidatePlan(reply.plan);
                    if (error != null || !reply.requires_confirmation) throw new InvalidOperationException(error ?? "Plan review is required.");
                    if (!ModelRevision.Same(before, _context.Capture()))
                    { Status("Part đã thay đổi"); Message("CẦN LẬP LẠI KẾ HOẠCH", "Part đã thay đổi trong lúc lập kế hoạch. Hãy gửi lại yêu cầu.", true); return; }
                    CadOperation op = reply.plan.operations[0];
                    CadReadinessReport readiness = _executor.CheckPart();
                    string issue = op.kind == "create_plate" ? readiness.create_issue : readiness.edit_issue;
                    if (issue != null) { Message("KIỂM TRA PART", issue, true); Status("Part chưa đáp ứng điều kiện"); return; }
                    _pending = reply.plan; _reviewedContext = before;
                    ShowPlan(reply.plan, before, reply.message); Status("Kế hoạch sẵn sàng · Kiểm tra thông số trước khi áp dụng");
                }
                else { Message("MECHRA", reply.message ?? string.Empty); Status("Đang chờ yêu cầu tiếp theo"); }
            } catch (Exception ex) { if (!IsDisposed && !Disposing) { Message("YÊU CẦU CHƯA HOÀN TẤT", ex.Message, true); Status("Kiểm tra dịch vụ Mechra cục bộ"); } }
            finally { if (!IsDisposed && !Disposing) SetBusy(false); }
        }

        private static string Number(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.###", CultureInfo.InvariantCulture);
        private Control Metrics(string[] labels, string[] values)
        {
            var grid = new TableLayoutPanel { ColumnCount = labels.Length, RowCount = 2, Dock = DockStyle.Fill,
                AutoSize = true, Margin = new Padding(0, Px(5), 0, Px(10)), BackColor = ProductTheme.Raised, Padding = new Padding(Px(9)) };
            for (int i = 0; i < labels.Length; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / labels.Length));
                var key = ProductTheme.Label(labels[i], 7.5F, ProductTheme.Muted); key.Margin = new Padding(0, 0, Px(4), Px(4));
                var value = ProductTheme.Label(values[i], 12, ProductTheme.Text, true); value.Margin = new Padding(0, 0, Px(4), 0);
                grid.Controls.Add(key, i, 0); grid.Controls.Add(value, i, 1);
            }
            return grid;
        }
        private void ShowPlan(CadPlan plan, ModelContextSnapshot before, string explanation)
        {
            CadOperation op = plan.operations[0]; bool create = op.kind == "create_plate";
            var card = Card("CAD PLAN  /  01 OPERATION", ProductTheme.Accent);
            card.Add(ProductTheme.Label(create ? "Tạo plate native" : "Cập nhật chiều dày", 14, ProductTheme.Text, true));
            card.Add(ProductTheme.Label(before.document_title + "  ·  " + before.configuration, 8.5F, ProductTheme.Muted));
            if (create) card.Add(Metrics(new[] { "RỘNG · mm", "CAO · mm", "DÀY · mm" },
                new[] { Number(op.inputs["width_mm"]), Number(op.inputs["height_mm"]), Number(op.inputs["thickness_mm"]) }));
            else card.Add(Metrics(new[] { "CHIỀU DÀY MỚI · mm" }, new[] { Number(op.inputs["thickness_mm"]) }));
            card.Add(ProductTheme.Label(create ? "Sketch chữ nhật trên mặt phẳng tham chiếu đầu tiên. Extrude theo chiều dày đã chọn."
                : "Sửa extrusion của plate Mechra hiện tại. Giữ nguyên chiều rộng và chiều cao.", 9, ProductTheme.Muted));
            card.Add(ProductTheme.Label("Đơn vị: mm. Giá trị không ghi đơn vị được hiểu là mm.", 8.5F, ProductTheme.Muted));
            if (!string.IsNullOrWhiteSpace(explanation)) Log("PLANNER", explanation);
            card.Add(ProductTheme.Label("Tự động rebuild và đối chiếu kích thước, thể tích sau khi thực thi.", 9, ProductTheme.Muted));
            _planState = ProductTheme.Label("CHỜ BẠN XÁC NHẬN", 8, ProductTheme.Accent, true); card.Add(_planState);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty };
            _apply = new ProductButton("Áp dụng kế hoạch", Px(151), true) { Height = Px(36) }; _apply.Click += (_, __) => ApplyPlan();
            _cancel = new ProductButton("Hủy", Px(61)) { Height = Px(36) };
            _cancel.Click += (_, __) => { ClearPlan("Đã hủy"); Status("Đã hủy kế hoạch"); };
            actions.Controls.AddRange(new Control[] { _apply, _cancel }); card.Add(actions);
            Log("PLAN", plan.summary + "\nPart: " + before.document_title + "\n" + string.Join("\n", op.inputs.Select(p => p.Key + ": " + Convert.ToString(p.Value, CultureInfo.InvariantCulture))));
            AddCard(card);
        }
        private void ApplyPlan()
        {
            if (_busy || _pending == null) return;
            CadPlan plan = _pending; ModelContextSnapshot before = _reviewedContext;
            ClearPlan("Đã gửi thực thi"); SetBusy(true); Status("Đang thực thi CAD...");
            try {
                // Keep every COM mutation on the SOLIDWORKS UI thread; never pump messages here.
                CadExecutionResult result = _executor.Execute(plan, before, Status);
                ShowResult(result); ContextLabel(_context.Capture());
                Status(result.success ? "Đã kiểm chứng · Part native có thể chỉnh sửa"
                    : result.rollback_verified ? "Thao tác thất bại · Đã kiểm chứng khôi phục" : "Đã dừng · Xem chi tiết kết quả");
            } catch (Exception ex) { Message("THAO TÁC ĐÃ DỪNG", ex.Message, true); Status("Kiểm tra Part và log thực thi"); }
            finally { SetBusy(false); }
        }
        private void ShowResult(CadExecutionResult result)
        {
            string details = "Duration: " + result.elapsed_ms + " ms\nFailure phase: " + (result.failure_phase ?? "none")
                + "\nRollback verified: " + result.rollback_verified + "\n" + result.message
                + "\n\nTRACE\n" + string.Join("\n", result.trace ?? new System.Collections.Generic.List<string>())
                + "\n\nSTEPS\n" + string.Join("\n", result.steps ?? new System.Collections.Generic.List<string>())
                + "\n\nCHECKS\n" + string.Join("\n", result.local_verification == null ? new string[0] : result.local_verification.checks.ToArray());
            Log(result.success ? "VERIFIED" : "FAILED", details);
            var card = Card(result.success ? "VERIFIED  /  ĐÃ KIỂM CHỨNG" : "STOPPED  /  THAO TÁC ĐÃ DỪNG", result.success ? ProductTheme.Success : ProductTheme.Warning);
            card.LineColor = result.success ? Color.FromArgb(40, 80, 64) : Color.FromArgb(90, 62, 45);
            card.Add(ProductTheme.Label(result.success ? "Part đã sẵn sàng." : "Cần kiểm tra kết quả", 14, ProductTheme.Text, true));
            if (result.verification != null)
            {
                var v = result.verification;
                card.Add(ProductTheme.Label(result.success ? "KÍCH THƯỚC ĐO LẠI" : "SỐ ĐO TRƯỚC KHI KẾT THÚC / KHÔI PHỤC", 7.5F, ProductTheme.Muted));
                card.Add(Metrics(new[] { "RỘNG · mm", "CAO · mm", "DÀY · mm" }, new[] {
                    Number(v.measured_width_mm), Number(v.measured_height_mm), Number(v.measured_thickness_mm) }));
                card.Add(ProductTheme.Label("Thể tích: " + Number(v.measured_volume_mm3) + " mm³", 9.5F));
            }
            card.Add(ProductTheme.Label(result.success ? "Rebuild thành công. Kích thước và thể tích nằm trong dung sai kiểm chứng."
                : result.message, 9, result.success ? ProductTheme.Muted : ProductTheme.Warning));
            if (!result.success && result.failure_phase != null) card.Add(ProductTheme.Label("Bước dừng: " + result.failure_phase, 8.5F, ProductTheme.Warning));
            AddDetails(card, "Chi tiết kỹ thuật", details); AddCard(card);
        }
        private void AddDetails(StackCard card, string title, string details)
        {
            var text = ProductTheme.Label(details, 8.5F, ProductTheme.Muted); text.Visible = false;
            var toggle = new ProductButton("+ " + title, Px(170)) { Height = Px(32), Margin = new Padding(0, Px(5), 0, Px(8)) };
            bool expanded = false;
            toggle.Click += (_, __) => { expanded = !expanded; text.Visible = expanded; toggle.Text = (expanded ? "− " : "+ ") + title; ResizeCards(); };
            card.Add(toggle); card.Add(text);
        }
        private void ClearPlan(string state)
        {
            _pending = null; _reviewedContext = null;
            if (_apply != null && !_apply.IsDisposed) _apply.Enabled = false;
            if (_cancel != null && !_cancel.IsDisposed) _cancel.Enabled = false;
            if (_planState != null && !_planState.IsDisposed) { _planState.Text = state; _planState.ForeColor = ProductTheme.Muted; Log("PLAN STATE", state); }
            _apply = _cancel = null; _planState = null;
        }
        private void ResetChat()
        {
            if (_busy) return;
            ClearPlan("Cuộc trò chuyện mới"); _agent.ResetSession();
            // Keep diagnostic history for export until this pane is closed.
            while (_feed.Controls.Count > 0) { Control card = _feed.Controls[0]; _feed.Controls.Remove(card); card.Dispose(); }
            _welcome = null; _input.Clear(); Welcome(); Status("Cuộc trò chuyện mới · " + Release);
        }
        private void ShowModelContext()
        {
            if (_busy) return;
            ClearPlan("Cần lập lại sau khi kiểm tra Part"); RemoveWelcome(); SetBusy(true); Status("Đang kiểm tra Part...");
            try {
                var c = _context.Capture(); ContextLabel(c); CadReadinessReport report = _executor.CheckPart();
                var card = Card("PART CHECK", ProductTheme.Accent);
                card.Add(ProductTheme.Label(c.document_title ?? "Chưa có tài liệu", 13, ProductTheme.Text, true));
                card.Add(ProductTheme.Label(report.can_create ? "Sẵn sàng tạo plate mới." : report.can_edit ? "Sẵn sàng sửa chiều dày plate Mechra." : "Part chưa đáp ứng điều kiện.", 10,
                    report.can_create || report.can_edit ? ProductTheme.Success : ProductTheme.Warning));
                string details = c.document_type + " / " + c.configuration + "\nSolid bodies: " + report.solid_bodies + " | Surface bodies: " + report.surface_bodies
                    + "\nTạo plate: " + (report.create_issue ?? "Sẵn sàng") + "\nSửa chiều dày: " + (report.edit_issue ?? "Sẵn sàng")
                    + "\n\nFEATURES\n" + string.Join("\n", c.features.Select(f => f.name + " [" + (f.type_name ?? "unknown") + "]"));
                if (!report.can_create && !report.can_edit) card.Add(ProductTheme.Label(report.create_issue, 9, ProductTheme.Muted));
                AddDetails(card, "Chi tiết Part", details); AddCard(card); Log("PART CHECK", details);
                Status(report.can_create || report.can_edit ? "Part đã kiểm tra · Hãy gửi yêu cầu" : "Part chưa đáp ứng điều kiện");
            } catch (Exception ex) { Message("KIỂM TRA PART THẤT BẠI", ex.Message, true); Status("Không đọc được Part"); }
            finally { SetBusy(false); }
        }
        private void ExportLog()
        {
            if (_busy) return;
            using (var dialog = new SaveFileDialog { Filter = "Text log (*.txt)|*.txt", FileName = "Mechra-session-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try { File.WriteAllText(dialog.FileName, "Mechra " + Release + "\r\nAssembly: " + typeof(CopilotPanel).Assembly.GetName().Version
                        + "\r\nLoaded DLL: " + typeof(CopilotPanel).Assembly.Location + "\r\n\r\n" + _log, Encoding.UTF8); Status("Đã lưu log phiên làm việc"); }
                    catch (Exception ex) { Message("KHÔNG LƯU ĐƯỢC LOG", ex.Message, true); }
        }
        private void SetBusy(bool busy)
        {
            _busy = busy;
            _input.Enabled = _check.Enabled = _reset.Enabled = _export.Enabled = !busy;
            _send.Enabled = !busy && _input.Text.Trim().Length > 0;
            if (_apply != null) _apply.Enabled = !busy && _pending != null;
            if (_cancel != null) _cancel.Enabled = !busy && _pending != null;
        }
        private void Status(string text)
        { if (!IsDisposed && !Disposing) { _status.Text = text; _tips.SetToolTip(_status, text); _status.Refresh(); } }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _tips.Dispose(); Font.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
