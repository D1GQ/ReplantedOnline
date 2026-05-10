using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class TanglekelpPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPostfix]
    private static void Plant_FindTargetZombie_Postfix(Plant __instance, ref Zombie __result)
    {
        if (__instance.mSeedType != SeedType.Tanglekelp) return;

        // Check if we're in an online multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            var plantNetworked = __instance.GetNetworked();
            if (plantNetworked != null)
            {
                if (VersusState.AmPlantSide)
                {
                    Zombie targetZombie = null;
                    foreach (var zombie in __instance.mBoard.GetZombies())
                    {
                        if (__instance.mRow != zombie.mRow) continue;

                        if (PvZRUtils.ReloadedObjectXToGridX(zombie.mPosX) == PvZRUtils.ReloadedObjectXToGridX(__instance.mX))
                        {
                            targetZombie = zombie;
                            break;
                        }
                    }

                    if (targetZombie != null && !targetZombie.IsTangleKelpTarget())
                    {
                        __result = targetZombie;
                        if (plantNetworked.Target == null)
                        {
                            plantNetworked.Target = targetZombie;
                            plantNetworked.SendSetZombieTargetRpc(targetZombie);
                        }

                        return;
                    }
                }
                else
                {
                    __result = plantNetworked.Target;
                    return;
                }
            }

            __result = null;
        }
    }
}
