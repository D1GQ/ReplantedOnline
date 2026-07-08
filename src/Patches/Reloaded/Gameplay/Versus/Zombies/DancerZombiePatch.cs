using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class DancerZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.NeedsMoreBackupDancers))]
    [HarmonyPostfix]
    private static void Zombie_NeedsMoreBackupDancers_Postfix(Zombie __instance, ref bool __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                int amountNeeded = 4;

                bool edge = __instance.mRow == 0 || __instance.mRow == (__instance.mBoard.GetNumRows() - 1);
                if (edge)
                {
                    amountNeeded--;
                }

                if (__instance.mBoard.StageHasPool() && __instance.mRow is 1 or 4)
                {
                    amountNeeded--;
                }

                int count = 0;
                foreach (var followerId in __instance.mFollowerZombieID)
                {
                    var follower = __instance.mBoard.ZombieGet(followerId);
                    if (follower != null && !follower.mDead)
                    {
                        count++;
                    }
                }
                if (count < amountNeeded)
                {
                    __result = true;
                    return;
                }
            }

            __result = false;
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SummonBackupDancer))]
    [HarmonyPrefix]
    private static bool Zombie_SummonBackupDancer_Prefix(Zombie __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            __instance.CheckBackupDancers();
            return false;
        }

        return true;
    }

    private static void CheckBackupDancers(this Zombie dancer)
    {
        if (!dancer.NeedsMoreBackupDancers()) return;

        ZombieID[] array = [.. dancer.mFollowerZombieID];
        for (int i = 0; i < dancer.mFollowerZombieID.Count; i++)
        {
            ZombieID followerId = dancer.mFollowerZombieID[i];
            var follower = dancer.mBoard.ZombieGet(followerId);
            if (follower != null && !follower.mDead) continue;

            int row = dancer.mRow + (int)Math.Round(Math.Sin(i * Math.PI / 2));
            float posX = dancer.mPosX + 100 * (int)Math.Round(Math.Cos(i * Math.PI / 2));

            var backupDancer = dancer.TrySummonBackupDancer((int)posX, row);
            if (backupDancer != null)
            {
                array[i] = backupDancer.DataID;
            }
        }
        dancer.mFollowerZombieID = array;
    }

    private static Zombie? TrySummonBackupDancer(this Zombie dancer, int thePosX, int theRow)
    {
        if (theRow < 0 || theRow > (dancer.mBoard.GetNumRows() - 1)) return null;
        if (dancer.mBoard.mPlantRow[theRow] == PlantRowType.Pool) return null;

        return SeedPacketDefinitions.SpawnZombie(ZombieType.BackupDancer, thePosX, theRow, true).Zombie;
    }
}
