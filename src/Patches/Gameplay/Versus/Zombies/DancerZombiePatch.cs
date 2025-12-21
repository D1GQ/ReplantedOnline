using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class DancerZombiePatch
{
    /// Prevents the plant side from triggering backup dancer spawning logic
    /// Only the zombie side should control dancer spawning in versus mode
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.NeedsMoreBackupDancers))]
    [HarmonyPostfix]
    private static void NeedsMoreBackupDancers_Postfix(Zombie __instance, ref bool __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                __result = false;
            }
        }
    }

    /// Reworks backup dancer spawning to use RPCs for network synchronization
    /// Handles dancers spawned by Dancing Zombies
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SummonBackupDancer))]
    [HarmonyPrefix]
    private static bool SummonBackupDancer_Prefix(Zombie __instance, int theRow, int thePosX, ref ZombieID __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var zombie = SeedPacketSyncPatch.SpawnZombie(ZombieType.BackupDancer, thePosX, theRow, false, true);
            __result = zombie.DataID;

            return false;
        }

        return true;
    }
}
