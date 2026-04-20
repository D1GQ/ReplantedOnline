using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class ChomperPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool Plant_FindTargetZombie_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Chomper) return true;

        // Check if we're in an online multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            // If player is NOT on the plant side
            if (!VersusState.AmPlantSide)
            {
                // The chomper animations will be handled by PlantNetworked.cs
                return false;
            }
        }

        return true;
    }
}