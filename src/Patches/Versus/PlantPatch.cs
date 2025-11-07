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
    private static bool Update_Prefix(Plant __instance, ref Zombie __result, ref int __state)
    {
        __state = __instance.mTargetZombieID;

        if (NetLobby.AmInLobby())
        {
            // Sync the target in the network
            if (VersusState.ZombieSide)
            {
                if (__instance.mSeedType is SeedType.Potatomine or SeedType.Chomper or SeedType.Squash)
                {
                    if (__instance.mTargetZombieID != 0)
                    {
                        __result = Instances.GameplayActivity.Board.ZombieGet(__instance.mTargetZombieID);
                    }

                    return false;
                }
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindSquashTarget))]
    [HarmonyPostfix]
    private static void Update_Postfix(Plant __instance, Zombie __result, int __state)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.PlantSide)
            {
                if (__instance.mTargetZombieID != __state)
                {
                    __instance.GetNetworked<PlantNetworked>().SendTargetZombie(__result.GetNetworked<ZombieNetworked>());
                }
            }
        }
    }
}