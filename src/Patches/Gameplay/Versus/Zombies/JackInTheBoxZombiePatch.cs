using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class JackInTheBoxZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.EatPlant))]
    [HarmonyPrefix]
    private static bool Zombie_EatPlant_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.JackInTheBox) return true;

        if (ReplantedLobby.AmInLobby())
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