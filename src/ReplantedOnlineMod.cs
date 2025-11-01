using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Items.Attributes;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline;

internal class ReplantedOnlineMod : MelonMod
{
    internal static readonly HarmonyLib.Harmony _Harmony = new(ModInfo.ModGUID);

    public override void OnInitializeMelon()
    {
        InstanceAttribute.RegisterAll();
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
}
