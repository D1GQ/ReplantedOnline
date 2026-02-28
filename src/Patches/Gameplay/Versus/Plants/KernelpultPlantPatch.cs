using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Steam;

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
                var netPlant = __instance.GetNetworked<PlantNetworked>();
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
