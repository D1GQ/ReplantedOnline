using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Steam;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class PolevaulterZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ref Plant __result)
    {
        if (__instance.mZombieType != ZombieType.Polevaulter) return;

        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                if (__instance.mZombiePhase == ZombiePhase.PolevaulterPreVault)
                {
                    // Wait for plant side to find target to vault
                    var netZombie = __instance.GetNetworked<ZombieNetworked>();
                    if (netZombie != null)
                    {
                        if (netZombie._Target != null)
                        {
                            __result = netZombie._Target;
                        }
                        else
                        {
                            __result = null;

                            // Push back until plant side has vaulted
                            if (__instance.FindPlantTargetOriginal(ZombieAttackType.Vault) != null)
                            {
                                __instance.mPosX += 1;
                            }
                        }
                    }
                }
            }
        }
    }
}