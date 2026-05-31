using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities.Modded;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class DolphinRiderZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant __result)
    {
        if (__instance.mZombieType != ZombieType.DolphinRider) return;
        if (theAttackType != ZombieAttackType.Vault) return;

        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                if (__instance.mZombiePhase == ZombiePhase.DolphinRiding)
                {
                    // Wait for plant side to find target to vault
                    var zombieNetworked = __instance.GetNetworked();
                    if (zombieNetworked != null)
                    {
                        if (zombieNetworked.Target != null)
                        {
                            __result = zombieNetworked.Target;
                        }
                        else
                        {
                            // Push back until plant side has vaulted
                            if (__result != null)
                            {
                                if (__result.mX < __instance.mX)
                                {
                                    __instance.mPosX -= __instance.GetZombieMoveDirection();
                                }

                                __result = null;
                            }
                        }
                    }
                }
            }
        }
    }
}