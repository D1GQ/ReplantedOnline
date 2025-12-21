using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using static Il2CppReloaded.Constants;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class FlagZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPostfix]
    private static void Zombie_ZombieInitialize_Postfix(ZombieType theType)
    {
        if (theType is not ZombieType.Flag) return;

        if (NetLobby.AmInLobby())
        {
            Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_HUGE_WAVE);
        }
    }
}