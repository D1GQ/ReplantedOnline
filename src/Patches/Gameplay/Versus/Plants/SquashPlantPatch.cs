using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;
using ReplantedOnline.Utilities;

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

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindSquashTarget))]
    [HarmonyPostfix]
    private static void Plant_FindSquashTarget_Postfix(Plant __instance, Zombie __result)
    {
        if (__instance.mSeedType != SeedType.Squash) return;

        // Check if we're in an online multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            // Only plant-side players need to send network updates
            if (VersusState.AmPlantSide)
            {
                // If the Squash found a target zombie
                if (__result != null)
                {
                    __instance.GetNetworked()?.GetNetworkComponent<SquashNetworkComponent>().SendSquashTargetRpc(__result);
                }
            }
        }
    }
}