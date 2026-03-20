using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class LadderZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant __result)
    {
        if (__instance.mZombieType != ZombieType.Ladder) return;
        if (theAttackType != ZombieAttackType.Ladder) return;

        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                var netZombie = __instance.GetNetworked();
                if (netZombie != null)
                {
                    if (__instance.mZombiePhase == ZombiePhase.LadderCarrying)
                    {
                        // Wait for plant side to find target to place ladder
                        if (netZombie.Target != null)
                        {
                            __result = netZombie.Target;
                        }
                        else
                        {
                            // Push back until plant side has placed ladder
                            if (__result != null)
                            {
                                __result = null;
                                __instance.mPosX++;
                            }
                        }
                    }
                }
            }
        }
    }
}