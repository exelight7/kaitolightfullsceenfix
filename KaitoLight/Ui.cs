using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KaitoLight
{
    internal static class Theme
    {
        public static readonly Color Back = Color.FromArgb(15, 15, 26);
        public static readonly Color Card = Color.FromArgb(28, 28, 46);
        public static readonly Color CardHover = Color.FromArgb(38, 38, 62);
        public static readonly Color Text = Color.FromArgb(240, 240, 252);
        public static readonly Color Muted = Color.FromArgb(150, 150, 180);
        public static readonly Color Violet = Color.FromArgb(124, 92, 255);
        public static readonly Color Cyan = Color.FromArgb(0, 200, 255);
        public static readonly Color Green = Color.FromArgb(40, 210, 130);
        public static readonly Color Amber = Color.FromArgb(255, 184, 0);
        public static readonly Color Gray = Color.FromArgb(90, 90, 115);

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

        /// <summary>Dark title bar on Windows 10/11.</summary>
        public static void DarkTitleBar(IntPtr hwnd)
        {
            var on = 1;
            if (DwmSetWindowAttribute(hwnd, 20, ref on, 4) != 0) DwmSetWindowAttribute(hwnd, 19, ref on, 4);
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
    }

    /// <summary>The big "on / paused" button at the top.</summary>
    internal class PowerButton : DrawnControl
    {
        public bool On { get; set; } = true;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(2, 2, Width - 5, Height - 5);
            var a = On ? Color.FromArgb(30, 200, 120) : Color.FromArgb(95, 95, 120);
            var b = On ? Color.FromArgb(0, 170, 220) : Color.FromArgb(70, 70, 95);
            using (var path = Theme.Round(r, r.Height / 2))
            using (var brush = new LinearGradientBrush(r, a, b, 0f))
            {
                g.FillPath(brush, path);
                if (Hover)
                {
                    using (var glow = new SolidBrush(Color.FromArgb(35, 255, 255, 255))) g.FillPath(glow, path);
                }
            }

            var dot = new RectangleF(r.Right - r.Height + 8, r.Y + 8, r.Height - 16, r.Height - 16);
            using (var white = new SolidBrush(Color.White)) g.FillEllipse(white, dot);
            TextRenderer.DrawText(g, On ? "⏻" : "▶", Theme.Font(14, FontStyle.Bold), Rectangle.Round(dot),
                On ? Color.FromArgb(20, 160, 110) : Color.FromArgb(90, 90, 115),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var text = new Rectangle((int) r.X + 18, (int) r.Y, (int) (r.Width - r.Height - 24), (int) r.Height);
            TextRenderer.DrawText(g, On ? "شغّال — أي لعبة تفتحها تصير Borderless" : "موقوف — اضغط للتشغيل",
                Theme.Font(12.5f, FontStyle.Bold), text, Color.White,
                TextFormatFlags.Right | TextFormatFlags.RightToLeft | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>One game: icon, name, status, and an on/off switch.</summary>
    internal class GameCard : DrawnControl
    {
        private static readonly Color[][] Accents =
        {
            new[] { Color.FromArgb(255, 94, 98), Color.FromArgb(255, 153, 102) },
            new[] { Color.FromArgb(124, 92, 255), Color.FromArgb(198, 92, 255) },
            new[] { Color.FromArgb(0, 176, 255), Color.FromArgb(0, 224, 200) },
            new[] { Color.FromArgb(255, 72, 162), Color.FromArgb(255, 128, 92) },
            new[] { Color.FromArgb(46, 196, 120), Color.FromArgb(160, 220, 60) },
            new[] { Color.FromArgb(255, 176, 0), Color.FromArgb(255, 110, 64) },
            new[] { Color.FromArgb(64, 120, 255), Color.FromArgb(140, 82, 255) }
        };

        public GameWindow Game { get; }
        public Icon GameIcon { get; set; }

        /// <summary>Raised when the switch is clicked.</summary>
        public event EventHandler ToggleClicked;
        /// <summary>Raised when the rest of the card is clicked.</summary>
        public event EventHandler CardClicked;

        public GameCard(GameWindow game)
        {
            Game = game;
            Height = 84;
        }

        private Rectangle SwitchRect => new Rectangle(18, (Height - 30) / 2, 58, 30);

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

            var accent = Accents[(int) ((uint) Game.ExeName.ToLowerInvariant().GetHashCode() % Accents.Length)];
            var r = new RectangleF(1, 1, Width - 3, Height - 3);
            var on = Game.State != GameState.Off;

            using (var path = Theme.Round(r, 16))
            {
                using (var back = new SolidBrush(Hover ? Theme.CardHover : Theme.Card)) g.FillPath(back, path);

                // colored strip on the right edge
                var strip = new RectangleF(r.Right - 7, r.Y, 7, r.Height);
                var oldClip = g.Clip;
                g.SetClip(path);
                using (var sb = new LinearGradientBrush(strip, accent[0], accent[1], 90f)) g.FillRectangle(sb, strip);
                g.Clip = oldClip;
            }

            // icon tile
            var tile = new RectangleF(r.Right - 76, r.Y + 12, 56, 56);
            using (var tp = Theme.Round(tile, 14))
            using (var tb = new LinearGradientBrush(tile, accent[0], accent[1], 45f))
            {
                g.FillPath(tb, tp);
            }
            if (GameIcon != null)
            {
                g.DrawIcon(GameIcon, new Rectangle((int) tile.X + 12, (int) tile.Y + 12, 32, 32));
            }
            else
            {
                TextRenderer.DrawText(g, "🎮", new Font("Segoe UI Emoji", 18), Rectangle.Round(tile), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // name + status
            var textRight = (int) tile.X - 14;
            var textLeft = SwitchRect.Right + 16;
            var flags = TextFormatFlags.Right | TextFormatFlags.RightToLeft | TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Game.DisplayName, Theme.Font(13, FontStyle.Bold),
                new Rectangle(textLeft, (int) r.Y + 14, textRight - textLeft, 28), Theme.Text, flags);

            string status;
            Color statusColor;
            switch (Game.State)
            {
                case GameState.Borderless:
                    status = "● Borderless ملء الشاشة";
                    statusColor = Theme.Green;
                    break;
                case GameState.Waiting:
                    status = "● ننتظر اللعبة تظهر…";
                    statusColor = Theme.Amber;
                    break;
                default:
                    status = Store.Paused ? "● البرنامج موقوف" : "● مطفي لهذي اللعبة";
                    statusColor = Theme.Muted;
                    break;
            }
            TextRenderer.DrawText(g, status, Theme.Font(10, FontStyle.Bold),
                new Rectangle(textLeft, (int) r.Y + 46, textRight - textLeft, 22), statusColor, flags);

            // switch
            var sw = SwitchRect;
            var swOn = on && !Store.Skipped.Contains(Game.ExeName);
            using (var sp = Theme.Round(sw, sw.Height / 2f))
            {
                if (swOn)
                {
                    using (var b = new LinearGradientBrush(sw, accent[0], accent[1], 0f)) g.FillPath(b, sp);
                }
                else
                {
                    using (var b = new SolidBrush(Theme.Gray)) g.FillPath(b, sp);
                }
            }
            var knobX = swOn ? sw.Left + 4 : sw.Right - sw.Height + 4; // RTL: "on" knob sits left
            using (var knob = new SolidBrush(Color.White))
            {
                g.FillEllipse(knob, knobX, sw.Top + 4, sw.Height - 8, sw.Height - 8);
            }
        }
    }

    /// <summary>Flat rounded text button.</summary>
    internal class FlatButton : DrawnControl
    {
        public Color Fill { get; set; } = Theme.Card;
        public Color FillHover { get; set; } = Theme.CardHover;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(1, 1, Width - 3, Height - 3);
            using (var p = Theme.Round(r, r.Height / 2))
            using (var b = new SolidBrush(Hover ? FillHover : Fill))
            {
                g.FillPath(b, p);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.RightToLeft);
        }
    }
}
