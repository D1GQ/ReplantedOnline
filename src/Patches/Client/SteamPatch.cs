using HarmonyLib;
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;

namespace ReplantedOnline.Patches.Client;

[HarmonyPatch]
internal class SteamPatch
{
    internal static AppIdServers AppServer { get; private set; }

    [HarmonyPatch(typeof(SteamClient), nameof(SteamClient.Init))]
    [HarmonyPrefix]
    private static void SteamClient_Init_Prefix(ref uint appid)
    {
        BloomEngineManager.InitializeMelon();
        appid = BloomEngineManager.m_gameServer.Value;
        AppServer = (AppIdServers)BloomEngineManager.m_gameServer.Value;
    }
}
