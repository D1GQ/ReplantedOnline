using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities.Il2Cpp;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Arenas;

[HarmonyPatch]
internal static class PoolArenaPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.CheckForPool))]
    [HarmonyPrefix]
    private static bool Zombie_CheckForPool_Prefix(Zombie __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // All logic is remade in ZombieInPoolNetworkComponent.cs
            return false;
        }

        return true;
    }


    [HarmonyPatch(typeof(ReloadedCharacterController), nameof(ReloadedCharacterController.ShouldTriggerTimedEvent))]
    [HarmonyPostfix]
    private static void ReloadedCharacterController_ShouldTriggerTimedEvent_Postfix(ReloadedCharacterController __instance, ref bool __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Remove running particles
            if (VersusState.Arena is ArenaTypes.Pool or ArenaTypes.PoolNight)
            {
                if (__instance.Il2CppTryCast<ZombieController>(out var zombieController))
                {
                    var zombie = zombieController.m_zombie;
                    if (zombie != null && zombie.mZombieType is ZombieType.Football or ZombieType.Polevaulter)
                    {
                        if (zombie.mBoard.mPlantRow[zombie.mRow] == PlantRowType.Pool)
                        {
                            __result = false;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.UpdateFog))]
    [HarmonyPrefix]
    private static bool Board_UpdateFog_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(FogController), nameof(FogController.SetTarget))]
    [HarmonyPrefix]
    private static bool FogController_SetTarget_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(FogController), nameof(FogController.SetTarget))]
    internal static void SetTargetOriginal(this FogController __instance, int target, TodCurves curveType, float delay = 0f, float duration = 2f, bool ignoreDupeCheck = false)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.CanTargetPlant))]
    [HarmonyPostfix]
    private static void Zombie_CanTargetPlant_Postfix(Zombie __instance, Plant thePlant, ZombieAttackType theAttackType, ref bool __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Fix zombies in pool not finding target
            if (__instance.mBoard.mPlantRow[__instance.mRow] == PlantRowType.Pool)
            {
                var plantOnLawn = __instance.mBoard.GetPlantsOnLawn(PvZRUtils.ReloadedObjectXToGridX(thePlant.X), PvZRUtils.ReloadedObjectYToGridY(thePlant.Y));

                if (plantOnLawn.PumpkinPlant != null)
                {
                    if (plantOnLawn.PumpkinPlant == thePlant)
                    {
                        __result = true;
                    }

                    return;
                }

                if (plantOnLawn.NormalPlant != null)
                {
                    if (plantOnLawn.NormalPlant == thePlant)
                    {
                        __result = true;
                    }

                    return;
                }

                if (plantOnLawn.UnderPlant == thePlant)
                {
                    __result = true;
                }
            }
        }
    }
}
