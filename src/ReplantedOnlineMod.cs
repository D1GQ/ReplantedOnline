using Il2CppInterop.Runtime.Injection;
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Items.Attributes;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Online;
using UnityEngine;

namespace ReplantedOnline;

internal class ReplantedOnlineMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.PatchAll();
        InstanceAttribute.RegisterAll();
        RegisterAllMonoBehavioursInAssembly();
        NetworkClass.SetupPrefabs();
    }

    public override void OnUpdate()
    {
        if (!loaded) return;

        NetworkDispatcher.Update();
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

            // Must make sure Il2CppSteamworks has initialized!
            if (!SteamClient.initialized)
            {
                SteamClient.Init(3654560);
            }

            NetLobby.Initialize();
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
}
