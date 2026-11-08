using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>
    ///     The whole app on one screen: the service switch, the games (running now / all remembered),
    ///     and a button to add a game by hand. It lives in the notification area; closing just hides it.
    /// </summary>
    internal class MainForm : Form
    {
        private readonly Enforcer _enforcer = new Enforcer();
        private readonly Dictionary<string, Icon> _icons = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        private readonly bool _startHidden;
        private readonly bool _silent;
        private readonly Icon _iconOn;
        private readonly Icon _iconOff;

        private PowerButton _power;
        private Tabs _tabs;
        private FlowLayoutPanel _list;
        private Panel _empty;
        private CheckBox _startup;
        private NotifyIcon _tray;
        private ToolStripMenuItem _trayStatus;
        private ToolStripMenuItem _trayPause;
        private Timer _timer;
        private bool _exiting;
        private string _cardsKey = "";

        private bool ShowingAll => _tabs.Selected == 1;

        public MainForm(bool startHidden, bool silent)
        {
            _startHidden = startHidden;
            _silent = silent;

            Text = "KaitoLight";
            _iconOn = LoadAppIcon() ?? MakeAppIcon(false);
            _iconOff = MakeAppIcon(true);
            Icon = _iconOn;
            ClientSize = new Size(560, 680);
            MinimumSize = new Size(480, 540);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = Theme.Font(10);
            RightToLeft = L.Rtl;
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

            var title = new Label { Text = "KaitoLight", Font = Theme.Font(20, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Top = 18 };
            var subtitle = new Label { Text = L.Subtitle, Font = Theme.Font(10.5f), ForeColor = Theme.Muted, AutoSize = true, Top = 58 };

            _power = new PowerButton { On = !Store.Paused };
            _power.Click += (s, e) => SetPaused(!Store.Paused);

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(_power);
            header.Resize += (s, e) =>
            {
                // RightToLeft doesn't mirror absolute positions, so place labels on the reading-direction start
                var start = L.Ar ? header.ClientSize.Width - header.Padding.Right : header.Padding.Left;
                title.Left = L.Ar ? start - title.Width : start;
                subtitle.Left = L.Ar ? start - subtitle.Width : start;
                _power.SetBounds(header.Padding.Left, 96, header.ClientSize.Width - header.Padding.Horizontal, 70);
            };
            Controls.Add(header);
        }

        private void BuildList()
        {
            var section = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 14, 24, 0), BackColor = Theme.Back };

            _tabs = new Tabs { Dock = DockStyle.Top, Height = 40, Items = new[] { L.TabRunning, L.TabAll } };
            _tabs.SelectedChanged += (s, e) =>
            {
                _cardsKey = "";
                RefreshCards();
            };
            var gap = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Theme.Back };

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

                string head, hint;
                if (ShowingAll) { head = L.EmptyAll; hint = L.EmptyAllHint; }
                else if (Store.Paused) { head = L.ServiceOff; hint = L.EmptyPausedHint; }
                else { head = L.EmptyRunning; hint = L.EmptyRunningHint; }

                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | (L.Ar ? TextFormatFlags.RightToLeft : 0);
                TextRenderer.DrawText(g, head, Theme.Font(13, FontStyle.Bold), new Rectangle(16, 46, r.Width - 32, 30), Theme.Text, flags);
                TextRenderer.DrawText(g, hint, Theme.Font(10), new Rectangle(24, 84, r.Width - 48, 50), Theme.Muted, flags);
            };

            section.Controls.Add(_list);
            section.Controls.Add(gap);
            section.Controls.Add(_tabs);
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
                Text = L.AddGame,
                Font = Theme.Font(10, FontStyle.Bold),
                ForeColor = Theme.Text,
                FillHover = Theme.AccentDim,
                Dock = DockStyle.Right,
                Width = 170
            };
            add.Click += (s, e) => AddGameByHand();

            var lang = new FlatButton
            {
                Text = L.OtherLanguage,
                Font = Theme.Font(9.5f),
                ForeColor = Theme.Muted,
                Dock = DockStyle.Right,
                Width = 92
            };
            lang.Click += (s, e) => SwitchLanguage();
            var spacer = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Back };

            _startup = new CheckBox
            {
                Text = L.StartWithWindows,
                ForeColor = Theme.Muted,
                Font = Theme.Font(10),
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 10, 0, 0),
                Checked = Store.StartsWithWindows()
            };
            _startup.CheckedChanged += (s, e) => Store.SetStartWithWindows(_startup.Checked);

            footer.Controls.Add(_startup);
            footer.Controls.Add(lang);
            footer.Controls.Add(spacer);
            footer.Controls.Add(add);
            Controls.Add(footer);
        }

        private void BuildTray()
        {
            _trayStatus = new ToolStripMenuItem { Enabled = false };
            _trayPause = new ToolStripMenuItem("", null, (s, e) => SetPaused(!Store.Paused)) { Font = Theme.Font(10, FontStyle.Bold) };

            var menu = new ContextMenuStrip
            {
                RightToLeft = L.Rtl,
                Renderer = new ToolStripProfessionalRenderer(new MenuColors()),
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                Font = Theme.Font(10),
                ShowImageMargin = false,
                Padding = new Padding(4)
            };
            menu.Items.Add(_trayStatus);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_trayPause);
            menu.Items.Add(L.TrayOpen, null, (s, e) => ShowFromTray());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L.TrayExit, null, (s, e) => ExitApp());
            foreach (ToolStripItem item in menu.Items) item.ForeColor = Theme.Text;

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
            _tray.Text = "KaitoLight — " + (on ? L.ServiceOn : L.ServiceOff);
            _trayStatus.Text = on ? L.TrayOn : L.TrayOff;
            _trayStatus.ForeColor = Theme.Muted;
            _trayPause.Text = on ? L.TrayPause : L.TrayResume;
            Icon = _tray.Icon;
        }

        private class MenuColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Theme.Card;
            public override Color MenuBorder => Theme.Border;
            public override Color MenuItemSelected => Theme.CardHover;
            public override Color MenuItemBorder => Theme.CardHover;
            public override Color SeparatorDark => Theme.Border;
            public override Color SeparatorLight => Theme.Border;
        }

        // ---------------------------------------------------------------- window behaviour

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.DarkTitleBar(Handle);
        }

        protected override void SetVisibleCore(bool value)
        {
            // start hidden in the notification area without flashing the window
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
                _tray.ShowBalloonTip(3000, L.StartedTitle, L.StartedBody, ToolTipIcon.Info);
            };
            once.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!Store.TrayHintShown)
                {
                    _tray.ShowBalloonTip(3000, L.BackgroundTitle, L.BackgroundBody, ToolTipIcon.Info);
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
            _tray.ShowBalloonTip(1500, "KaitoLight", paused ? L.PausedToast : L.ResumedToast, ToolTipIcon.None);
            if (paused) _enforcer.RestoreAll();
            _cardsKey = "";
            Tick();
        }

        private void SwitchLanguage()
        {
            Store.Lang = L.Ar ? "en" : "ar";
            Store.Save();
            L.Init(Store.Lang);
            MessageBox.Show(this, L.RestartForLanguage, "KaitoLight", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try
            {
                Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--show --restart") { UseShellExecute = true });
            }
            catch
            {
                return;   // couldn't relaunch (UAC declined): keep running in the old language
            }
            ExitApp();
        }

        // ---------------------------------------------------------------- games

        private void Tick()
        {
            _enforcer.Scan();
            if (Visible) RefreshCards();
        }

        /// <summary>Running tab: live windows. All tab: every remembered game, live where it's open.</summary>
        private List<GameWindow> VisibleGames()
        {
            var running = _enforcer.Games;
            if (!ShowingAll) return running.ToList();

            var byExe = running.GroupBy(g => g.ExeName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            return Store.Known
                .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                .Select(k => byExe.TryGetValue(k.Key, out var live)
                    ? live
                    : new GameWindow
                    {
                        ExeName = k.Key,
                        ExePath = k.Value,
                        Title = PrettyName(k.Value, k.Key),
                        State = GameState.NotRunning
                    })
                .ToList();
        }

        /// <summary>Use the exe's product name ("Terraria") rather than the file name when we have it.</summary>
        private static string PrettyName(string path, string fallback)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                var name = string.IsNullOrWhiteSpace(info.ProductName) ? info.FileDescription : info.ProductName;
                return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
            }
            catch
            {
                return fallback;
            }
        }

        private void RefreshCards()
        {
            var games = VisibleGames();
            _tabs.Items = new[] { L.Count(L.TabRunning, _enforcer.Games.Count), L.Count(L.TabAll, Store.Known.Count) };
            _tabs.Invalidate();

            var key = (ShowingAll ? "all:" : "run:") + string.Join("|", games.Select(g => g.ExeName + "@" + g.Hwnd.ToInt64()));
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

            if (games.Count == 0) _list.Controls.Add(_empty);

            foreach (var game in games)
            {
                var card = new GameCard(game) { GameIcon = IconFor(game.ExePath), Margin = new Padding(0, 0, 0, 10) };
                card.ToggleClicked += (s, e) => ToggleGame(game);
                card.CardClicked += (s, e) => OpenGame(game);
                card.MenuRequested += (s, e) => ShowCardMenu(card, game, e.Location);
                new ToolTip().SetToolTip(card, game.ExeName + ".exe — " + (game.State == GameState.NotRunning ? L.CardTipLaunch : L.CardTip));
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

        private void OpenGame(GameWindow game)
        {
            if (game.State != GameState.NotRunning)
            {
                _enforcer.Focus(game);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(game.ExePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(game.ExePath) ?? "",
                    UseShellExecute = true
                });
            }
            catch
            {
                // file moved or launch refused
            }
        }

        private void ShowCardMenu(Control card, GameWindow game, Point at)
        {
            var menu = new ContextMenuStrip
            {
                RightToLeft = L.Rtl,
                Renderer = new ToolStripProfessionalRenderer(new MenuColors()),
                BackColor = Theme.Card,
                Font = Theme.Font(10),
                ShowImageMargin = false
            };
            menu.Items.Add(L.OpenFolder, null, (s, e) =>
            {
                try { Process.Start("explorer.exe", "/select,\"" + game.ExePath + "\""); } catch { }
            });
            if (game.State == GameState.NotRunning)
            {
                menu.Items.Add(L.RemoveFromList, null, (s, e) =>
                {
                    Store.Forget(game.ExeName);
                    Store.Save();
                    _cardsKey = "";
                    RefreshCards();
                });
            }
            foreach (ToolStripItem item in menu.Items) item.ForeColor = Theme.Text;
            menu.Closed += (s, e) => BeginInvoke(new Action(menu.Dispose));
            menu.Show(card, at);
        }

        private void ToggleGame(GameWindow game)
        {
            if (Store.Skipped.Remove(game.ExeName))
            {
                if (game.Hwnd != IntPtr.Zero) _enforcer.Enforce(game);
            }
            else
            {
                Store.Skipped.Add(game.ExeName);
                if (game.Hwnd != IntPtr.Zero)
                {
                    _enforcer.Restore(game);
                    game.State = GameState.Off;
                }
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
                Store.Remember(pick.Picked.ExeName, pick.Picked.ExePath);
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
                _icons[exePath] = icon = File.Exists(exePath) ? Enforcer.IconFor(exePath) : null;
            }
            return icon;
        }

        // ---------------------------------------------------------------- icons

        /// <summary>The real app.ico embedded in the exe (sized for the tray).</summary>
        private static Icon LoadAppIcon()
        {
            try
            {
                using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("KaitoLight.app.ico"))
                {
                    return s == null ? null : new Icon(s, SystemInformation.SmallIconSize);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Flat tile with four corner brackets ("fullscreen"); gray variant marks the service as off.</summary>
        internal static Icon MakeAppIcon(bool off)
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
                    using (var pen = new Pen(Color.FromArgb(235, 242, 241), 5f)
                    {
                        StartCap = System.Drawing.Drawing2D.LineCap.Round,
                        EndCap = System.Drawing.Drawing2D.LineCap.Round,
                        LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                    })
                    {
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
