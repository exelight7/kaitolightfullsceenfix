using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KaitoLight
{
    /// <summary>Decides whether an exe is a game: store install folder, engine files next to it, or engine naming.</summary>
    internal static class GameDetector
    {
        private static readonly ConcurrentDictionary<string, bool> Cache =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] GameFolders =
        {
            @"\steamapps\common\", @"\epic games\", @"\gog games\", @"\gog galaxy\games\", @"\riot games\",
            @"\ubisoft game launcher\games\", @"\ubisoft\ubisoft game launcher\games\", @"\ea games\",
            @"\origin games\", @"\xboxgames\", @"\battle.net\", @"\rockstar games\", @"\amazon games\library\",
            @"\itch\apps\", @"\games\", @"\roblox\versions\"
        };

        // Launchers, overlays and helpers that live in game folders but should never be touched
        private static readonly HashSet<string> NeverGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "steam", "steamwebhelper", "steamservice", "epicgameslauncher", "epicwebhelper", "galaxyclient",
            "galaxyclient helper", "riotclientservices", "riotclientux", "riotclientuxrender", "leagueclient",
            "leagueclientux", "leagueclientuxrender", "upc", "ubisoftconnect", "ubisoftgamelauncher", "eadesktop",
            "eabackgroundservice", "origin", "battle.net", "agent", "rockstarlauncher", "launcher",
            "socialclubhelper", "playnite.desktopapp", "playnite.fullscreenapp", "gogalaxy", "amazon games",
            "itch", "unitycrashhandler64", "unitycrashhandler32", "crashreportclient", "crashpad_handler",
            "easyanticheat", "easyanticheat_eos", "easyanticheat_eos_setup", "beservice", "vanguard",
            "explorer", "kaitolight", "borderlessgaming", "gamebar", "gamebarftserver", "xboxapp", "xboxpcapp",
            "discord", "obs64", "obs32", "nvidia share", "nvcontainer", "radeonsoftware", "amdrsserv",
            "lghub", "wallpaper32", "wallpaper64", "robloxstudiobeta", "robloxplayerlauncher", "applicationframehost"
        };

        private static readonly string[] EngineFiles =
        {
            "UnityPlayer.dll", "GameAssembly.dll", "steam_api64.dll", "steam_api.dll", "EOSSDK-Win64-Shipping.dll",
            "bink2w64.dll", "binkw64.dll", "bink2w32.dll", "binkw32.dll", "fmod.dll", "fmodex64.dll", "fmodstudio.dll",
            "PhysX3_x64.dll", "PhysX_64.dll", "galaxy64.dll", "Galaxy.dll", "d3dx9_43.dll", "xinput1_3.dll",
            "openvr_api.dll", "discord_game_sdk.dll", "sdl2.dll", "SDL3.dll",
            "Engine.dll", "nvngx_dlss.dll", "amd_fidelityfx_dx12.dll", "libxess.dll", "nw.dll"
        };

        public static bool IsNeverGame(string exeName)
        {
            return NeverGames.Contains(exeName);
        }

        public static bool IsGame(string exePath)
        {
            if (string.IsNullOrEmpty(exePath)) return false;
            return Cache.GetOrAdd(exePath, Detect);
        }

        private static bool Detect(string exePath)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(exePath);
                if (IsNeverGame(name)) return false;

                var lower = exePath.ToLowerInvariant();
                if (lower.Contains(@"\windows\") || lower.Contains(@"\windowsapps\microsoft.")) return false;

                if (name.EndsWith("-Shipping", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("-Win64-Shipping", StringComparison.OrdinalIgnoreCase)) return true;

                if (GameFolders.Any(lower.Contains)) return true;

                var dir = Path.GetDirectoryName(exePath);
                if (dir == null) return false;

                if (EngineFiles.Any(f => File.Exists(Path.Combine(dir, f)))) return true;
                if (Directory.Exists(Path.Combine(dir, name + "_Data"))) return true;           // Unity
                if (File.Exists(Path.Combine(dir, name + ".pck"))) return true;                 // Godot
                if (File.Exists(Path.Combine(dir, "data.win"))) return true;                    // GameMaker
                if (Directory.Exists(Path.Combine(dir, "www")) &&
                    File.Exists(Path.Combine(dir, "www", "js", "rpg_core.js"))) return true;   // RPG Maker MV

                var parent = Directory.GetParent(dir);
                // Unreal: <Game>\Binaries\Win64\<exe>
                if (parent != null && dir.EndsWith(@"\Binaries\Win64", StringComparison.OrdinalIgnoreCase)) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
