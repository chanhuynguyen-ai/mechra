using System;
using System.Drawing;

namespace SwCursor.SolidWorksAddin.UI
{
    // Pure geometry, shared by the pane and Windows layout tests. All rectangles stay inside the host.
    internal sealed class PaneRegions
    {
        public Rectangle Header, Feed, Toolbar, Composer, Status;
        public static PaneRegions Calculate(Size size, int dpi, int desiredComposer)
        {
            Func<int, int> px = n => Math.Max(0, (int)Math.Round(n * Math.Max(96, dpi) / 96.0));
            int width = Math.Max(0, size.Width), height = Math.Max(0, size.Height);
            int margin = Math.Min(px(10), width / 8), inner = Math.Max(0, width - 2 * margin);
            int header = Math.Min(px(86), height / 4);
            int status = Math.Min(px(24), height - header);
            int toolbar = Math.Min(px(36), Math.Max(0, height - header - status));
            int composer = Math.Min(Math.Max(px(88), desiredComposer), Math.Max(0, height - header - status - toolbar - px(32)));
            int feed = Math.Max(0, height - header - status - toolbar - composer);
            return new PaneRegions {
                Header = new Rectangle(margin, 0, inner, header),
                Feed = new Rectangle(margin, header, inner, feed),
                Toolbar = new Rectangle(margin, header + feed, inner, toolbar),
                Composer = new Rectangle(margin, header + feed + toolbar, inner, composer),
                Status = new Rectangle(margin, height - status, inner, status)
            };
        }
    }
}
