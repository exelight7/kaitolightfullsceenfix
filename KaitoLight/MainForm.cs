using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>
    ///     The whole app on one screen: a big on/off button, the running games as cards, and one
    ///     "my game isn't listed" button. Closing hides to the tray; the enforcer keeps running.
    /// </summary>
    internal class MainForm : Form
    {
        private readonly Enforcer _enforcer = new Enforcer();
        private readonly Dictionary<string, Icon> _icons = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        private readonly bool _startHidden;

        private PowerButton _power;
        private Label _countLabel;
        private FlowLayoutPanel _list;
        private Panel _empty;
        private CheckBox _startup;
        private NotifyIcon _tray;
        private ToolStripMenuItem _trayPause;
        private Timer _timer;
        private bool _exiting;
        private string _cardsKey = "";

        public MainForm(bool startHidden)
        {
            _startHidden = startHidden;
            Store.Load();

            Text = "KaitoLight";
            Icon = MakeAppIcon();
            ClientSize = new Size(560, 680);
            MinimumSize = new Size(460, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = Theme.Font(10);
            RightToLeft = RightToLeft.Yes;
            DoubleBuffered = true;

            BuildHeader();
            BuildFooter();
            BuildList();
            BuildTray();

            _timer = new Timer { Interval = 1000 };
            _timer.Tick += (s, e) => Tick();
            _timer.Start();
        }

        // ---------------------------------------------------------------- layout

        private void BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 196, Padding = new Padding(22, 18, 22, 10) };
            header.Paint += (s, e) =>
            {
                var r = header.ClientRectangle;
                using (var b = new LinearGradientBrush(r, Color.FromArgb(42, 24, 92), Color.FromArgb(8, 40, 80), 15f))
                    e.Graphics.FillRectangle(b, r);
                using (var line = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(r.Width, 1), 3), Theme.Violet, Theme.Cyan, 0f))
                    e.Graphics.FillRectangle(line, 0, r.Bottom - 3, r.Width, 3);
            };

            var title = new Label
            {
                Text = "⚡ KaitoLight",
                Font = Theme.Font(24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 14)
            };
            var subtitle = new Label
            {
                Text = "يحوّل ألعابك Borderless ملء الشاشة — غصب وتلقائي 🎮",
                Font = Theme.Font(11),
                ForeColor = Color.FromArgb(195, 200, 255),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 62)
            };

            _power = new PowerButton { On = !Store.Paused, Height = 62 };
            _power.Click += (s, e) => SetPaused(!Store.Paused);

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(_power);
            header.Resize += (s, e) =>
            {
                title.Left = header.ClientSize.Width - header.Padding.Right - title.Width;
                subtitle.Left = header.ClientSize.Width - header.Padding.Right - subtitle.Width;
                _power.SetBounds(header.Padding.Left, 104, header.ClientSize.Width - header.Padding.Horizontal, 62);
            };
            Controls.Add(header);
        }

        private void BuildList()
        {
            var section = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 0), BackColor = Theme.Back };

            _countLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Font = Theme.Font(11.5f, FontStyle.Bold),
                ForeColor = Theme.Cyan,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "ألعابك الشغّالة"
            };

            _list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.Back,
                RightToLeft = RightToLeft.No
            };
            _list.Resize += (s, e) => SizeCards();

            _empty = new Panel { Height = 220, BackColor = Theme.Back };
            _empty.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Theme.Smooth(g);
                var r = _empty.ClientRectangle;
                TextRenderer.DrawText(g, "🎮", new Font("Segoe UI Emoji", 40), new Rectangle(0, 10, r.Width, 80),
                    Theme.Text, TextFormatFlags.HorizontalCenter);
                TextRenderer.DrawText(g, Store.Paused ? "البرنامج موقوف" : "شغّل أي لعبة الحين",
                    Theme.Font(15, FontStyle.Bold), new Rectangle(0, 100, r.Width, 34), Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.RightToLeft);
                TextRenderer.DrawText(g,
                    Store.Paused ? "اضغط الزر الأخضر فوق عشان نرجع نحوّل ألعابك." : "بتطلع هنا وبنحوّلها Borderless ملء الشاشة بنفسنا.\nما تحتاج تسوي أي شي.",
                    Theme.Font(10.5f), new Rectangle(10, 140, r.Width - 20, 60), Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.RightToLeft | TextFormatFlags.WordBreak);
            };

            section.Controls.Add(_list);
            section.Controls.Add(_countLabel);
            Controls.Add(section);
            section.BringToFront();
        }

        private void BuildFooter()
        {
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 74, Padding = new Padding(18, 12, 18, 14), BackColor = Theme.Back };

            var add = new FlatButton
            {
                Text = "+  لعبتي ما طلعت؟ أضفها",
                Font = Theme.Font(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Fill = Color.FromArgb(52, 40, 110),
                FillHover = Theme.Violet,
                Dock = DockStyle.Right,
                Width = 230
            };
            add.Click += (s, e) => AddGameByHand();

            _startup = new CheckBox
            {
                Text = "يشتغل مع ويندوز",
                ForeColor = Theme.Muted,
                Font = Theme.Font(10),
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 12, 0, 0),
                Checked = Store.StartsWithWindows()
            };
            _startup.CheckedChanged += (s, e) => Store.SetStartWithWindows(_startup.Checked);

            footer.Controls.Add(add);
            footer.Controls.Add(_startup);
            Controls.Add(footer);
        }

        private void BuildTray()
        {
            _trayPause = new ToolStripMenuItem(Store.Paused ? "▶ تشغيل" : "⏸ إيقاف مؤقت", null, (s, e) => SetPaused(!Store.Paused));
            var menu = new ContextMenuStrip { RightToLeft = RightToLeft.Yes };
            menu.Items.Add("فتح KaitoLight", null, (s, e) => ShowFromTray());
            menu.Items.Add(_trayPause);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("خروج", null, (s, e) => ExitApp());

            _tray = new NotifyIcon
            {
                Icon = Icon,
                Text = "KaitoLight — يحوّل ألعابك Borderless",
                ContextMenuStrip = menu,
                Visible = true
            };
            _tray.DoubleClick += (s, e) => ShowFromTray();
        }

        // ---------------------------------------------------------------- behaviour

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.DarkTitleBar(Handle);
        }

        protected override void SetVisibleCore(bool value)
        {
            // --tray: start hidden without flashing the window
            if (_startHidden && !IsHandleCreated)
            {
                CreateHandle();
                value = false;
            }
            base.SetVisibleCore(value);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Tick();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                // keep working in the background
                e.Cancel = true;
                Hide();
                if (!Store.TrayHintShown)
                {
                    _tray.ShowBalloonTip(3000, "KaitoLight شغّال بالخلفية",
                        "بيكمّل يحوّل ألعابك. تلقاه بجنب الساعة، وللخروج كليك يمين ← خروج.", ToolTipIcon.Info);
                    Store.TrayHintShown = true;
                    Store.Save();
                }
                return;
            }
            _tray.Visible = false;
            base.OnFormClosing(e);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            Tick();
        }

        private void ExitApp()
        {
            _exiting = true;
            _timer.Stop();
            _enforcer.RestoreAll();   // leave games the way we found them
            _tray.Visible = false;
            Application.Exit();
        }

        private void SetPaused(bool paused)
        {
            Store.Paused = paused;
            Store.Save();
            _power.On = !paused;
            _power.Invalidate();
            _trayPause.Text = paused ? "▶ تشغيل" : "⏸ إيقاف مؤقت";
            if (paused) _enforcer.RestoreAll();
            _cardsKey = "";
            Tick();
        }

        private void Tick()
        {
            _enforcer.Scan();
            if (Visible) RefreshCards();
        }

        private void RefreshCards()
        {
            var games = _enforcer.Games;
            _countLabel.Text = games.Count == 0 ? "ألعابك الشغّالة" : $"ألعابك الشغّالة ({games.Count})";

            var key = string.Join("|", games.Select(g => g.Hwnd.ToInt64()));
            if (key == _cardsKey)
            {
                foreach (Control c in _list.Controls) c.Invalidate();   // states may have changed
                _empty.Invalidate();
                return;
            }
            _cardsKey = key;

            _list.SuspendLayout();
            foreach (Control c in _list.Controls.Cast<Control>().ToList())
            {
                _list.Controls.Remove(c);
                if (c != _empty) c.Dispose();
            }

            if (games.Count == 0)
            {
                _list.Controls.Add(_empty);
            }

            foreach (var game in games)
            {
                var card = new GameCard(game) { GameIcon = IconFor(game.ExePath), Margin = new Padding(0, 0, 0, 12) };
                card.ToggleClicked += (s, e) => ToggleGame(game);
                card.CardClicked += (s, e) => _enforcer.Focus(game);
                new ToolTip().SetToolTip(card, game.ExeName + ".exe — اضغط على البطاقة عشان تروح للعبة");
                _list.Controls.Add(card);
            }
            SizeCards();
            _list.ResumeLayout();
        }

        private void SizeCards()
        {
            var w = _list.ClientSize.Width - (_list.VerticalScroll.Visible ? 4 : 0) - 2;
            foreach (Control c in _list.Controls) c.Width = Math.Max(200, w);
        }

        private void ToggleGame(GameWindow game)
        {
            if (Store.Skipped.Remove(game.ExeName))
            {
                _enforcer.Enforce(game);
            }
            else
            {
                Store.Skipped.Add(game.ExeName);
                _enforcer.Restore(game);
                game.State = GameState.Off;
            }
            Store.Save();
            foreach (Control c in _list.Controls) c.Invalidate();
        }

        private void AddGameByHand()
        {
            var known = new HashSet<int>(_enforcer.Games.Select(g => g.Pid));
            var windows = _enforcer.TopLevelWindows().Where(w => !known.Contains(w.Pid)).ToList();
            using (var pick = new PickWindowForm(windows, IconFor))
            {
                if (pick.ShowDialog(this) != DialogResult.OK || pick.Picked == null) return;
                Store.Games.Add(pick.Picked.ExeName);
                Store.Skipped.Remove(pick.Picked.ExeName);
                Store.Save();
            }
            _cardsKey = "";
            Tick();
        }

        private Icon IconFor(string exePath)
        {
            if (string.IsNullOrEmpty(exePath)) return null;
            if (!_icons.TryGetValue(exePath, out var icon))
            {
                _icons[exePath] = icon = Enforcer.IconFor(exePath);
            }
            return icon;
        }

        /// <summary>Draws a small violet/cyan lightning icon so we don't need an .ico file.</summary>
        private static Icon MakeAppIcon()
        {
            using (var bmp = new Bitmap(64, 64))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    Theme.Smooth(g);
                    var r = new RectangleF(2, 2, 60, 60);
                    using (var p = Theme.Round(r, 16))
                    using (var b = new LinearGradientBrush(r, Theme.Violet, Theme.Cyan, 45f))
                    {
                        g.FillPath(b, p);
                    }
                    var bolt = new[]
                    {
                        new PointF(36, 8), new PointF(16, 36), new PointF(30, 36),
                        new PointF(26, 56), new PointF(48, 26), new PointF(34, 26), new PointF(38, 8)
                    };
                    using (var w = new SolidBrush(Color.White)) g.FillPolygon(w, bolt);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
