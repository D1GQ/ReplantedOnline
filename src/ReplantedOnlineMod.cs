using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Patches;
using ReplantedOnline.Patches.Client.UI;
using System.Reflection;
using UnityEngine;

namespace ReplantedOnline;

internal class ReplantedOnlineMod : MelonMod
{
    internal static HarmonyLib.Harmony harmony = new(ModInfo.MOD_GUID);

    public override void OnInitializeMelon()
    {
        File.WriteAllText("steam_appid.txt", ((uint)AppIdServers.PVZ_Replanted).ToString());
        harmony.PatchAll();
        InstanceAttribute.RegisterAll();
        RegisterAllMonoBehavioursInAssembly();
        Il2cppEnumeratorWrapper.Register();
        NetworkObject.SetupPrefabs();
        BloomEngineManager.InitializeBloom(this);
        InfoDisplay.Initialize();
        AudioManager.Initialize();
        Application.runInBackground = true;
    }

    private void OnInitializeMainMenu()
    {
        if (!SteamClient.initialized)
            SteamClient.Init((uint)AppIdServers.PVZ_Replanted);
        LevelEntries.Initialize();
        SeedPacketDefinitions.Initialize();
        ContentManager.Initialize();
        NetLobby.Initialize();
    }

    public override void OnPreSupportModule()
    {
        // Fix Reverse Patches crashing the game
        // Special thanks to https://github.com/RaptorRush135 for the fix
        // https://github.com/LavaGang/MelonLoader/issues/1003
        // https://github.com/LavaGang/MelonLoader/pull/1106
        MethodInfo badPatch = AccessTools.Method("MelonLoader.Fixes.InstancePatchFix:PatchMethod")
            ?? AccessTools.Method("MelonLoader.Fixes.Harmony.InstancePatchFix:PatchMethod");

        if (badPatch == null)
        {
            return;
        }

        HarmonyInstance.Unpatch(AccessTools.Method("HarmonyLib.PatchFunctions:ReversePatch"), badPatch);
        HarmonyInstance.Unpatch(AccessTools.Method("HarmonyLib.HarmonyMethod:ImportMethod"), badPatch);

        Il2CppInteropExceptionLogPatch.Patch(HarmonyInstance);
    }

    public override void OnUpdate()
    {
        if (!loaded) return;

        JoinLobbyCodePanelPatch.ValidateText();
    }

    // Delayed initialized for BootStrap sequence...
    // For some reason the game likes to occasionally black screen if not delayed ¯\_(ツ)_/¯
    private bool loaded;
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Frontend")
        {
            if (loaded) return;
            loaded = true;
            OnInitializeMainMenu();
        }
    }

    /// <summary>
    /// Registers all MonoBehaviour-derived types in the current assembly with IL2CPP for interop support.
    /// </summary>
    internal void RegisterAllMonoBehavioursInAssembly()
    {
        var assembly = MelonAssembly.Assembly;

        var monoBehaviourTypes = assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
            .OrderBy(type => type.Name);

        foreach (var type in monoBehaviourTypes)
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp(type);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to register MonoBehaviour: {type.FullName}\n{ex}");
            }
        }
    }

    internal class Constants
    {
        internal const int DEFAULT_PLAYER_INDEX = -1;
        internal const int LOCAL_PLAYER_INDEX = 0;
        internal const int OPPONENT_PLAYER_INDEX = 1;
        internal const string MOD_VERSION_KEY = "mod_version";
        internal const string GAME_CODE_KEY = "game_code";
        internal const string Heartbeat_KEY = "game_code";
        internal const int MAX_NETWORK_CHILDREN = 5;
    }
}