using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>Tiny settings file: %AppData%\KaitoLight\settings.txt (one "key=value" per line).</summary>
    internal static class Store
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KaitoLight");
        private static readonly string FilePath = Path.Combine(Dir, "settings.txt");

        /// <summary>Exe names (no extension) the user added by hand.</summary>
        public static readonly HashSet<string> Games = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Exe names the user switched off.</summary>
        public static readonly HashSet<string> Skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every game we've recognised: exe name → full exe path (for the "All games" list).</summary>
        public static readonly Dictionary<string, string> Known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static bool Paused { get; set; }
        public static bool TrayHintShown { get; set; }
        public static string Lang { get; set; } = "";

        /// <summary>Remember a game; returns true if it was new (caller decides when to save).</summary>
        public static bool Remember(string exeName, string exePath)
        {
            if (string.IsNullOrEmpty(exeName) || string.IsNullOrEmpty(exePath)) return false;
            if (Known.TryGetValue(exeName, out var old) && string.Equals(old, exePath, StringComparison.OrdinalIgnoreCase)) return false;
            Known[exeName] = exePath;
            return true;
        }

        public static void Forget(string exeName)
        {
            Known.Remove(exeName);
            Games.Remove(exeName);
            Skipped.Remove(exeName);
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var i = line.IndexOf('=');
                    if (i <= 0) continue;
                    var key = line.Substring(0, i).Trim();
                    var val = line.Substring(i + 1).Trim();
                    switch (key)
                    {
                        case "game": Games.Add(val); break;
                        case "skip": Skipped.Add(val); break;
                        case "paused": Paused = val == "1"; break;
                        case "trayhint": TrayHintShown = val == "1"; break;
                        case "lang": Lang = val; break;
                        case "known":
                            var bar = val.IndexOf('|');
                            if (bar > 0) Known[val.Substring(0, bar)] = val.Substring(bar + 1);
                            break;
                    }
                }
            }
            catch
            {
                // start fresh on a broken file
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var lines = new List<string>
                {
                    "paused=" + (Paused ? "1" : "0"),
                    "trayhint=" + (TrayHintShown ? "1" : "0"),
                    "lang=" + Lang
                };
                lines.AddRange(Known.Select(k => "known=" + k.Key + "|" + k.Value));
                lines.AddRange(Games.Select(g => "game=" + g));
                lines.AddRange(Skipped.Select(g => "skip=" + g));
                File.WriteAllLines(FilePath, lines);
            }
            catch
            {
                // ignored
            }
        }

        // ---- Start with Windows (scheduled task, so it can start elevated without a UAC prompt) ----

        private const string TaskName = "KaitoLight";

        public static bool StartsWithWindows()
        {
            return RunSchtasks("/Query /TN \"" + TaskName + "\"") == 0;
        }

        public static void SetStartWithWindows(bool on)
        {
            if (on)
            {
                var exe = Application.ExecutablePath;
                RunSchtasks("/Create /F /TN \"" + TaskName + "\" /SC ONLOGON /RL HIGHEST /TR \"\\\"" + exe + "\\\" --tray\"");
            }
            else
            {
                RunSchtasks("/Delete /F /TN \"" + TaskName + "\"");
            }
        }

        private static int RunSchtasks(string args)
        {
            try
            {
                using (var p = Process.Start(new ProcessStartInfo("schtasks.exe", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }))
                {
                    p.WaitForExit(5000);
                    return p.ExitCode;
                }
            }
            catch
            {
                return -1;
            }
        }
    }
}
