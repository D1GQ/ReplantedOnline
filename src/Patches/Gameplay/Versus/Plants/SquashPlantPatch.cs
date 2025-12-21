using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class SquashPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindSquashTarget))]
    [HarmonyPrefix]
    private static bool Plant_FindSquashTarget_Prefix(Plant __instance)
    {
        if (__instance.mSeedType is not SeedType.Squash) return true;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
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
        if (__instance.mSeedType is not SeedType.Squash) return;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // Only plant-side players need to send network updates
            if (VersusState.AmPlantSide)
            {
                // If the Squash found a target zombie
                if (__result != null)
                {
                    var netPlant = __instance.GetNetworked<PlantNetworked>();
                    netPlant?.SendSquashRpc(__result);
                }
            }
        }
    }
}