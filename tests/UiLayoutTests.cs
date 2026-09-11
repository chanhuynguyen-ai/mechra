using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SwCursor.SolidWorksAddin.UI;

[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]

internal static class UiLayoutTests
{
    private static int _checks;
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window, int index);
    private static void Check(bool condition, string name)
    { _checks++; if (!condition) throw new Exception(name); }
    private static StackCard Card(string title, string body)
    {
        var c = new StackCard();
        c.Add(ProductTheme.Label(title, 14, ProductTheme.Text, true));
        c.Add(ProductTheme.Label(body));
        return c;
    }
    private static void CheckRegions()
    {
        foreach (int dpi in new[] { 96, 144, 192 })
        foreach (int width in new[] { 1, 200, 320, 380, 520, 800 })
        foreach (int height in new[] { 0, 80, 320, 480, 760, 1100 })
        {
            var host = new Size(width, height);
            var r = PaneRegions.Calculate(host, dpi, (int)(128 * dpi / 96F));
            var parts = new[] { r.Header, r.Feed, r.Toolbar, r.Composer, r.Status };
            for (int i = 0; i < parts.Length; i++)
            {
                Rectangle part = parts[i];
                Check(part.Left >= 0 && part.Top >= 0 && part.Right <= width && part.Bottom <= height,
                    "pane region exceeds host at " + width + "x" + height + " / " + dpi);
                if (i > 0) Check(parts[i-1].Bottom <= part.Top, "pane regions overlap");
            }
        }
    }
    [STAThread]
    public static int Main(string[] args)
    {
        try {
            Application.EnableVisualStyles();
            CheckRegions();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var font = new Font("Segoe UI", 9F))
            using (var form = new Form { Font = font, BackColor = ProductTheme.Canvas, ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) })
            using (var feed = new ConversationViewport())
            using (var composer = new PromptComposer())
            {
                form.Controls.Add(feed); form.Controls.Add(composer); form.Show();
                int dpi = form.DeviceDpi;
                foreach (int logicalWidth in new[] { 320, 380, 520 })
                foreach (int logicalHeight in new[] { 480, 760 })
                {
                    form.ClientSize = new Size(logicalWidth * dpi / 96, logicalHeight * dpi / 96);
                    Action layout = () => {
                        PaneRegions regions = PaneRegions.Calculate(form.ClientSize, dpi, composer.DesiredHeight);
                        composer.Bounds = regions.Composer; feed.Bounds = regions.Feed;
                        composer.PerformLayout(); feed.Reflow();
                    };
                    layout(); layout();
                    foreach (string input in new[] { "", "Tạo plate 100 x 60 x 5 mm", string.Join("\r\n", Enumerable.Repeat("Một dòng nhập dài để kiểm tra ô soạn lệnh và nút gửi.", 12)) })
                    {
                        composer.Editor.Text = input; layout(); layout();
                        Check(composer.Editor.Bottom < composer.SendButton.Top, "editor overlaps Send");
                        Check(composer.SendButton.Bottom <= composer.ClientSize.Height, "Send clipped below composer");
                        Check(composer.SendButton.Right <= composer.ClientSize.Width, "Send clipped at right edge");
                        Check((GetWindowLong(composer.Editor.Handle, -16) & 0x00200000) == 0, "native white editor scrollbar returned");
                    }
                    composer.Editor.Clear(); layout(); layout();
                    feed.ClearCards();
                    string drafted = null;
                    var welcome = WelcomeCardFactory.Create(value => drafted = value, dpi);
                    feed.AddCard(welcome, true); feed.Reflow();
                    Check(welcome.Right <= feed.ClientSize.Width, "welcome card exceeds viewport");
                    if (logicalHeight >= 760) Check(welcome.Height < feed.ClientSize.Height, "compact welcome requires scrolling in a tall pane");
                    welcome.Controls.OfType<ProductButton>().First().PerformClick();
                    Check(drafted == "Tạo plate 100 x 60 x 5 mm", "starter prompt did not populate a draft");
                    foreach (Control child in welcome.Controls)
                        Check(child.Bottom <= welcome.Height, "welcome child clipped");
                    if (args.Length > 0)
                    {
                        Directory.CreateDirectory(args[0]);
                        using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                        { form.DrawToBitmap(bitmap, form.ClientRectangle); bitmap.Save(Path.Combine(args[0], "ui-" + logicalWidth + "x" + logicalHeight + "-dpi" + dpi + ".png"), ImageFormat.Png); }
                    }
                    for (int i = 0; i < 6; i++) feed.AddCard(Card("MECHRA " + i, new string('x', 220)));
                    feed.Reflow(); feed.SetOffset(0);
                    int before = feed.ScrollOffset;
                    feed.AddCard(Card("MECHRA", "Tin trả lời khi người dùng đang đọc lịch sử."));
                    Check(feed.ScrollOffset == before, "new reply moved historical reading position");
                    Check(feed.HasUnread, "unread reply is not discoverable");
                    feed.SetOffset(int.MaxValue);
                    Check(feed.ScrollOffset == feed.MaximumOffset, "scroll not clamped at end");
                    Check(!feed.HasUnread, "unread marker remains at end");
                    Check((GetWindowLong(feed.Handle, -16) & 0x00300000) == 0, "native viewport scrollbar returned");
                    feed.SetOffset(0); int firstTop = feed.Cards[0].Top;
                    var detail = new DetailPreview { Text = new string('a', 4000), Dock = DockStyle.Fill };
                    feed.Cards[1].Add(detail); feed.Reflow();
                    Check(feed.Cards[0].Top == firstTop, "expanding details moved reading anchor");
                    Check(detail.GetPreferredSize(new Size(260, 0)).Height <= (int)(136 * dpi / 96F) + 1, "detail preview is unbounded");
                    int previousBottom = int.MinValue;
                    foreach (var card in feed.Cards)
                    {
                        Check(card.Left >= 0 && card.Right <= feed.ClientSize.Width, "card width overflow");
                        Check(card.Top >= previousBottom, "conversation cards overlap"); previousBottom = card.Bottom;
                    }

                }
                feed.ClearCards(); Check(feed.ScrollOffset == 0 && feed.MaximumOffset == 0, "clear left stale scrolling");
                form.Close();
            }
            Console.WriteLine("PASS: " + _checks + " UI region, native layout, composer, scroll and detail checks.");
            Console.WriteLine("Native controls tested at the current Windows DPI; 96/144/192 region calculations are synthetic. SOLIDWORKS hosting remains a separate gate.");
            return 0;
        } catch (Exception ex) { Console.Error.WriteLine("FAIL UI: " + ex); return 1; }
    }
}
