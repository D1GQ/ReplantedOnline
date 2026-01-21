using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

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
        if (NetLobby.AmInLobby() && VersusState.VersusPhase is VersusPhase.Gameplay or VersusPhase.SuddenDeath)
        {
            // Allow Target zombies (like Target Zombie from I, Zombie) to use original logic
            if (theZombieType is ZombieType.Target) return true;

            if (!VersusState.AmPlantSide)
            {
                // Fix flag zombie on other sides
                if (theZombieType != ZombieType.Imp)
                {
                    __result = ObjectHelper.CreateReloadedObject<Zombie>();
                }

                return false;
            }

            // Spawn zombie at column 9 (right side of board) with network synchronization
            __result = Utils.SpawnZombie(theZombieType, 9, theRow, theZombieType is not ZombieType.Imp, true);

            // Skip original method since we handled spawning with network sync
            return false;
        }

        return true; // Allow original method in single player or non-gameplay phases
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.WalkIntoHouse))]
    [HarmonyPrefix]
    private static bool Zombie_WalkIntoHouse_Prefix(Zombie __instance)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var netZombie = __instance.GetNetworked<ZombieNetworked>();
                netZombie.SendEnteringHouseRpc(__instance.mPosX);
                VersusGameplayManager.EndGame(__instance.mController?.gameObject, PlayerTeam.Zombies);
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.StartMindControlled))]
    [HarmonyPrefix]
    private static bool Zombie_StartMindControlled_Prefix(Zombie __instance)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var netZombie = __instance.GetNetworked<ZombieNetworked>();
                netZombie.SendMindControlledRpc();
            }
        }

        return true;
    }
}