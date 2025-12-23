using HarmonyLib;
using Il2Cpp;
using Il2CppReloaded.TreeStateActivities;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Client;

[HarmonyPatch]
internal static class PausePatch
{
    [HarmonyPatch(typeof(PauseGameActivity), nameof(PauseGameActivity.ActiveStarted))]
    [HarmonyPatch(typeof(PauseMusicActivity), nameof(PauseMusicActivity.ActiveStarted))]
    [HarmonyPatch(typeof(PauseGameActivity), nameof(PauseGameActivity.InactiveStarted))]
    [HarmonyPatch(typeof(PauseMusicActivity), nameof(PauseMusicActivity.InactiveStarted))]
    [HarmonyPrefix]
    private static bool Bulk_Pause_Prefix()
    {
        if (NetLobby.AmInLobby())
        {
            // This prevents the game from being paused in multiplayer contexts
            return false;
        }

        return true;
    }
}