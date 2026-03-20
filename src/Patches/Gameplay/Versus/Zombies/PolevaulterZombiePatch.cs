using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class PolevaulterZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant __result)
    {
        if (__instance.mZombieType != ZombieType.Polevaulter) return;
        if (theAttackType != ZombieAttackType.Vault) return;

        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                if (__instance.mZombiePhase == ZombiePhase.PolevaulterPreVault)
                {
                    // Wait for plant side to find target to vault
                    var netZombie = __instance.GetNetworked();
                    if (netZombie != null)
                    {
                        if (netZombie.Target != null)
                        {
                            __result = netZombie.Target;
                        }
                        else
                        {
                            // Push back until plant side has vaulted
                            if (__result != null)
                            {
                                __result = null;
                                __instance.mPosX -= __instance.GetZombieMoveDirection();
                            }
                        }
                    }
                }
            }
        }
    }
}