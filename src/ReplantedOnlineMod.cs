using Il2CppInterop.Runtime.Injection;
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Client.UI;
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
        NetworkClass.SetupPrefabs();
        BloomEngineManager.InitializeBloom(this);
        InfoDisplay.Initialize();
        Application.runInBackground = true;
    }

    private void OnInitializeMainMenu()
    {
        if (!SteamClient.initialized)
            SteamClient.Init(0);
        LevelEntries.Initialize();
        PlantDefinitions.Initialize();
        ContentManager.Initialize();
        NetLobby.Initialize();
    }

    public override void OnUpdate()
    {
        if (!loaded) return;

        NetworkDispatcher.Update();
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
        internal const int MAX_NETWORK_CHILDREN = 25;
    }
}