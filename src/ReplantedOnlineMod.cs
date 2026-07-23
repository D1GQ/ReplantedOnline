#pragma warning disable CS0162

using MelonLoader;
using ReplantedOnline.Attributes.Hook;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Discord;
using ReplantedOnline.Network.Github;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Patches.Misc;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus;
using ReplantedOnline.Utilities.MelonLoader;
using UnityEngine;

namespace ReplantedOnline;

/// <summary>
/// The main class for the Replanted Online mod.
/// </summary>
internal partial class ReplantedOnlineMod : MelonMod
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

        if (Instances.GameplayActivity?.SeedChooserScreen != null)
        {
            SeedChooserPatch.UpdateSeedChooserScreen(Instances.GameplayActivity.SeedChooserScreen);
        }
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
}