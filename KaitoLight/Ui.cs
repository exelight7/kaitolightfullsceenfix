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
            var onLeft = L.Ar;                              // RTL: "on" knob sits on the left
            var x = on == onLeft ? sw.Left + 4 : sw.Right - d - 4;
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

            // Arabic: dot + text on the right, switch on the left. English: mirrored.
            var dotX = L.Ar ? r.Right - 30 : r.X + 20;
            var dot = new RectangleF(dotX, r.Y + r.Height / 2 - 5, 10, 10);
            using (var b = new SolidBrush(On ? Theme.Ok : Theme.Muted)) g.FillEllipse(b, dot);

            var flags = L.Align | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            var textLeft = L.Ar ? 96 : (int) dot.Right + 12;
            var textRight = L.Ar ? (int) dot.X - 12 : Width - 96;
            TextRenderer.DrawText(g, On ? L.ServiceOn : L.ServiceOff, Theme.Font(12, FontStyle.Bold),
                new Rectangle(textLeft, (int) r.Y + 12, textRight - textLeft, 24), Theme.Text, flags);
            TextRenderer.DrawText(g, On ? L.ServiceOnHint : L.ServiceOffHint,
                Theme.Font(9.5f), new Rectangle(textLeft, (int) r.Y + 38, textRight - textLeft, 20), Theme.Muted, flags);

            DrawSwitch(g, new Rectangle(L.Ar ? 22 : Width - 22 - 54, (Height - 28) / 2, 54, 28), On);
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

        private Rectangle SwitchRect => new Rectangle(L.Ar ? 20 : Width - 20 - 50, (Height - 26) / 2, 50, 26);

        /// <summary>Raised on right-click (remove / open folder).</summary>
        public event MouseEventHandler MenuRequested;

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right) MenuRequested?.Invoke(this, e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
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

            // icon tile (reading-direction start)
            var tile = new RectangleF(L.Ar ? r.Right - 64 : r.X + 18, r.Y + 14, 46, 46);
            using (var tp = Theme.Round(tile, 10))
            using (var tb = new SolidBrush(Theme.Back))
            {
                g.FillPath(tb, tp);
            }
            if (GameIcon != null)
            {
                g.DrawIcon(GameIcon, new Rectangle((int) tile.X + 7, (int) tile.Y + 7, 32, 32));
            }

            var skipped = Store.Skipped.Contains(Game.ExeName);
            string status;
            Color statusColor;
            switch (Game.State)
            {
                case GameState.Borderless:
                    status = L.StateBorderless;
                    statusColor = Theme.Ok;
                    break;
                case GameState.Waiting:
                    status = L.StateWaiting;
                    statusColor = Theme.Wait;
                    break;
                case GameState.NotRunning:
                    status = skipped ? L.StateOffGame : L.StateNotRunning;
                    statusColor = Theme.Off;
                    break;
                default:
                    status = Store.Paused && !skipped ? L.ServiceOff : L.StateOffGame;
                    statusColor = Theme.Muted;
                    break;
            }

            var sw = SwitchRect;
            int textLeft, textRight;
            if (L.Ar)
            {
                textRight = (int) tile.X - 14;
                textLeft = sw.Right + 18;
            }
            else
            {
                textLeft = (int) tile.Right + 14;
                textRight = sw.Left - 18;
            }
            var flags = L.Align | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Game.DisplayName, Theme.Font(11.5f, FontStyle.Bold),
                new Rectangle(textLeft, (int) r.Y + 14, textRight - textLeft, 24), Theme.Text, flags);

            // small status dot + text
            var dotX = L.Ar ? textRight - 8 : textLeft;
            var statusRect = L.Ar
                ? new Rectangle(textLeft, (int) r.Y + 42, textRight - textLeft - 14, 20)
                : new Rectangle(textLeft + 14, (int) r.Y + 42, textRight - textLeft - 14, 20);
            using (var b = new SolidBrush(statusColor)) g.FillEllipse(b, dotX, statusRect.Y + 7, 7, 7);
            TextRenderer.DrawText(g, status, Theme.Font(9.5f), statusRect, Theme.Muted, flags);

            DrawSwitch(g, sw, !skipped);
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
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | (L.Ar ? TextFormatFlags.RightToLeft : 0));
        }
    }

    /// <summary>Two-option segmented control ("Running" / "All games").</summary>
    internal class Tabs : DrawnControl
    {
        public string[] Items { get; set; } = new string[0];
        public int Selected { get; set; }
        public event EventHandler SelectedChanged;

        private Rectangle ItemRect(int i)
        {
            var w = (Width - 8) / Math.Max(1, Items.Length);
            var visual = L.Ar ? Items.Length - 1 - i : i;   // first item on the reading-direction start
            return new Rectangle(4 + visual * w, 4, w, Height - 8);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            for (var i = 0; i < Items.Length; i++)
            {
                if (!ItemRect(i).Contains(e.Location) || i == Selected) continue;
                Selected = i;
                Invalidate();
                SelectedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var p = Theme.Round(r, 10))
            {
                using (var b = new SolidBrush(Theme.Card)) g.FillPath(b, p);
                using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, p);
            }
            for (var i = 0; i < Items.Length; i++)
            {
                var ir = ItemRect(i);
                if (i == Selected)
                {
                    using (var p = Theme.Round(ir, 7))
                    using (var b = new SolidBrush(Theme.AccentDim))
                    {
                        g.FillPath(b, p);
                    }
                }
                TextRenderer.DrawText(g, Items[i], Theme.Font(10, i == Selected ? FontStyle.Bold : FontStyle.Regular), ir,
                    i == Selected ? Theme.Text : Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | (L.Ar ? TextFormatFlags.RightToLeft : 0));
            }
        }
    }
}
