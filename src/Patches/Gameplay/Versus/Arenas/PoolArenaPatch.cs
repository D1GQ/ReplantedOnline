using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Arenas;

[HarmonyPatch]
internal static class PoolArenaPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.CheckForPool))]
    [HarmonyPrefix]
    private static bool Zombie_CheckForPool_Prefix(Zombie __instance)
    {
        if (ReplantedLobby.AmInLobby())
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
        if (ReplantedLobby.AmInLobby())
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
}
