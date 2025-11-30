using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
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
            if (VersusState.ZombieSide)
            {
                if (__instance.mSeedType is (SeedType.Potatomine or SeedType.Chomper or SeedType.Squash))
                {
                    return false;
                }
            }
        }

        return true;
    }
}