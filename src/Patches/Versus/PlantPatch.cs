using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Versus;

[HarmonyPatch]
internal static class PlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindSquashTarget))]
    [HarmonyPrefix]
    private static bool Find_Prefix(Plant __instance, ref Zombie __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmZombieSide)
            {
                if (__instance.mSeedType is (SeedType.Potatomine or SeedType.Chomper or SeedType.Squash))
                {
                    return false;
                }
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindSquashTarget))]
    [HarmonyPostfix]
    private static void Find_Prefix(Plant __instance, Zombie __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (__result != null)
            {
                var netPlant = __instance.GetNetworked<PlantNetworked>();
                if (VersusState.AmPlantSide)
                {
                    switch (__instance.mSeedType)
                    {
                        case SeedType.Squash:
                            netPlant.SendSquashRpc(__result);
                            break;
                    }
                }
            }
        }
    }
}