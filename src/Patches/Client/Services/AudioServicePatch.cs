using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Client.Services;

[HarmonyPatch]
internal static class AudioServicePatch
{
    [HarmonyPatch(typeof(AudioService), nameof(AudioService.PlayFoley), [typeof(FoleyType)])]
    [HarmonyPrefix]
    private static bool AudioService_PlaySample_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (VersusState.VersusPhase is VersusPhase.Gameplay && VersusState.IsInCountDown)
            {
                // Prevent jalapeno foley from playing in ChinaJalapenoNetworkComponent.cs
                return false;
            }
        }

        return true;
    }
}
