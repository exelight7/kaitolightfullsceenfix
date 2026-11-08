using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using BorderlessGaming.Logic.Core;
using BorderlessGaming.Logic.Models;
using BorderlessGaming.Logic.Windows;

namespace BorderlessGaming.Forms
{
    /// <summary>
    ///     Colorful "your games" bar at the top of the main window, plus a dark theme for the rest of the form.
    ///     One click on a game card makes it borderless; another click restores it.
    /// </summary>
    public partial class MainWindow
    {
        // Theme palette
        internal static readonly Color ThemeBack = Color.FromArgb(18, 18, 30);
        internal static readonly Color ThemeSurface = Color.FromArgb(30, 30, 48);
        internal static readonly Color ThemeSurfaceHi = Color.FromArgb(44, 44, 70);
        internal static readonly Color ThemeText = Color.FromArgb(236, 236, 248);
        internal static readonly Color ThemeMuted = Color.FromArgb(160, 160, 190);
        internal static readonly Color ThemeAccent = Color.FromArgb(124, 92, 255);
        internal static readonly Color ThemeAccent2 = Color.FromArgb(0, 200, 255);

        // Each game card gets one of these gradient pairs, picked from its name
        private static readonly Color[][] CardPalettes =
        {
            new[] { Color.FromArgb(255, 94, 98), Color.FromArgb(255, 153, 102) },   // coral → peach
            new[] { Color.FromArgb(124, 92, 255), Color.FromArgb(198, 92, 255) },   // violet → orchid
            new[] { Color.FromArgb(0, 176, 255), Color.FromArgb(0, 224, 200) },     // sky → aqua
            new[] { Color.FromArgb(255, 72, 162), Color.FromArgb(255, 128, 92) },   // pink → orange
            new[] { Color.FromArgb(46, 196, 120), Color.FromArgb(160, 220, 60) },   // green → lime
            new[] { Color.FromArgb(255, 176, 0), Color.FromArgb(255, 110, 64) },    // gold → tangerine
            new[] { Color.FromArgb(64, 120, 255), Color.FromArgb(140, 82, 255) },   // blue → purple
        };

        private GradientPanel _gamesBar;
        private FlowLayoutPanel _gamesFlow;
        private Label _gamesEmpty;
        private CheckBox _autoGames;
        private Timer _gamesTimer;
        private string _gamesSignature = "";

        private void BuildGamesBar()
        {
            _gamesBar = new GradientPanel
            {
                Dock = DockStyle.Top,
                Height = 178,
                Padding = new Padding(16, 10, 16, 10),
                RightToLeft = RightToLeft.Yes,
                ColorA = Color.FromArgb(40, 22, 84),
                ColorB = Color.FromArgb(10, 40, 78)
            };

            var header = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.Transparent };

            var title = new Label
            {
                Text = "🎮  ألعابك",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Right,
                BackColor = Color.Transparent
            };

            var subtitle = new Label
            {
                Text = "اضغط على اللعبة لتصير ملء الشاشة بلا حدود",
                ForeColor = Color.FromArgb(190, 200, 255),
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = true,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 9, 10, 0),
                BackColor = Color.Transparent
            };

            _autoGames = new CheckBox
            {
                Text = "⚡ طبّق تلقائياً على أي لعبة أفتحها",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.Transparent,
                Checked = Config.Instance.AppSettings.AutoBorderlessGames
            };
            _autoGames.CheckedChanged += (s, e) =>
            {
                Config.Instance.AppSettings.AutoBorderlessGames = _autoGames.Checked;
                Config.Save();
            };

            header.Controls.Add(subtitle);
            header.Controls.Add(title);
            header.Controls.Add(_autoGames);

            _gamesFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };

            _gamesEmpty = new Label
            {
                Text = "ما فيه ألعاب شغّالة الحين 😴\nشغّل لعبتك بوضع Windowed (نافذة) وبتطلع هنا تلقائياً.",
                ForeColor = Color.FromArgb(200, 205, 240),
                Font = new Font("Segoe UI", 10.5F),
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 22, 4, 0)
            };

            _gamesBar.Controls.Add(_gamesFlow);
            _gamesBar.Controls.Add(header);

            Height += _gamesBar.Height;
            MinimumSize = new Size(Math.Max(MinimumSize.Width, 760), Math.Max(MinimumSize.Height, 520));

            Controls.Add(_gamesBar);
            // Sit just below the menu strip (docking runs from the back of the z-order forward)
            Controls.SetChildIndex(_gamesBar, Controls.GetChildIndex(mnuMain));

            ApplyTheme();

            _gamesTimer = new Timer { Interval = 1500 };
            _gamesTimer.Tick += (s, e) => RefreshGamesBar();
            _gamesTimer.Start();
            RefreshGamesBar();
        }

        /// <summary>Dark, colorful look for the existing designer controls.</summary>
        private void ApplyTheme()
        {
            BackColor = ThemeBack;
            ForeColor = ThemeText;

            tableLayoutPanel1.BackColor = ThemeBack;
            tableLayoutPanel1.Padding = new Padding(10, 8, 10, 6);
            flowLayoutPanel1.BackColor = ThemeBack;

            foreach (var lbl in new[] { processLabel, favoritesLabel })
            {
                lbl.ForeColor = ThemeAccent2;
                lbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            }

            foreach (var list in new[] { lstProcesses, lstFavorites })
            {
                list.BackColor = ThemeSurface;
                list.ForeColor = ThemeText;
                list.BorderStyle = BorderStyle.None;
                list.Font = new Font("Segoe UI", 10F);
                list.DrawMode = DrawMode.OwnerDrawFixed;
                list.ItemHeight = 28;
                list.DrawItem += DrawThemedListItem;
            }

            foreach (Control c in flowLayoutPanel1.Controls)
            {
                if (c is Button b)
                {
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderSize = 0;
                    b.BackColor = ThemeSurfaceHi;
                    b.ForeColor = ThemeText;
                    b.FlatAppearance.MouseOverBackColor = ThemeAccent;
                    b.Cursor = Cursors.Hand;
                    b.UseVisualStyleBackColor = false;
                }
            }

            var renderer = new ToolStripProfessionalRenderer(new ThemeColorTable()) { RoundedEdges = false };
            mnuMain.Renderer = renderer;
            mnuMain.BackColor = ThemeBack;
            mnuMain.ForeColor = ThemeText;
            ColorMenuItems(mnuMain.Items);

            statusStrip1.Renderer = renderer;
            statusStrip1.BackColor = Color.FromArgb(24, 22, 44);
            statusLabel.ForeColor = ThemeMuted;
        }

        private static void ColorMenuItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = ThemeText;
                if (item is ToolStripMenuItem mi && mi.HasDropDownItems)
                {
                    mi.DropDown.BackColor = ThemeSurface;
                    ColorMenuItems(mi.DropDownItems);
                }
            }
        }

        private void DrawThemedListItem(object sender, DrawItemEventArgs e)
        {
            var list = (ListBox) sender;
            if (e.Index < 0 || e.Index >= list.Items.Count)
            {
                return;
            }

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var back = new SolidBrush(ThemeSurface))
            {
                g.FillRectangle(back, e.Bounds);
            }

            var item = list.Items[e.Index];
            var isGame = item is ProcessDetails pd && pd.IsGame;
            var r = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 2, e.Bounds.Width - 8, e.Bounds.Height - 4);

            if (selected)
            {
                using (var path = RoundedRect(r, 8))
                using (var brush = new LinearGradientBrush(r, ThemeAccent, Color.FromArgb(70, 110, 255), LinearGradientMode.Horizontal))
                {
                    g.FillPath(brush, path);
                }
            }
            else if (isGame)
            {
                using (var path = RoundedRect(r, 8))
                using (var brush = new SolidBrush(Color.FromArgb(46, 38, 82)))
                {
                    g.FillPath(brush, path);
                }
                using (var bar = new SolidBrush(ThemeAccent2))
                {
                    g.FillRectangle(bar, r.X, r.Y + 5, 3, r.Height - 10);
                }
            }

            var textColor = selected ? Color.White : isGame ? Color.FromArgb(150, 230, 255) : ThemeText;
            var textRect = new Rectangle(r.X + 10, r.Y, r.Width - 14, r.Height);
            TextRenderer.DrawText(g, item.ToString(), list.Font, textRect, textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private List<ProcessDetails> CurrentGames()
        {
            try
            {
                return _watcher.Processes.ToList()
                    .Where(p => p != null && p.Manageable && !p.ProcessHasExited && p.IsGame)
                    .GroupBy(p => p.Proc.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            catch
            {
                return new List<ProcessDetails>();
            }
        }

        private void RefreshGamesBar()
        {
            if (_gamesFlow == null || !Visible)
            {
                return;
            }

            var games = CurrentGames();
            var signature = string.Join("|", games.Select(g => g.Proc.Id + ":" + g.MadeBorderless + ":" + g.WindowTitle));
            if (signature == _gamesSignature)
            {
                return;
            }
            _gamesSignature = signature;

            _gamesFlow.SuspendLayout();
            foreach (Control c in _gamesFlow.Controls.Cast<Control>().ToList())
            {
                if (c != _gamesEmpty)
                {
                    c.Dispose();
                }
            }
            _gamesFlow.Controls.Clear();

            if (games.Count == 0)
            {
                _gamesFlow.Controls.Add(_gamesEmpty);
            }

            foreach (var game in games)
            {
                _gamesFlow.Controls.Add(MakeGameCard(game));
            }

            _gamesFlow.ResumeLayout();
        }

        private Control MakeGameCard(ProcessDetails game)
        {
            var name = string.IsNullOrWhiteSpace(game.WindowTitle) ? game.BinaryName : game.WindowTitle.Trim();
            var palette = CardPalettes[(int) ((uint) game.BinaryName.ToLowerInvariant().GetHashCode() % CardPalettes.Length)];

            var card = new GameCard
            {
                Title = name,
                Subtitle = game.MadeBorderless ? "✔ بلا حدود — اضغط للإرجاع" : "▶ اضغط لملء الشاشة",
                Active = game.MadeBorderless,
                ColorA = palette[0],
                ColorB = palette[1],
                Size = new Size(230, 92),
                Margin = new Padding(8, 0, 8, 0)
            };

            card.Click += async (s, e) =>
            {
                card.Enabled = false;
                try
                {
                    if (game.MadeBorderless)
                    {
                        Manipulation.RestoreWindow(game);
                    }
                    else
                    {
                        await _watcher.RemoveBorder(game);
                    }
                }
                catch
                {
                    // ignored: the game may have closed
                }
                _gamesSignature = "";
                RefreshGamesBar();
            };

            new ToolTip().SetToolTip(card, game.BinaryName + ".exe");
            return card;
        }

        internal static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Panel with a diagonal gradient background and a soft glow line along the bottom.</summary>
        private class GradientPanel : Panel
        {
            public Color ColorA { get; set; }
            public Color ColorB { get; set; }

            public GradientPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
                {
                    return;
                }
                using (var brush = new LinearGradientBrush(ClientRectangle, ColorA, ColorB, 20f))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                var line = new Rectangle(0, Height - 3, Width, 3);
                using (var glow = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(Width, 1), 3), ThemeAccent, ThemeAccent2, 0f))
                {
                    e.Graphics.FillRectangle(glow, line);
                }
            }
        }

        /// <summary>Rounded, gradient game card with hover lift and an "active" ring.</summary>
        private class GameCard : Control
        {
            private bool _hover;

            public string Title { get; set; }
            public string Subtitle { get; set; }
            public bool Active { get; set; }
            public Color ColorA { get; set; }
            public Color ColorB { get; set; }

            public GameCard()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _hover = true;
                Invalidate();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hover = false;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var lift = _hover && Enabled ? 0 : 3;
                var r = new Rectangle(3, 3 + lift, Width - 7, Height - 10);

                // shadow
                using (var shadowPath = RoundedRect(new Rectangle(r.X + 2, r.Y + 5, r.Width, r.Height), 14))
                using (var shadow = new SolidBrush(Color.FromArgb(_hover ? 110 : 70, 0, 0, 0)))
                {
                    g.FillPath(shadow, shadowPath);
                }

                var a = Enabled ? ColorA : Color.Gray;
                var b = Enabled ? ColorB : Color.DimGray;
                using (var path = RoundedRect(r, 14))
                {
                    using (var brush = new LinearGradientBrush(r, a, b, 35f))
                    {
                        g.FillPath(brush, path);
                    }

                    // glossy top highlight
                    var gloss = new Rectangle(r.X, r.Y, r.Width, r.Height / 2);
                    using (var glossPath = RoundedRect(gloss, 14))
                    using (var glossBrush = new LinearGradientBrush(gloss, Color.FromArgb(_hover ? 70 : 45, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
                    {
                        g.FillPath(glossBrush, glossPath);
                    }

                    if (Active)
                    {
                        using (var ring = new Pen(Color.White, 3f))
                        {
                            g.DrawPath(ring, path);
                        }
                    }
                }

                // badge
                var badge = new Rectangle(r.Right - 40, r.Y + 10, 28, 28);
                using (var badgeBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                {
                    g.FillEllipse(badgeBrush, badge);
                }
                TextRenderer.DrawText(g, Active ? "✔" : "🎮", new Font("Segoe UI Emoji", 11F), badge, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                var flags = TextFormatFlags.Right | TextFormatFlags.RightToLeft | TextFormatFlags.EndEllipsis |
                            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
                using (var titleFont = new Font("Segoe UI", 11.5F, FontStyle.Bold))
                using (var subFont = new Font("Segoe UI", 9F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, Title, titleFont, new Rectangle(r.X + 10, r.Y + 12, r.Width - 58, 26), Color.White, flags);
                    TextRenderer.DrawText(g, Subtitle, subFont, new Rectangle(r.X + 10, r.Bottom - 32, r.Width - 22, 22),
                        Color.FromArgb(240, 255, 255, 255), flags);
                }
            }
        }

        /// <summary>Colors for the menu strip, drop-downs and status bar.</summary>
        private class ThemeColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin => ThemeBack;
            public override Color MenuStripGradientEnd => ThemeBack;
            public override Color ToolStripDropDownBackground => ThemeSurface;
            public override Color ImageMarginGradientBegin => ThemeSurface;
            public override Color ImageMarginGradientMiddle => ThemeSurface;
            public override Color ImageMarginGradientEnd => ThemeSurface;
            public override Color MenuItemSelected => ThemeAccent;
            public override Color MenuItemSelectedGradientBegin => ThemeAccent;
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(70, 110, 255);
            public override Color MenuItemPressedGradientBegin => ThemeSurfaceHi;
            public override Color MenuItemPressedGradientEnd => ThemeSurfaceHi;
            public override Color MenuItemBorder => ThemeAccent;
            public override Color MenuBorder => ThemeSurfaceHi;
            public override Color SeparatorDark => ThemeSurfaceHi;
            public override Color SeparatorLight => ThemeSurfaceHi;
            public override Color CheckBackground => ThemeAccent;
            public override Color CheckSelectedBackground => ThemeAccent2;
            public override Color CheckPressedBackground => ThemeAccent2;
            public override Color StatusStripGradientBegin => Color.FromArgb(24, 22, 44);
            public override Color StatusStripGradientEnd => Color.FromArgb(24, 22, 44);
        }
    }
}
