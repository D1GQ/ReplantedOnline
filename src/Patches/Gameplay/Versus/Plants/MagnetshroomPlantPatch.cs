using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class MagnetshroomPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.MagnetShroomAttactItem))]
    [HarmonyPrefix]
    private static bool Plant_MagnetShroomAttactItem_Prefix(Plant __instance, ref Zombie theZombie)
    {
        if (__instance.mSeedType != SeedType.Magnetshroom) return true;

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
                    if (theZombie != null)
                    {
                        // Send network message to tell other players about the magnet shroom target
                        netPlant.SendSetZombieTargetRpc(theZombie);
                    }
                }
                else
                {
                    // For other players, get the target from network state instead of local AI
                    if (netPlant._Target != null)
                    {
                        // Override the result with the networked zombie target
                        theZombie = netPlant._Target;
                        netPlant._Target = null;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
