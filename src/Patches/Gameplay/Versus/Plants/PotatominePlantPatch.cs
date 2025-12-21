using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PotatominePlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool Plant_FindTargetZombie_Prefix(Plant __instance)
    {
        if (__instance.mSeedType is not SeedType.Potatomine) return true;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // If player is NOT on the plant side
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPostfix]
    private static void Plant_FindTargetZombie_Postfix(Plant __instance, ref Zombie __result)
    {
        if (__instance.mSeedType is not SeedType.Potatomine) return;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            var netPlant = __instance.GetNetworked<PlantNetworked>();

            if (netPlant != null)
            {
                // PLANT-SIDE PLAYER LOGIC
                if (VersusState.AmPlantSide)
                {
                    // If the plant found a target zombie (original logic worked)
                    if (__result != null)
                    {
                        // Send network message to tell other players about the potato mine target
                        netPlant.SendPotatomineRpc(__result);
                    }
                }
                else
                {
                    // For other players, get the target from network state instead of local AI
                    if (netPlant._State is Zombie zombie)
                    {
                        // Override the result with the networked zombie target
                        __result = zombie;
                    }
                }
            }
        }
    }
}