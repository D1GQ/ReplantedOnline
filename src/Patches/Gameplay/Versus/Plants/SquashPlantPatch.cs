using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class SquashPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindSquashTarget))]
    [HarmonyPrefix]
    private static bool Plant_FindSquashTarget_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Squash) return true;

        // Check if we're in an online multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            // If player is NOT on plant side
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }
}