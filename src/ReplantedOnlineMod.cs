#pragma warning disable CS0162

using MelonLoader;
using ReplantedOnline.Attributes.Hook;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Discord;
using ReplantedOnline.Network.Github;
using ReplantedOnline.Patches.Misc;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities.MelonLoader;
using UnityEngine;

namespace ReplantedOnline;

internal class ReplantedOnlineMod : MelonMod
{
    private static readonly HarmonyLib.Harmony harmony = new(ModInfo.MOD_GUID);

    internal static MelonLogger.Instance Logger { get; } = new(ModInfo.MOD_NAME.Replace(" ", ""));
    internal static MelonLogger.Instance DebugLogger { get; } = new(ModInfo.MOD_NAME.Replace(" ", "") + "Debug");

    internal static bool LoadFailed { get; private set; }

    internal ReplantedOnlineMod()
    {
        bool hasVer = false;
        foreach (var ver in ModInfo.Replanted.SUPPORTED_VERSIONS.Split(", "))
        {
            if (Application.version == ver)
            {
                hasVer = true;
                break;
            }

            if (ver.EndsWith("*") && Application.version.StartsWith(ver[..^1]))
            {
                hasVer = true;
                break;
            }
        }

        if (!hasVer)
        {
            Logger.Error(typeof(ReplantedOnlineMod), $"Plants vs. Zombies: Replanted v{Application.version} does not support Replanted Online v{ModInfo.MOD_VERSION_FORMATTED}");
            LoadFailed = true;
        }
    }

    public override void OnInitializeMelon()
    {
        if (LoadFailed == true) return;

        try
        {
            DependencyResolver.Initialize();
            harmony.PatchAll();
            DebugLoggerPatch.Patch(harmony);
            Il2CppInteropExceptionLogPatch.Patch(harmony);
            DetourHookAttribute.InstallAll();
            NativeDetourHook.InstallAll();
            AutoRegisterAttribute.RegisterAll();
            NetworkObject.InitializePrefabs();
            RpcHandlerAttribute.Initialize();
            BloomEngineManager.InitializeBloom(this);
            MonoSingleton<InfoDisplay>.CreateInstance();
            MonoSingleton<GithubAPI>.CreateInstance();
            AudioManager.Initialize();
            DiscordManager.Initialize();
            Application.runInBackground = true;
        }
        catch (Exception ex)
        {
            LoadFailed = true;
            Logger.BigError(typeof(ReplantedOnlineMod), ex.ToString());
        }
    }

    public override void OnLateInitializeMelon()
    {
        if (LoadFailed == true) return;

        // Fix constant "Memory Access Violations" on older versions of Unity Explorer!
        // I personally prefer using this older version because it allows to access custom classes https://github.com/sinai-dev/UnityExplorer
        try
        {
            UniverseLibPatch.Patch(harmony);
        }
        catch (Exception ex)
        {
            LoadFailed = true;
            Logger.BigError(typeof(ReplantedOnlineMod), ex.ToString());
        }
    }

    private static void OnInitializeMainMenu()
    {
        if (LoadFailed == true) return;

        try
        {
            LevelEntries.Initialize();
            SeedPacketDefinitions.Initialize();
            ContentManager.Initialize();
            MonoSingleton<MainThreadDispatcher>.CreateInstance();
            ReloadedLobby.Initialize();
            DiscordManager.ReadyToJoin();
        }
        catch (Exception ex)
        {
            LoadFailed = true;
            Logger.BigError(typeof(ReplantedOnlineMod), ex.ToString());
        }
    }

    public override void OnUpdate()
    {
        if (LoadFailed == true) return;
        if (!loaded) return;

        DiscordManager.Update();
        LobbyCodePanel.ValidateText();
    }

    public override void OnApplicationQuit()
    {
        if (ReloadedLobby.AmInLobby())
        {
            ReloadedLobby.LeaveLobby();
        }

        DiscordManager.Dispose();
    }

    // Delayed initialized for BootStrap sequence...
    // For some reason the game likes to occasionally black screen if not delayed ¯\_(ツ)_/¯
    private bool loaded;
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Frontend")
        {
            // If the game some how goes to main menu while in the lobby leave it
            if (ReloadedLobby.AmInLobby())
            {
                ReloadedLobby.LeaveLobby();
            }

            if (loaded) return;
            loaded = true;
            OnInitializeMainMenu();
        }
    }

    private static readonly string[] _authorizedMods = [ModInfo.MOD_NAME, ModInfo.BloomEngine.BLOOM_ENGINE_NAME];
    internal static bool HasUnauthorizedMod(out MelonMod melonMod)
    {
#if DEBUG
        melonMod = null!;
        return false;
#endif

        foreach (var mod in RegisteredMelons)
        {
            if (!_authorizedMods.Contains(mod.Info.Name))
            {
                melonMod = mod;
                return true;
            }
        }

        melonMod = null!;
        return false;
    }

    internal static class Constants
    {
        internal static class Network
        {
            internal const int MAX_NETWORK_CHILDREN = 5;
            internal const string MOD_VERSION_KEY = "mod_version";
            internal const string GAME_CODE_KEY = "game_code";

            internal static class ObjectStates
            {
                internal const string NULL_STATE = "null";
                internal const string UPDATE_STATE = "update";

                internal const string ZOMBIE_MIND_CONTROLLED_STATE = "mind_controlled";
                internal const string ZOMBIE_CHEWING_PLANT_STATE = "chewing_plant";
                internal const string ZOMBIE_MOWED_DOWN_STATE = "mowed_down";

                internal const string GARGANTUAR_SMASH_STATE = "gargantuar_smash";
                internal const string GARGANTUAR_TARGET_STATE = "gargantuar_target";

                internal const string LADDER_ZOMBIE_PLACED_LADDER = "placed_ladder";

                internal const string CHOMPER_BITE_STATE = "chomper_bite";
            }
        }

        internal static class Reloaded
        {
            internal const int DEFAULT_PLAYER_INDEX = -1;
            internal const int LOCAL_PLAYER_INDEX = 0;
            internal const int OPPONENT_PLAYER_INDEX = 1;
            internal const float X2_SEEDPACKET_COOLDOWN_TIME_END = 60f;
            internal const string REDIRECT_ALMANAC_PREFIX = "ALMANAC-REDIRECT:";

            internal static class Production
            {
                internal static readonly IntTime INITIAL_SKY_RATE = 12f;
                internal static readonly (IntTime MIN, IntTime MAX) INITIAL_PLANT_OR_GRAVE_RATE_RANGE = (8f, 12f);
                internal static readonly IntTime SKY_RATE = 22f;
                internal static readonly IntTime PLANT_RATE = 14f;
                internal static readonly IntTime GRAVE_RATE = 34f;
            }
        }
    }
}