using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Structs;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class JackInTheBoxZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPostfix]
    private static void Zombie_ZombieInitialize_Postfix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.JackInTheBox)
            return;

        if (ReloadedLobby.AmInLobby())
        {
            __instance.mPhaseCounter = Common.RandRangeInt(IntTime.From(15f), IntTime.From(26f));
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.EatPlant))]
    [HarmonyPrefix]
    private static bool Zombie_EatPlant_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.JackInTheBox)
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                // From Versus Mode Console:
                // When Jack in the Box comes in contact with any plant have him immediately detonate
                if (__instance.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
                {
                    __instance.mPhaseCounter = 0;
                }
            }

            return false;
        }

        return true;
    }
}