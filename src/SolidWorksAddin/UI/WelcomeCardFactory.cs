using System;
using System.Drawing;
using System.Windows.Forms;

namespace SwCursor.SolidWorksAddin.UI
{
    // The same welcome controls are exercised by the native Windows UI harness.
    internal static class WelcomeCardFactory
    {
        public static StackCard Create(Action<string> draft, int dpi)
        {
            Func<int, int> px = n => (int)Math.Round(n * Math.Max(96, dpi) / 96.0);
            var card = new StackCard { Padding = new Padding(px(14)) };
            card.Add(ProductTheme.Label("THIẾT KẾ CÙNG MECHRA", 8, ProductTheme.Accent, true));
            card.Add(ProductTheme.Label("Bạn muốn tạo gì?", 14, ProductTheme.Text, true));
            card.Add(ProductTheme.Label("Tạo plate native hoặc sửa chiều dày. Mechra kiểm chứng sau mỗi lần áp dụng.", 9.5F, ProductTheme.Muted));
            card.Add(ProductTheme.Label("Mô tả → Xem kế hoạch → Áp dụng", 8, ProductTheme.Accent));
            var create = new ProductButton("Tạo plate 100 × 60 × 5 mm", 230) { Dock = DockStyle.Fill,
                Margin = new Padding(0, px(4), 0, px(8)), Height = px(34) };
            create.Click += (_, __) => draft("Tạo plate 100 x 60 x 5 mm"); card.Add(create);
            var edit = new ProductButton("Đổi chiều dày thành 8 mm", 230) { Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, px(8)), Height = px(34) };
            edit.Click += (_, __) => draft("Đổi chiều dày thành 8 mm"); card.Add(edit);
            card.Add(ProductTheme.Label("Bắt đầu với Part trống. Planner cục bộ; chưa kết nối mô hình AI.", 8.5F, ProductTheme.Muted));
            return card;
        }
    }
}
