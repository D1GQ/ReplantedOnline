using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class GravestonePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateGravestone))]
    [HarmonyPrefix]
    private static void Zombie_UpdateGravestone_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.Gravestone) return;

        if (__instance.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            __instance.mZombiePhase = (ZombiePhase)100;
            __instance.mPhaseCounter = UnityEngine.Random.Range(1000, 1500);
        }
    }
}