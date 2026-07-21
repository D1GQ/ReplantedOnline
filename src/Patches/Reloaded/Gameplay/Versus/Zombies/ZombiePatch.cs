using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class ZombiePatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    [HarmonyPrefix]
    private static bool Board_AddZombieInRow_Prefix(ref Zombie __result)
    {
        if (ReloadedLobby.AmInLobby() && VersusState.IsInGameplay)
        {
            // Remove normal zombie spawning from gameplay
            __result = ObjectUtils.CreateReloadedObject<Zombie>();
            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    internal static Zombie AddZombieInRowOriginal(this Board __instance, ZombieType theZombieType, int theRow, int theFromWave, bool shakeBrush = true)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateZombieWalking))]
    [HarmonyPrefix]
    private static void Zombie_UpdateZombieWalking_Prefix(Zombie __instance, ref float __state)
    {
        if (ReloadedLobby.AmInLobby())
        {
            __state = __instance.mPosX;
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateZombieWalking))]
    [HarmonyPostfix]
    private static void Zombie_UpdateZombieWalking_Postfix(Zombie __instance, float __state)
    {
        if (ReloadedLobby.AmInLobby())
        {
            float distance = Math.Abs(__instance.mPosX - __state);
            __instance.mPosX = __state;
            if (__instance.TryGetNetworked<ZombieNetworked>(out var zombieNetworked))
            {
                zombieNetworked.LogicComponent.UpdatePosition(distance);
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.WalkIntoHouse))]
    [HarmonyPrefix]
    private static bool Zombie_WalkIntoHouse_Prefix(Zombie __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var zombieNetworked = __instance.GetNetworked();
                zombieNetworked?.SendEnteringHouseRpc(__instance.mPosX);
                VersusGameplayManager.EndGame(__instance.mController.transform.position, PlayerTeam.Zombies);
                __instance.mBoard.mCutScene.StartZombiesWon();
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant? __result)
    {
        if (theAttackType != ZombieAttackType.Chew) return;
        if (__instance.mZombieType is ZombieType.Gargantuar or ZombieType.RedeyeGargantuar) return;

        // Allow ladder climbing
        var gridX = __instance.mBoard.PixelToGridXKeepOnBoard(__instance.mPosX, __instance.mPosY);
        if (__instance.mBoard.GetLadderAt(gridX, __instance.mRow) != null) return;

        if (ReloadedLobby.AmInLobby())
        {
            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked == null) return;

            if (VersusState.AmPlantSide)
            {
                if (__result != null)
                {
                    if (zombieNetworked.State is not ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        zombieNetworked.State = ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE;
                        zombieNetworked.SendSetStateRpc(ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE);
                    }
                }
                else
                {
                    if (zombieNetworked.State is ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        zombieNetworked.State = null;
                        zombieNetworked.SendSetStateRpc(ReplantedOnlineMod.Constants.Network.ObjectStates.NULL_STATE);
                    }
                }
            }
            else
            {
                if (__result != null)
                {
                    // Push back until plant side starts eating plant
                    if (zombieNetworked.State is not ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        __result = null;
                    }
                }
                else
                {
                    // Move zombie forward to synced pos
                    if (zombieNetworked.State is ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        zombieNetworked.LogicComponent.InterpolatePosition();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.EatZombie))]
    [HarmonyPrefix]
    private static bool Zombie_EatZombie_Prefix(Zombie theZombie)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Prevent hypno affected zombies from eating target and gravestone zombies
            // This is a issue with replanted itself 
            if (theZombie.mZombieType.IsGravestoneOrTarget())
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.TrySpawnLevelAward))]
    [HarmonyPrefix]
    private static bool Zombie_TrySpawnLevelAward_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }
}