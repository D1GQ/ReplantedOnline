using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class TargetZombiePatch
{
    // Let network zombie set the ZombieLife
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DropHead))]
    [HarmonyPrefix]
    private static void Zombie_DropHead_Prefix(Zombie __instance, ref int __state)
    {
        if (__instance.mZombieType is ZombieType.Target)
        {
            __state = Instances.GameplayActivity.VersusMode.ZombieLife;
            Instances.GameplayActivity.VersusMode.ZombieLife = 3;
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DropHead))]
    [HarmonyPostfix]
    private static void Zombie_DropHead_Postfix(Zombie __instance, int __state)
    {
        if (__instance.mZombieType is ZombieType.Target)
        {
            Instances.GameplayActivity.VersusMode.ZombieLife = __state;
        }
    }
}
