using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Versus.Plants;

[HarmonyPatch]
internal static class ChomperPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool Find_Prefix(Plant __instance)
    {
        if (__instance.mSeedType is not SeedType.Chomper) return true;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // If player is NOT on the plant side
            if (!VersusState.AmPlantSide)
            {
                // The chomper targeting/animations will be handled by network code
                return false;
            }
        }

        return true;
    }
}