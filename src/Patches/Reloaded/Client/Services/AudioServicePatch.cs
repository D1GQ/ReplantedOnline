using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;

namespace ReplantedOnline.Patches.Reloaded.Client.Services;

[HarmonyPatch]
internal static class AudioServicePatch
{
    [HarmonyPatch(typeof(AudioService), nameof(AudioService.PlayFoley), [typeof(FoleyType)])]
    [HarmonyPrefix]
    private static bool AudioService_PlaySample_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.VersusPhase is VersusPhase.Gameplay && VersusState.IsInPreCountDown)
            {
                // Prevent jalapeno foley from playing in ChinaJalapenoNetworkComponent.cs
                return false;
            }
        }

        return true;
    }
}
