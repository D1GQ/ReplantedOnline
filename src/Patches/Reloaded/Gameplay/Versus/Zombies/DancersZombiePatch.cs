using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using System.Collections;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class DancersZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.GetDancerFrame))]
    [HarmonyPrefix]
    private static bool Zombie_GetDancerFrame_Prefix(Zombie __instance, ref int __result)
    {
        if (ReloadedLobby.AmInLobby() && !ReloadedLobby.AmLobbyHost())
        {
            if (__instance.mZombiePhase == ZombiePhase.DancerDancingIn)
                return true;

            if (VersusGameplayManager.IsDancingThisFrameSynced)
            {
                __result = ReplantedOnlineMod.Constants.Reloaded.DANCER_DANCING_FRAME;
            }
            else
            {
                __result = ReplantedOnlineMod.Constants.Reloaded.DANCER_NON_DANCING_FRAME;

                // Force out of the DancerSnappingFingersHold phase
                if (__instance.mZombiePhase == ZombiePhase.DancerSnappingFingersHold)
                {
                    __instance.mPhaseCounter = 0;
                }
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.RowCanHaveZombieType))]
    [HarmonyPostfix]
    private static void Board_RowCanHaveZombieType_Postfix(Board __instance, int theRow, ZombieType theZombieType, ref bool __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (theZombieType == ZombieType.BackupDancer)
            {
                if (theRow < 0 || theRow > (__instance.GetNumRows() - 1))
                {
                    __result = false;
                    return;
                }

                if (__instance.mPlantRow[theRow] == PlantRowType.Pool)
                {
                    __result = false;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SummonBackupDancer))]
    [HarmonyPrefix]
    private static bool Zombie_SummonBackupDancer_Prefix(Zombie __instance, int theRow, int thePosX, ref ZombieID __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide || !__instance.mBoard.RowCanHaveZombieType(theRow, ZombieType.BackupDancer))
            {
                __result = ZombieID.Null;
                return false;
            }

            var backupDancer = SeedPacketDefinitions.SpawnZombie(ZombieType.BackupDancer, thePosX, theRow, false).Zombie;
            backupDancer.mRelatedZombieID = __instance.DataID;
            backupDancer.mGraveX = GetFollowerIndex(__instance, theRow, thePosX);
            SeedPacketDefinitions.SpawnZombieOnNetwork(backupDancer, thePosX, theRow);
            __result = backupDancer.DataID;

            return false;
        }

        return true;
    }

    private static int GetFollowerIndex(Zombie dancer, int theRow, int thePosX)
    {
        int row = dancer.mRow;
        int posX = (int)dancer.mPosX;

        if (theRow == row - 1 && Math.Abs(thePosX - posX) < 10)
            return 0;

        if (theRow == row + 1 && Math.Abs(thePosX - posX) < 10)
            return 1;

        if (theRow == row && Math.Abs(thePosX - (posX - 100)) < 10)
            return 2;

        if (theRow == row && Math.Abs(thePosX - (posX + 100)) < 10)
            return 3;

        return -1;
    }

    private static void SetFollowerByIndex(Zombie dancer, Zombie backupDancer, int index)
    {
        dancer.mController.StartCoroutine(CoSetFollowerByIndex(dancer, backupDancer, index));
    }

    private static IEnumerator CoSetFollowerByIndex(Zombie dancer, Zombie backupDancer, int index)
    {
        while (dancer.mZombiePhase != ZombiePhase.DancerSnappingFingersHold)
        {
            yield return null;
        }

        ZombieID[] array = [.. dancer.mFollowerZombieID];
        array[index] = backupDancer.DataID;
        dancer.mFollowerZombieID = array;
    }

    internal static void DancersSerialize(Zombie dancer, PacketWriter packetWriter)
    {
        var dancerLeader = dancer.mBoard.ZombieGet(dancer.mRelatedZombieID);
        if (dancerLeader == null)
        {
            packetWriter.WriteNetworkObject(null);
        }
        else
        {
            var netLeader = dancerLeader.GetNetworked();
            packetWriter.WriteNetworkObject(netLeader);
            packetWriter.WritePackedInt(dancer.mGraveX);
            dancer.mGraveX = 0;
        }
    }

    internal static void DancersDeserialize(Zombie dancer, PacketReader packetReader)
    {
        var netDancerLeader = packetReader.ReadNetworkObject<ZombieNetworked>();
        if (netDancerLeader?.Zombie != null)
        {
            var followerIndex = packetReader.ReadPackedInt();
            SetFollowerByIndex(netDancerLeader.Zombie, dancer, followerIndex);
        }
    }
}
