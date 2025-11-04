using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Patches.Versus;

[HarmonyPatch]
internal class ZombiePatch
{
    // Fix Bungee spawning in a random position
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PickBungeeZombieTarget))]
    [HarmonyPrefix]
    internal static bool PickBungeeZombieTarget_Prefix()
    {
        if (NetLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.NeedsMoreBackupDancers))]
    [HarmonyPostfix]
    internal static void NeedsMoreBackupDancers_Postfix(Zombie __instance, ref bool __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.PlantSide)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// rework spawning wave zombies to use RPCs.
    /// </summary>
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    [HarmonyPrefix]
    internal static bool AddZombieInRow_Prefix(Board __instance, ZombieType theZombieType, int theRow, ref Zombie __result)
    {
        if (NetLobby.AmInLobby() && VersusState.VersusPhase is VersusPhase.Gameplay or VersusPhase.SuddenDeath)
        {
            if (theZombieType == ZombieType.Target) return true;

            __result = Utils.SpawnZombie(theZombieType, 9, theRow, true, true);

            return false;
        }

        return true;
    }

    /// <summary>
    /// rework spawning backup dancers to use RPCs.
    /// </summary>
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SummonBackupDancer))]
    [HarmonyPrefix]
    internal static bool SummonBackupDancer_Prefix(Zombie __instance, int theRow, int thePosX, ref ZombieID __result)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.PlantSide) return false;

            // Find first available slot
            int nextIndex = 0;
            for (int i = 0; i < __instance.mFollowerZombieID.Length; i++)
            {
                if (__instance.mFollowerZombieID[i] == ZombieID.Null)
                {
                    nextIndex = i;
                    break;
                }
            }

            var zombie = SeedPacketSyncPatch.SpawnZombie(ZombieType.BackupDancer, thePosX, theRow, false, true);

            // Set the follower ID
            __result = zombie.DataID;

            __instance.GetNetworkedZombie().SendSetFollowerZombieIdRpc(nextIndex, zombie.DataID);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Tell client to let the zombie walk into the house
    /// </summary>
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.WalkIntoHouse))]
    [HarmonyPostfix]
    internal static void WalkIntoHouse_Postfix(Zombie __instance)
    {
        __instance.GetNetworkedZombie()?.SendEnteringHouseRpc();
    }
}
