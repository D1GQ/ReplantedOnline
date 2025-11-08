using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Patches.Versus;

[HarmonyPatch]
internal static class ZombiePatch
{
    /// Prevents Bungee Zombies from picking random targets in multiplayer
    /// This fixes synchronization issues with Bungee Zombie spawning positions
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PickBungeeZombieTarget))]
    [HarmonyPrefix]
    private static bool PickBungeeZombieTarget_Prefix()
    {
        // Disable random Bungee Zombie target selection in multiplayer
        // Target selection should be handled through network synchronization instead
        if (NetLobby.AmInLobby())
        {
            return false; // Skip the original method
        }

        return true; // Allow original method in single player
    }

    /// Prevents the plant side from triggering backup dancer spawning logic
    /// Only the zombie side should control dancer spawning in versus mode
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.NeedsMoreBackupDancers))]
    [HarmonyPostfix]
    private static void NeedsMoreBackupDancers_Postfix(Zombie __instance, ref bool __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.PlantSide)
            {
                // Force false result for plant side to prevent them from triggering dancer logic
                __result = false;
            }
        }
    }

    /// Reworks wave zombie spawning to use RPCs for network synchronization
    /// Handles zombies spawned during waves
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    [HarmonyPrefix]
    private static bool AddZombieInRow_Prefix(Board __instance, ZombieType theZombieType, int theRow, ref Zombie __result)
    {
        // Only intercept during active gameplay in multiplayer
        if (NetLobby.AmInLobby() && VersusState.VersusPhase is VersusPhase.Gameplay or VersusPhase.SuddenDeath)
        {
            // Allow Target zombies (like Target Zombie from I, Zombie) to use original logic
            if (theZombieType == ZombieType.Target) return true;

            // Spawn zombie at column 9 (right side of board) with network synchronization
            __result = Utils.SpawnZombie(theZombieType, 9, theRow, true, true);

            // Skip original method since we handled spawning with network sync
            return false;
        }

        return true; // Allow original method in single player or non-gameplay phases
    }

    /// Reworks backup dancer spawning to use RPCs for network synchronization
    /// Handles dancers spawned by Dancing Zombies
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SummonBackupDancer))]
    [HarmonyPrefix]
    private static bool SummonBackupDancer_Prefix(Zombie __instance, int theRow, int thePosX, ref ZombieID __result)
    {
        if (NetLobby.AmInLobby())
        {
            // Only zombie side can spawn backup dancers
            if (VersusState.PlantSide) return false;

            var zombie = SeedPacketSyncPatch.SpawnZombie(ZombieType.BackupDancer, thePosX, theRow, false, true);
            __result = zombie.DataID;

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.WalkIntoHouse))]
    [HarmonyPrefix]
    private static bool WalkIntoHouse_Prefix(Zombie __instance)
    {
        if (VersusState.PlantSide)
        {
            __instance.GetNetworked<ZombieNetworked>()?.SendEnteringHouseRpc();
            __instance.GetNetworked<ZombieNetworked>()?.HandleEnteringHouseRpc();
        }

        return false;
    }
}