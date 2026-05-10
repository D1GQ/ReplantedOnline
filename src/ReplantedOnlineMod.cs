using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Modded;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Modules.Il2cpp;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.MonoScripts.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Github;
using ReplantedOnline.Patches.Misc;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities.MelonLoader;
using System.Reflection;
using UnityEngine;

namespace ReplantedOnline;

internal class ReplantedOnlineMod : MelonMod
{
    internal static HarmonyLib.Harmony harmony = new(ModInfo.MOD_GUID);

    internal static MelonLogger.Instance Logger { get; } = new(ModInfo.MOD_NAME.Replace(" ", ""));
    internal static MelonLogger.Instance DebugLogger { get; } = new(ModInfo.MOD_NAME.Replace(" ", "") + "Debug");

    public override void OnInitializeMelon()
    {
        File.WriteAllText("steam_appid.txt", ((uint)AppIds.PVZ_Replanted).ToString());
        harmony.PatchAll();
        DebugLoggerPatch.Patch();
        AutoRegisterAttribute.RegisterAll();
        RegisterAllMonoBehavioursInAssembly();
        Il2cppEnumeratorWrapper.Register();
        NetworkObject.SetupPrefabs();
        RpcHandlerAttribute.Initialize();
        BloomEngineManager.InitializeBloom(this);
        MonoSingleton<InfoDisplay>.CreateInstance();
        MonoSingleton<GithubAPI>.CreateInstance();
        AudioManager.Initialize();
        Application.runInBackground = true;
    }

    public override void OnLateInitializeMelon()
    {
        // Fix constant "Memory Access Violations" on older versions of Unity Explorer!
        // I personally prefer using this older version because it allows to access custom classes https://github.com/sinai-dev/UnityExplorer
        UniverseLibPatch.Patch();
    }

    private static void OnInitializeMainMenu()
    {
        if (!SteamClient.initialized)
        {
            SteamClient.Init((uint)AppIds.PVZ_Replanted);
        }

        LevelEntries.Initialize();
        SeedPacketDefinitions.Initialize();
        ContentManager.Initialize();
        MonoSingleton<MainThreadDispatcher>.CreateInstance();
        ReloadedLobby.Initialize();
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

        LobbyCodePanel.ValidateText();
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
                Logger.Error(typeof(ReplantedOnlineMod), $"Failed to register MonoBehaviour: {type.FullName}\n{ex}");
            }
        }
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