using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>
    ///     The whole app on one screen: the service switch, the running games as cards, and a button
    ///     to add a game by hand. Closing hides to the notification area; the enforcer keeps running.
    /// </summary>
    internal class MainForm : Form
    {
        private readonly Enforcer _enforcer = new Enforcer();
        private readonly Dictionary<string, Icon> _icons = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        private readonly bool _startHidden;
        private readonly bool _silent;
        private Icon _iconOn;
        private Icon _iconOff;
        private ToolStripMenuItem _trayStatus;

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

        public MainForm(bool startHidden, bool silent)
        {
            _startHidden = startHidden;
            _silent = silent;
            Store.Load();

            Text = "KaitoLight";
            _iconOn = MakeAppIcon(false);
            _iconOff = MakeAppIcon(true);
            Icon = _iconOn;
            ClientSize = new Size(560, 660);
            MinimumSize = new Size(480, 520);
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
            ShowStartupNotice();
        }

        // ---------------------------------------------------------------- layout

        private void BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 186, BackColor = Theme.Header, Padding = new Padding(24, 20, 24, 18) };
            header.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border)) e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var title = new Label
            {
                Text = "KaitoLight",
                Font = Theme.Font(20, FontStyle.Bold),
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(0, 18)
            };
            var subtitle = new Label
            {
                Text = "أداة لتشغيل الألعاب في وضع ملء الشاشة دون إطار",
                Font = Theme.Font(10.5f),
                ForeColor = Theme.Muted,
                AutoSize = true,
                Location = new Point(0, 58)
            };

            _power = new PowerButton { On = !Store.Paused };
            _power.Click += (s, e) => SetPaused(!Store.Paused);

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(_power);
            header.Resize += (s, e) =>
            {
                title.Left = header.ClientSize.Width - header.Padding.Right - title.Width;
                subtitle.Left = header.ClientSize.Width - header.Padding.Right - subtitle.Width;
                _power.SetBounds(header.Padding.Left, 96, header.ClientSize.Width - header.Padding.Horizontal, 70);
            };
            Controls.Add(header);
        }

        private void BuildList()
        {
            var section = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 10, 24, 0), BackColor = Theme.Back };

            _countLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Font = Theme.Font(10.5f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "الألعاب قيد التشغيل"
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

            _empty = new Panel { Height = 170, BackColor = Theme.Back };
            _empty.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Theme.Smooth(g);
                var r = _empty.ClientRectangle;
                var box = new RectangleF(0.5f, 0.5f, r.Width - 2, r.Height - 2);
                using (var p = Theme.Round(box, 12))
                using (var pen = new Pen(Theme.Border) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    g.DrawPath(pen, p);
                }
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.RightToLeft | TextFormatFlags.WordBreak;
                TextRenderer.DrawText(g, Store.Paused ? "الخدمة متوقفة" : "لا توجد ألعاب قيد التشغيل",
                    Theme.Font(13, FontStyle.Bold), new Rectangle(16, 46, r.Width - 32, 30), Theme.Text, flags);
                TextRenderer.DrawText(g,
                    Store.Paused
                        ? "فعّل الخدمة من الأعلى لاستئناف تحويل الألعاب."
                        : "شغّل أي لعبة، وستظهر هنا وتُحوَّل إلى ملء الشاشة دون إطار تلقائيًا.",
                    Theme.Font(10), new Rectangle(24, 84, r.Width - 48, 50), Theme.Muted, flags);
            };

            section.Controls.Add(_list);
            section.Controls.Add(_countLabel);
            Controls.Add(section);
            section.BringToFront();
        }

        private void BuildFooter()
        {
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(24, 14, 24, 16), BackColor = Theme.Back };
            footer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border)) e.Graphics.DrawLine(pen, 24, 0, footer.Width - 24, 0);
            };

            var add = new FlatButton
            {
                Text = "إضافة لعبة يدويًا",
                Font = Theme.Font(10, FontStyle.Bold),
                ForeColor = Theme.Text,
                Fill = Theme.Card,
                FillHover = Theme.AccentDim,
                Dock = DockStyle.Right,
                Width = 170
            };
            add.Click += (s, e) => AddGameByHand();

            _startup = new CheckBox
            {
                Text = "التشغيل مع بدء Windows",
                ForeColor = Theme.Muted,
                Font = Theme.Font(10),
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 10, 0, 0),
                Checked = Store.StartsWithWindows()
            };
            _startup.CheckedChanged += (s, e) => Store.SetStartWithWindows(_startup.Checked);

            footer.Controls.Add(add);
            footer.Controls.Add(_startup);
            Controls.Add(footer);
        }

        private void BuildTray()
        {
            _trayStatus = new ToolStripMenuItem { Enabled = false };
            _trayPause = new ToolStripMenuItem("", null, (s, e) => SetPaused(!Store.Paused))
            {
                Font = Theme.Font(10, FontStyle.Bold)
            };

            var menu = new ContextMenuStrip
            {
                RightToLeft = RightToLeft.Yes,
                Renderer = new ToolStripProfessionalRenderer(new TrayColors()),
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                Font = Theme.Font(10),
                ShowImageMargin = false,
                Padding = new Padding(4)
            };
            menu.Items.Add(_trayStatus);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_trayPause);
            menu.Items.Add("فتح نافذة البرنامج", null, (s, e) => ShowFromTray());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("إنهاء البرنامج", null, (s, e) => ExitApp());
            foreach (ToolStripItem item in menu.Items) item.ForeColor = Theme.Text;
            _trayStatus.ForeColor = Theme.Muted;

            _tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
            // a single left click opens the same menu, so turning it off is one click away
            _tray.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                typeof(NotifyIcon).GetMethod("ShowContextMenu",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(_tray, null);
            };
            _tray.DoubleClick += (s, e) => ShowFromTray();
            UpdateTray();
        }

        private void UpdateTray()
        {
            var on = !Store.Paused;
            _tray.Icon = on ? _iconOn : _iconOff;
            _tray.Text = on ? "KaitoLight — الخدمة مفعّلة" : "KaitoLight — الخدمة متوقفة";
            _trayStatus.Text = on ? "●  الخدمة مفعّلة" : "○  الخدمة متوقفة";
            _trayPause.Text = on ? "إيقاف الخدمة" : "تفعيل الخدمة";
            Icon = _tray.Icon;
        }

        private class TrayColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Theme.Card;
            public override Color MenuBorder => Theme.Border;
            public override Color MenuItemSelected => Theme.CardHover;
            public override Color MenuItemBorder => Theme.CardHover;
            public override Color SeparatorDark => Theme.Border;
            public override Color SeparatorLight => Theme.Border;
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

        private void ShowStartupNotice()
        {
            if (!_startHidden || _silent) return;
            var once = new Timer { Interval = 600 };
            once.Tick += (s, e) =>
            {
                once.Dispose();
                _tray.ShowBalloonTip(3000, "يعمل KaitoLight الآن",
                    "البرنامج في منطقة الإشعارات بجوار الساعة. انقر على أيقونته لإيقاف الخدمة أو تفعيلها.", ToolTipIcon.Info);
            };
            once.Start();
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
                    _tray.ShowBalloonTip(3000, "يعمل KaitoLight في الخلفية",
                        "يستمر البرنامج في تحويل الألعاب. يمكنك الوصول إليه من منطقة الإشعارات، ولإنهائه انقر بزر الفأرة الأيمن ثم اختر «إنهاء البرنامج».",
                        ToolTipIcon.Info);
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
            UpdateTray();
            _tray.ShowBalloonTip(1500, "KaitoLight", paused ? "توقفت الخدمة، وأُعيدت الألعاب إلى وضعها الأصلي." : "فُعّلت الخدمة.", ToolTipIcon.None);
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
            _countLabel.Text = games.Count == 0 ? "الألعاب قيد التشغيل" : $"الألعاب قيد التشغيل ({games.Count})";

            var key = string.Join("|", games.Select(g => g.Hwnd.ToInt64()));
            if (key == _cardsKey)
            {
                foreach (Control c in _list.Controls) c.Invalidate();   // states may have changed
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
                var card = new GameCard(game) { GameIcon = IconFor(game.ExePath), Margin = new Padding(0, 0, 0, 10) };
                card.ToggleClicked += (s, e) => ToggleGame(game);
                card.CardClicked += (s, e) => _enforcer.Focus(game);
                new ToolTip().SetToolTip(card, game.ExeName + ".exe — انقر للانتقال إلى اللعبة");
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

        /// <summary>A simple flat teal tile with a light frame-less "screen" mark, so no .ico file is needed.</summary>
        private static Icon MakeAppIcon(bool off)
        {
            using (var bmp = new Bitmap(64, 64))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    Theme.Smooth(g);
                    using (var p = Theme.Round(new RectangleF(2, 2, 60, 60), 14))
                    using (var b = new SolidBrush(off ? Theme.Off : Theme.Accent))
                    {
                        g.FillPath(b, p);
                    }
                    using (var pen = new Pen(Color.FromArgb(235, 242, 241), 5f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                    {
                        // four corner brackets = "fullscreen"
                        g.DrawLines(pen, new[] { new PointF(16, 26), new PointF(16, 16), new PointF(26, 16) });
                        g.DrawLines(pen, new[] { new PointF(38, 16), new PointF(48, 16), new PointF(48, 26) });
                        g.DrawLines(pen, new[] { new PointF(48, 38), new PointF(48, 48), new PointF(38, 48) });
                        g.DrawLines(pen, new[] { new PointF(26, 48), new PointF(16, 48), new PointF(16, 38) });
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
