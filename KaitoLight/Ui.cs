using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>Calm, low-contrast dark palette: soft slate surfaces with one muted teal accent.</summary>
    internal static class Theme
    {
        public static readonly Color Back = Color.FromArgb(22, 24, 29);
        public static readonly Color Header = Color.FromArgb(27, 30, 36);
        public static readonly Color Card = Color.FromArgb(31, 34, 41);
        public static readonly Color CardHover = Color.FromArgb(37, 41, 49);
        public static readonly Color Border = Color.FromArgb(44, 48, 57);
        public static readonly Color Text = Color.FromArgb(226, 229, 234);
        public static readonly Color Muted = Color.FromArgb(139, 146, 158);
        public static readonly Color Accent = Color.FromArgb(94, 163, 156);      // muted teal
        public static readonly Color AccentDim = Color.FromArgb(52, 84, 82);
        public static readonly Color Ok = Color.FromArgb(125, 186, 142);         // soft sage
        public static readonly Color Wait = Color.FromArgb(205, 172, 110);       // soft sand
        public static readonly Color Off = Color.FromArgb(74, 79, 90);

        public static Font Font(float size, FontStyle style = FontStyle.Regular) => new Font("Segoe UI", size, style);

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>Dark title bar on Windows 10/11, tinted to match the header on Windows 11.</summary>
        public static void DarkTitleBar(IntPtr hwnd)
        {
            var on = 1;
            if (DwmSetWindowAttribute(hwnd, 20, ref on, 4) != 0) DwmSetWindowAttribute(hwnd, 19, ref on, 4);
            var caption = Header.R | (Header.G << 8) | (Header.B << 16);   // COLORREF (0x00BBGGRR)
            DwmSetWindowAttribute(hwnd, 35, ref caption, 4);               // DWMWA_CAPTION_COLOR
        }
    }

    /// <summary>Base for owner-drawn controls: double-buffered, hover tracking, hand cursor.</summary>
    internal abstract class DrawnControl : Control
    {
        protected bool Hover;

        protected DrawnControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { Hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { Hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected static void DrawSwitch(Graphics g, Rectangle sw, bool on)
        {
            using (var track = Theme.Round(sw, sw.Height / 2f))
            using (var b = new SolidBrush(on ? Theme.Accent : Theme.Off))
            {
                g.FillPath(b, track);
            }
            var d = sw.Height - 8;
            var x = on ? sw.Left + 4 : sw.Right - d - 4;   // RTL: "on" knob sits on the left
            using (var knob = new SolidBrush(on ? Color.FromArgb(240, 244, 243) : Color.FromArgb(170, 175, 184)))
            {
                g.FillEllipse(knob, x, sw.Top + 4, d, d);
            }
        }
    }

    /// <summary>Main service row at the top: status text on the right, a switch on the left.</summary>
    internal class PowerButton : DrawnControl
    {
        public bool On { get; set; } = true;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var path = Theme.Round(r, 12))
            {
                using (var b = new SolidBrush(Hover ? Theme.CardHover : Theme.Card)) g.FillPath(b, path);
                using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);
            }

            // status dot
            var dot = new RectangleF(r.Right - 30, r.Y + r.Height / 2 - 5, 10, 10);
            using (var b = new SolidBrush(On ? Theme.Ok : Theme.Muted)) g.FillEllipse(b, dot);

            var flags = TextFormatFlags.Right | TextFormatFlags.RightToLeft | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            var textRight = (int) dot.X - 12;
            var textLeft = 96;
            TextRenderer.DrawText(g, On ? "الخدمة مفعّلة" : "الخدمة متوقفة", Theme.Font(12, FontStyle.Bold),
                new Rectangle(textLeft, (int) r.Y + 12, textRight - textLeft, 24), Theme.Text, flags);
            TextRenderer.DrawText(g,
                On ? "يُحوَّل كل تطبيق ألعاب يُشغَّل إلى وضع ملء الشاشة دون إطار تلقائيًا." : "لن تُعدَّل نوافذ الألعاب حتى تُعاد الخدمة.",
                Theme.Font(9.5f), new Rectangle(textLeft, (int) r.Y + 38, textRight - textLeft, 20), Theme.Muted, flags);

            DrawSwitch(g, new Rectangle(22, (Height - 28) / 2, 54, 28), On);
        }
    }

    /// <summary>One game: icon, name, status, and an on/off switch.</summary>
    internal class GameCard : DrawnControl
    {
        public GameWindow Game { get; }
        public Icon GameIcon { get; set; }

        /// <summary>Raised when the switch is clicked.</summary>
        public event EventHandler ToggleClicked;
        /// <summary>Raised when the rest of the card is clicked.</summary>
        public event EventHandler CardClicked;

        public GameCard(GameWindow game)
        {
            Game = game;
            Height = 76;
        }

        private Rectangle SwitchRect => new Rectangle(20, (Height - 26) / 2, 50, 26);

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var hit = SwitchRect;
            hit.Inflate(10, 12);
            if (hit.Contains(e.Location)) ToggleClicked?.Invoke(this, EventArgs.Empty);
            else CardClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);

            using (var path = Theme.Round(r, 12))
            {
                using (var back = new SolidBrush(Hover ? Theme.CardHover : Theme.Card)) g.FillPath(back, path);
                using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);
            }

            // icon tile
            var tile = new RectangleF(r.Right - 64, r.Y + 14, 46, 46);
            using (var tp = Theme.Round(tile, 10))
            using (var tb = new SolidBrush(Theme.Back))
            {
                g.FillPath(tb, tp);
            }
            if (GameIcon != null)
            {
                g.DrawIcon(GameIcon, new Rectangle((int) tile.X + 7, (int) tile.Y + 7, 32, 32));
            }

            string status;
            Color statusColor;
            switch (Game.State)
            {
                case GameState.Borderless:
                    status = "يعمل بملء الشاشة دون إطار";
                    statusColor = Theme.Ok;
                    break;
                case GameState.Waiting:
                    status = "بانتظار ظهور نافذة اللعبة";
                    statusColor = Theme.Wait;
                    break;
                default:
                    status = Store.Paused ? "الخدمة متوقفة" : "التحويل معطّل لهذه اللعبة";
                    statusColor = Theme.Muted;
                    break;
            }

            var textRight = (int) tile.X - 14;
            var textLeft = SwitchRect.Right + 18;
            var flags = TextFormatFlags.Right | TextFormatFlags.RightToLeft | TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Game.DisplayName, Theme.Font(11.5f, FontStyle.Bold),
                new Rectangle(textLeft, (int) r.Y + 14, textRight - textLeft, 24), Theme.Text, flags);

            // small status dot + text
            var statusRect = new Rectangle(textLeft, (int) r.Y + 42, textRight - textLeft - 14, 20);
            using (var b = new SolidBrush(statusColor)) g.FillEllipse(b, textRight - 8, statusRect.Y + 7, 7, 7);
            TextRenderer.DrawText(g, status, Theme.Font(9.5f), statusRect, Theme.Muted, flags);

            DrawSwitch(g, SwitchRect, Game.State != GameState.Off);
        }
    }

    /// <summary>Flat rounded text button.</summary>
    internal class FlatButton : DrawnControl
    {
        public Color Fill { get; set; } = Theme.Card;
        public Color FillHover { get; set; } = Theme.CardHover;
        public Color Outline { get; set; } = Theme.Border;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var p = Theme.Round(r, 10))
            {
                using (var b = new SolidBrush(Hover ? FillHover : Fill)) g.FillPath(b, p);
                using (var pen = new Pen(Outline)) g.DrawPath(pen, p);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.RightToLeft);
        }
    }
}
