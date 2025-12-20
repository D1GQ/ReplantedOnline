using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using static Il2CppReloaded.Constants;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class FlagZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPrefix]
    private static void DropHead_Prefix(ZombieType theType)
    {
        if (theType is ZombieType.Flag)
        {
            Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_HUGE_WAVE);
        }
    }
}