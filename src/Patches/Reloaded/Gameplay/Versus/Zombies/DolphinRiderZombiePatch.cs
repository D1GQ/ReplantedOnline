using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class DolphinRiderZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant? __result)
    {
        if (__instance.mZombieType != ZombieType.DolphinRider) return;
        if (theAttackType != ZombieAttackType.Vault) return;

        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                if (__result != null)
                {
                    if (__result.mSeedType == SeedType.Tanglekelp)
                    {
                        __result = null;
                        return;
                    }
                }
            }
            else
            {
                __result = null;
            }
        }
    }
}