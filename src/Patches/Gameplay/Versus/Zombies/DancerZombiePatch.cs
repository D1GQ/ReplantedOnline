using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Logging;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Steam;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class DancerZombiePatch
{
    /// Prevents the plant side from triggering backup dancer spawning logic
    /// Only the zombie side should control dancer spawning in versus mode
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.NeedsMoreBackupDancers))]
    [HarmonyPostfix]
    private static void Zombie_NeedsMoreBackupDancers_Postfix(Zombie __instance, ref bool __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                foreach (var id in __instance.mFollowerZombieID)
                {
                    if (Instances.GameplayActivity.Board.m_zombies.DataArrayGet(id)?.mDead != false)
                    {
                        __result = true;
                        return;
                    }
                }
            }

            __result = false;
        }
    }

    /// Reworks backup dancer spawning to use RPCs for network synchronization
    /// Handles dancers spawned by Dancing Zombies
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SummonBackupDancer))]
    [HarmonyPrefix]
    private static bool Zombie_SummonBackupDancer_Prefix(Zombie __instance, int theRow, int thePosX, ref ZombieID __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var backupDancer = SeedPacketSyncPatch.SpawnZombie(ZombieType.BackupDancer, thePosX, theRow, false, true);
            __instance.AddNextId(backupDancer);

            throw new SilentPatchException();
        }

        return true;
    }

    private static void AddNextId(this Zombie dancer, Zombie backupDancer)
    {
        for (int i = 0; i < dancer.mFollowerZombieID.Count; i++)
        {
            ZombieID id = dancer.mFollowerZombieID[i];
            if (Instances.GameplayActivity.Board.m_zombies.DataArrayTryToGet(id)?.mDead != false)
            {
                var newArray = dancer.mFollowerZombieID.ToArray();
                newArray[i] = backupDancer.DataID;
                dancer.mFollowerZombieID = newArray;
                break;
            }
        }
    }
}
