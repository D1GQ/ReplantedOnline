using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class TargetZombiePatch
{
    // Let network zombie set the ZombieLife
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DropHead))]
    [HarmonyPostfix]
    private static void Zombie_DropHead_Postfix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.Target) return;

        Instances.GameplayActivity.VersusMode.ZombieLife++;
    }
}
