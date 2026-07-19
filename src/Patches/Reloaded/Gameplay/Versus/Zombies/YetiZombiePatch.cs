using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Reloaded.Client;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class YetiZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant? __result)
    {
        if (__instance.mZombieType != ZombieType.Yeti) return;
        if (theAttackType != ZombieAttackType.Chew) return;

        if (ReloadedLobby.AmInLobby())
        {
            __result = null;
        }
    }
}