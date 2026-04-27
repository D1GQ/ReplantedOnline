using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class ZombiePatch
{
    /// Reworks wave zombie spawning to use RPCs for network synchronization
    /// Handles zombies spawned during waves
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    [HarmonyPrefix]
    private static bool Board_AddZombieInRow_Prefix(Board __instance, ZombieType theZombieType, int theRow, int theFromWave, ref Zombie __result)
    {
        // Only intercept during active gameplay in multiplayer
        if (ReplantedLobby.AmInLobby() && VersusState.IsInGameplay)
        {
            // Allow Target zombies (like Target Zombie from I Zombie) to use original logic
            if (theZombieType is ZombieType.Target) return true;

            // Prevent zombies from spawning when flag zombie is spawned, this is handled in FlagZombiePatch.cs
            if (theZombieType is ZombieType.Normal or ZombieType.TrafficCone or ZombieType.Pail)
            {
                __result = ObjectUtils.CreateReloadedObject<Zombie>();
                return false;
            }

            if (!VersusState.AmPlantSide)
            {
                throw new SilentPatchException();
            }

            // Prevent imps from spawning normally, this is handled in GargantuarZombiePatch.cs
            if (theZombieType is ZombieType.Imp) throw new SilentPatchException();

            // Spawn zombie at column 9 (right side of board) with network synchronization
            __result = SeedPacketDefinitions.SpawnZombie(theZombieType, 9, theRow, true);

            // Prevent VersusMode from handling Bobsled, this is done in BobsledZombiePatch.cs
            if (theZombieType == ZombieType.Bobsled) throw new SilentPatchException();

            // Skip original method since we handled spawning with network sync
            return false;
        }

        return true; // Allow original method in single player or non-gameplay phases
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    internal static Zombie AddZombieInRowOriginal(this Board __instance, ZombieType theZombieType, int theRow, int theFromWave, bool shakeBrush = true)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.WalkIntoHouse))]
    [HarmonyPrefix]
    private static bool Zombie_WalkIntoHouse_Prefix(Zombie __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var netZombie = __instance.GetNetworked();
                netZombie?.SendEnteringHouseRpc(__instance.mPosX);
                VersusGameplayManager.EndGame(__instance.mController.transform.position, PlayerTeam.Zombies);
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant __result)
    {
        if (theAttackType != ZombieAttackType.Chew) return;
        if (__instance.mZombieType is ZombieType.Gargantuar or ZombieType.RedeyeGargantuar) return;

        // Allow ladder climbing
        var gridX = __instance.mBoard.PixelToGridXKeepOnBoard(__instance.mPosX, __instance.mPosY);
        if (__instance.mBoard.GetLadderAt(gridX, __instance.mRow) != null) return;

        if (ReplantedLobby.AmInLobby())
        {
            var netZombie = __instance.GetNetworked();
            if (netZombie == null) return;

            if (VersusState.AmPlantSide)
            {
                if (__result != null)
                {
                    if (netZombie.State is not NetStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        netZombie.State = NetStates.ZOMBIE_CHEWING_PLANT_STATE;
                        netZombie.SendSetStateRpc(NetStates.ZOMBIE_CHEWING_PLANT_STATE);
                    }
                }
                else
                {
                    if (netZombie.State is NetStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        netZombie.State = null;
                        netZombie.SendSetStateRpc(NetStates.NULL_STATE);
                    }
                }
            }
            else
            {
                if (__result != null)
                {
                    // Push back until plant side starts eating plant
                    if (netZombie.State is not NetStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        __result = null;
                        __instance.mPosX -= __instance.GetZombieMoveDirection();
                    }
                }
                else
                {
                    // Move zombie forward to get first target to start eating on
                    if (netZombie.State is NetStates.ZOMBIE_CHEWING_PLANT_STATE)
                    {
                        __instance.mPosX += __instance.GetZombieMoveDirection();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.EatZombie))]
    [HarmonyPrefix]
    private static bool Zombie_EatZombie_Prefix(Zombie theZombie)
    {
        if (ReplantedLobby.AmInLobby())
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

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.GetZombieRect))]
    [HarmonyPostfix]
    private static void Zombie_GetZombieRect_Postfix(Zombie __instance, ref Rect __result)
    {
        if (!__instance.mZombieType.IsGravestoneOrTarget()) return;

        // Check if we're in an online multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Make Target Zombies and Gravestones invulnerable when behind another gravestone
            // This is a direct fix to Fumeshroom OP piercing logic
            foreach (var gravestone in __instance.mBoard.m_vsGravestones)
            {
                // Check if Gravestone is in the same row of zombie 
                if (gravestone.mRow != __instance.mRow) continue;

                // Check if Gravestone is in front of zombie
                if (gravestone.mPosX < __instance.mPosX)
                {
                    if (gravestone.mZombiePhase == ZombiePhase.ZombieNormal)
                    {
                        __result.position = new Vector2(-9999, -9999);
                        break;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.TrySpawnLevelAward))]
    [HarmonyPrefix]
    private static bool Zombie_TrySpawnLevelAward_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }
}