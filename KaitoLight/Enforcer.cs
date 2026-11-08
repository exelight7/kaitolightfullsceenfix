using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KaitoLight
{
    internal enum GameState
    {
        Borderless,   // we forced it and it is staying borderless
        Waiting,      // minimized / loading
        Off,          // user switched this game off
        NotRunning    // a remembered game that isn't open right now
    }

    internal class GameWindow
    {
        public IntPtr Hwnd;
        public int Pid;
        public string ExePath;
        public string ExeName;
        public string Title;
        public GameState State;
        public int Forces;                 // how many times we had to (re)apply
        public DateTime LastForce;

        // what the window looked like before we touched it
        public bool Saved;
        public long OrigStyle;
        public long OrigExStyle;
        public Native.RECT OrigRect;

        public string DisplayName => string.IsNullOrWhiteSpace(Title) ? ExeName : Title.Trim();
    }

    /// <summary>
    ///     Finds game windows and keeps them borderless-fullscreen. If a game puts its border back
    ///     (resolution change, alt-enter, loading screen), it is forced again on the next scan.
    /// </summary>
    internal class Enforcer
    {
        private readonly Dictionary<IntPtr, GameWindow> _games = new Dictionary<IntPtr, GameWindow>();
        private readonly int _selfPid = Process.GetCurrentProcess().Id;

        public IReadOnlyList<GameWindow> Games => _games.Values.OrderBy(g => g.ExeName).ToList();

        /// <summary>Called on a timer from the UI thread.</summary>
        public bool Scan()
        {
            var changed = false;
            var learned = false;
            var seen = new HashSet<IntPtr>();

            foreach (var w in FindGameWindows())
            {
                seen.Add(w.Hwnd);
                learned |= Store.Remember(w.ExeName, w.ExePath);
                if (!_games.TryGetValue(w.Hwnd, out var g))
                {
                    _games[w.Hwnd] = g = w;
                    changed = true;
                }
                else if (g.Title != w.Title)
                {
                    g.Title = w.Title;
                    changed = true;
                }

                var before = g.State;
                Enforce(g);
                changed |= before != g.State;
            }

            foreach (var gone in _games.Keys.Where(h => !seen.Contains(h)).ToList())
            {
                _games.Remove(gone);
                changed = true;
            }

            if (learned) Store.Save();
            return changed || learned;
        }

        public void Enforce(GameWindow g)
        {
            if (Store.Paused || Store.Skipped.Contains(g.ExeName))
            {
                g.State = GameState.Off;
                return;
            }

            if (!Native.IsWindow(g.Hwnd) || Native.IsIconic(g.Hwnd))
            {
                g.State = GameState.Waiting;
                return;
            }

            var target = Screen.FromHandle(g.Hwnd).Bounds;
            var style = Native.GetStyle(g.Hwnd, Native.GWL_STYLE);
            var ex = Native.GetStyle(g.Hwnd, Native.GWL_EXSTYLE);
            Native.GetWindowRect(g.Hwnd, out var r);

            var hasFrame = (style & Native.FrameStyles) != 0 || (ex & Native.FrameExStyles) != 0;
            var wrongSize = r.Left != target.Left || r.Top != target.Top || r.Width != target.Width || r.Height != target.Height;

            if (!hasFrame && !wrongSize)
            {
                g.State = GameState.Borderless;
                return;
            }

            // don't fight a game that is mid-resize; give it a moment between forces
            if ((DateTime.Now - g.LastForce).TotalMilliseconds < 800) return;

            if (!g.Saved)
            {
                g.OrigStyle = style;
                g.OrigExStyle = ex;
                g.OrigRect = r;
                g.Saved = true;
            }

            if (Native.IsZoomed(g.Hwnd))
            {
                Native.ShowWindow(g.Hwnd, Native.SW_RESTORE);
            }

            Native.SetStyle(g.Hwnd, Native.GWL_STYLE, style & ~Native.FrameStyles);
            Native.SetStyle(g.Hwnd, Native.GWL_EXSTYLE, ex & ~Native.FrameExStyles);
            Native.SetWindowPos(g.Hwnd, IntPtr.Zero, target.Left, target.Top, target.Width, target.Height,
                Native.SWP_FRAMECHANGED | Native.SWP_NOOWNERZORDER | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);

            g.LastForce = DateTime.Now;
            g.Forces++;
            g.State = GameState.Borderless;
        }

        /// <summary>Put a window back the way it was before we touched it.</summary>
        public void Restore(GameWindow g)
        {
            if (!g.Saved || !Native.IsWindow(g.Hwnd)) return;
            Native.SetStyle(g.Hwnd, Native.GWL_STYLE, g.OrigStyle);
            Native.SetStyle(g.Hwnd, Native.GWL_EXSTYLE, g.OrigExStyle);
            var r = g.OrigRect;
            Native.SetWindowPos(g.Hwnd, IntPtr.Zero, r.Left, r.Top, Math.Max(r.Width, 640), Math.Max(r.Height, 360),
                Native.SWP_FRAMECHANGED | Native.SWP_NOOWNERZORDER | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            g.Saved = false;
        }

        public void RestoreAll()
        {
            foreach (var g in _games.Values) Restore(g);
        }

        public void Focus(GameWindow g)
        {
            if (Native.IsIconic(g.Hwnd)) Native.ShowWindow(g.Hwnd, Native.SW_RESTORE);
            Native.SetForegroundWindow(g.Hwnd);
        }

        // ---------------------------------------------------------------------------------------------

        private IEnumerable<GameWindow> FindGameWindows()
        {
            // keep only the biggest window per game process
            var best = new Dictionary<int, (GameWindow w, long area)>();

            foreach (var w in TopLevelWindows(minWidth: 400, minHeight: 300))
            {
                var isGame = Store.Games.Contains(w.ExeName) ||
                             (!GameDetector.IsNeverGame(w.ExeName) && GameDetector.IsGame(w.ExePath));
                if (!isGame) continue;

                Native.GetWindowRect(w.Hwnd, out var r);
                var area = Native.IsIconic(w.Hwnd) ? 1 : (long) r.Width * r.Height;
                if (!best.TryGetValue(w.Pid, out var cur) || area > cur.area)
                {
                    best[w.Pid] = (w, area);
                }
            }

            return best.Values.Select(v => v.w);
        }

        /// <summary>Normal, visible, top-level app windows (used for detection and for the "pick a window" list).</summary>
        public IEnumerable<GameWindow> TopLevelWindows(int minWidth = 200, int minHeight = 150)
        {
            var list = new List<GameWindow>();
            Native.EnumWindows((h, _) =>
            {
                try
                {
                    if (!Native.IsWindowVisible(h) || Native.IsCloaked(h)) return true;
                    if (Native.GetWindow(h, Native.GW_OWNER) != IntPtr.Zero) return true;

                    var style = Native.GetStyle(h, Native.GWL_STYLE);
                    var ex = Native.GetStyle(h, Native.GWL_EXSTYLE);
                    if ((style & Native.WS_CHILD) != 0 || (ex & Native.WS_EX_TOOLWINDOW) != 0 || (ex & Native.WS_EX_NOACTIVATE) != 0) return true;

                    if (!Native.IsIconic(h))
                    {
                        Native.GetWindowRect(h, out var r);
                        if (r.Width < minWidth || r.Height < minHeight) return true;
                    }

                    Native.GetWindowThreadProcessId(h, out var pid);
                    if (pid == _selfPid || pid == 0) return true;

                    var path = Native.GetExePath(pid);
                    if (string.IsNullOrEmpty(path)) return true;

                    list.Add(new GameWindow
                    {
                        Hwnd = h,
                        Pid = pid,
                        ExePath = path,
                        ExeName = Path.GetFileNameWithoutExtension(path),
                        Title = Native.GetTitle(h)
                    });
                }
                catch
                {
                    // skip windows we can't read
                }
                return true;
            }, IntPtr.Zero);
            return list;
        }

        public static Icon IconFor(string exePath)
        {
            try
            {
                return Icon.ExtractAssociatedIcon(exePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
