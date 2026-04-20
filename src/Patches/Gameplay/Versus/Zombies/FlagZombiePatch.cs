using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using static Il2CppReloaded.Constants;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class FlagZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPrefix]
    private static void Zombie_ZombieInitialize_Prefix(ZombieType theType)
    {
        if (theType != ZombieType.Flag) return;

        if (ReplantedLobby.AmInLobby())
        {
            Instances.GameplayActivity.PlaySample(Sound.SOUND_HUGE_WAVE);
        }
    }
}