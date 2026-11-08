using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessGaming.Logic.Core
{
    /// <summary>
    ///     Guesses whether a running process is a game, from where its exe lives
    ///     and which engine/runtime files sit next to it. Results are cached per process id.
    /// </summary>
    public static class GameDetector
    {
        private static readonly ConcurrentDictionary<int, bool> Cache = new ConcurrentDictionary<int, bool>();

        // Folders that game stores install games into
        private static readonly string[] GameFolders =
        {
            @"\steamapps\common\", @"\epic games\", @"\gog games\", @"\gog galaxy\games\",
            @"\riot games\", @"\ubisoft game launcher\games\", @"\ea games\", @"\origin games\",
            @"\xboxgames\", @"\battle.net\games\", @"\rockstar games\", @"\games\"
        };

        // Launchers, helpers and anti-cheat that live in the same folders but are not games
        private static readonly HashSet<string> NotGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "steam", "steamwebhelper", "steamservice", "epicgameslauncher", "epicwebhelper", "galaxyclient",
            "galaxyclient helper", "riotclientservices", "riotclientux", "riotclientuxrender", "upc",
            "ubisoftconnect", "ubisoftgamelauncher", "eadesktop", "eabackgroundservice", "origin",
            "battle.net", "launcher", "rockstarlauncher", "socialclubhelper", "unitycrashhandler64",
            "unitycrashhandler32", "crashreportclient", "easyanticheat", "easyanticheat_eos", "beservice",
            "explorer", "borderlessgaming", "gamebar", "xboxapp", "xboxpcapp"
        };

        // Engine/runtime files that strongly suggest a game
        private static readonly string[] EngineFiles =
        {
            "UnityPlayer.dll", "GameAssembly.dll", "steam_api64.dll", "steam_api.dll",
            "EOSSDK-Win64-Shipping.dll", "bink2w64.dll", "binkw64.dll", "fmod.dll", "fmodex64.dll",
            "PhysX3_x64.dll", "galaxy64.dll", "nw.dll"
        };

        public static bool IsGame(int processId, string processName)
        {
            return Cache.GetOrAdd(processId, _ => Detect(processId, processName));
        }

        private static bool Detect(int processId, string processName)
        {
            try
            {
                if (string.IsNullOrEmpty(processName) || NotGames.Contains(processName))
                {
                    return false;
                }

                // Unreal Engine shipping builds
                if (processName.EndsWith("-Shipping", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var exePath = GetExePath(processId);
                if (string.IsNullOrEmpty(exePath))
                {
                    return false;
                }

                var lower = exePath.ToLowerInvariant();
                if (lower.Contains(@"\windows\") || lower.Contains(@"\microsoft\"))
                {
                    return false;
                }

                if (GameFolders.Any(lower.Contains))
                {
                    return true;
                }

                var dir = Path.GetDirectoryName(exePath);
                if (dir == null)
                {
                    return false;
                }

                if (EngineFiles.Any(f => File.Exists(Path.Combine(dir, f))))
                {
                    return true;
                }

                // Unity games ship a "<ExeName>_Data" folder
                return Directory.Exists(Path.Combine(dir, Path.GetFileNameWithoutExtension(exePath) + "_Data"));
            }
            catch
            {
                return false;
            }
        }

        private const uint ProcessQueryLimitedInformation = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

        // Works across 32/64-bit boundaries, unlike Process.MainModule
        private static string GetExePath(int processId)
        {
            var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (handle == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                var sb = new StringBuilder(1024);
                var size = sb.Capacity;
                return QueryFullProcessImageName(handle, 0, sb, ref size) ? sb.ToString() : null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
    }
}
