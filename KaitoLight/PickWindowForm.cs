using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>"My game isn't listed": pick it from the open windows; it's remembered from then on.</summary>
    internal class PickWindowForm : Form
    {
        private readonly ListBox _list;
        private readonly Func<string, Icon> _iconFor;

        public GameWindow Picked { get; private set; }

        public PickWindowForm(List<GameWindow> windows, Func<string, Icon> iconFor)
        {
            _iconFor = iconFor;

            Text = "أضف لعبتك";
            ClientSize = new Size(480, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = Theme.Font(10);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            var hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(16, 12, 16, 0),
                Text = "تأكد إن لعبتك شغّالة، واختارها من القائمة.\nبنتذكرها ونحوّلها Borderless كل مرة تفتحها.",
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.MiddleRight
            };

            _list = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 46,
                IntegralHeight = false
            };
            _list.DrawItem += DrawItem;
            _list.DoubleClick += (s, e) => Accept();
            foreach (var w in windows) _list.Items.Add(w);
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(16, 12, 16, 12) };
            var ok = new FlatButton
            {
                Text = "✔  هذي لعبتي",
                Font = Theme.Font(11, FontStyle.Bold),
                ForeColor = Color.White,
                Fill = Theme.Violet,
                FillHover = Color.FromArgb(150, 120, 255),
                Dock = DockStyle.Right,
                Width = 180
            };
            ok.Click += (s, e) => Accept();
            var cancel = new FlatButton
            {
                Text = "إلغاء",
                ForeColor = Theme.Muted,
                Dock = DockStyle.Left,
                Width = 110
            };
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(ok);
            bottom.Controls.Add(cancel);

            var listWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 0) };
            listWrap.Controls.Add(_list);

            Controls.Add(listWrap);
            Controls.Add(bottom);
            Controls.Add(hint);

            if (windows.Count == 0)
            {
                hint.Text = "ما لقينا نوافذ مفتوحة.\nشغّل لعبتك أول، وبعدين ارجع هنا.";
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.DarkTitleBar(Handle);
        }

        private void Accept()
        {
            Picked = _list.SelectedItem as GameWindow;
            if (Picked == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var w = (GameWindow) _list.Items[e.Index];
            var g = e.Graphics;
            Theme.Smooth(g);
            var selected = (e.State & DrawItemState.Selected) != 0;

            using (var back = new SolidBrush(Theme.Card)) g.FillRectangle(back, e.Bounds);
            var r = new RectangleF(e.Bounds.X + 4, e.Bounds.Y + 3, e.Bounds.Width - 8, e.Bounds.Height - 6);
            if (selected)
            {
                using (var p = Theme.Round(r, 10))
                using (var b = new SolidBrush(Theme.Violet))
                {
                    g.FillPath(b, p);
                }
            }

            var icon = _iconFor(w.ExePath);
            if (icon != null) g.DrawIcon(icon, new Rectangle(e.Bounds.Right - 44, e.Bounds.Y + 7, 32, 32));

            var text = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 64, e.Bounds.Height);
            var flags = TextFormatFlags.Right | TextFormatFlags.RightToLeft | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, w.DisplayName, Theme.Font(10.5f, FontStyle.Bold),
                new Rectangle(text.X, text.Y + 4, text.Width, 20), Color.White, flags);
            TextRenderer.DrawText(g, w.ExeName + ".exe", Theme.Font(8.5f),
                new Rectangle(text.X, text.Y + 24, text.Width, 18), selected ? Color.FromArgb(225, 225, 255) : Theme.Muted, flags);
        }
    }
}
