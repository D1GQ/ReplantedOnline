using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
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
            var netPlant = __instance.GetNetworked();

            if (netPlant != null)
            {
                if (VersusState.AmPlantSide)
                {
                    if (theZombie != null)
                    {
                        // Send network message to tell other players about the magnet shroom target
                        if (netPlant._Target != theZombie)
                        {
                            netPlant._Target = theZombie;
                            netPlant.SendSetZombieTargetRpc(theZombie);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.MagnetShroomAttactItem))]
    internal static void MagnetShroomAttactItemOriginal(this Plant __instance, Zombie theZombie)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
