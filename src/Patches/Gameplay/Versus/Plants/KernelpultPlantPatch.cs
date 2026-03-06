using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class KernelpultPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.Fire))]
    [HarmonyPrefix]
    private static bool Plant_Fire_Prefix(Plant __instance, Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        if (__instance.mSeedType != SeedType.Kernelpult) return true;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                // Sync Butter
                var netPlant = __instance.GetPlantNetworked();
                if (netPlant != null)
                {
                    netPlant.SendFireRpc(theTargetZombie, theRow, thePlantWeapon);
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
