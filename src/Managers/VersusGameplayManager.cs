using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Patches.Gameplay.UI;
using ReplantedOnline.Patches.Gameplay.Versus;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Managers;

/// <summary>
/// Static manager class responsible for handling versus mode in lonny
/// </summary>
internal class VersusGameplayManager
{
    internal static void OnStart()
    {
        VersusHudPatch.SetHuds();

        List<SeedPacket> allSeedPackets =
        [
            .. Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets,
            .. Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets,
        ];

        IArena.GetCurrentArena()?.InitializeSeedPacketCooldowns([.. allSeedPackets]);

        // Disable inputs for starting countdown 
        Instances.GameplayActivity.InputService
            .GetPlayer(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
            .Player.DeactivateInput();

        Instances.GameplayActivity.StartCoroutine(CoroutineUtils.WaitForCondition(() =>
            {
                return VersusState.IsInGameplay;
            }, () =>
            {
                Instances.GameplayActivity.InputService
                    .GetPlayer(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
                    .Player.ActivateInput();
            }
        ));

        ReplantedLobby.LobbyData.ReadyForNetworkObjects = true;
    }

    internal static void EndGame(Vector3 focusPos, PlayerTeam winningTeam)
    {
        IVersusGamemode.GetCurrentGamemode()?.OnGameplayEnd(Instances.GameplayActivity.VersusMode, winningTeam);

        if (winningTeam is PlayerTeam.Plants)
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.PlantsWin;
        }
        else
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ZombiesWin;
        }

        GameObject trueFocus = new("FocusObject");
        trueFocus.transform.position = focusPos;

        Instances.GameplayActivity.VersusMode.SetFocusOriginal(trueFocus, Vector3.zero);
        Instances.GameplayActivity.m_audioService.StopAllMusic();
        Instances.GameplayActivity.Board.Pause(true);
        VersusEndGameManager.EndGame(winningTeam);
    }

    /// <summary>
    /// Calculates the new brain spawn counter.
    /// </summary>
    /// <param name="currentCounter">The current brain spawn counter value.</param>
    internal static int MultiplyBrainSpawnCounter(int currentCounter)
    {
        int plantMultiplier = 15 * Instances.GameplayActivity.Board.GetPlants().Length;
        return currentCounter + plantMultiplier;
    }

    /// <summary>
    /// Calculates the new grave counter.
    /// </summary>
    /// <param name="currentCounter">The current grave counter value.</param>
    internal static int MultiplyGraveCounter(int currentCounter)
    {
        int zombieMultiplier = 0;
        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            zombieMultiplier += zombie.mZombieType switch
            {
                ZombieType.Gargantuar => 200, // 1x
                ZombieType.Gravestone => 50, // 1x
                ZombieType.Zamboni => 150, // 1x
                ZombieType.Catapult => 125, // 1x
                ZombieType.Football => 125, // 1x
                ZombieType.Target => 100, // 1x
                ZombieType.Ladder => 100, // 1x
                ZombieType.Pail => 100, // 1x
                ZombieType.Pogo => 75, // 1x
                ZombieType.TrafficCone => 65, // 1x
                ZombieType.Polevaulter => 65, // 1x
                ZombieType.Bobsled => 25, // 4x: 100
                ZombieType.BackupDancer => 25, // 4x : 100
                ZombieType.Imp => 15, // 1x
                _ => 50,
            };
        }

        int plantMultiplier = 5 * Instances.GameplayActivity.Board.m_plants.m_itemLookup.Keys.Count;

        return currentCounter + Math.Max(zombieMultiplier - plantMultiplier, 0);
    }

    /// <summary>
    /// Gets the list of special zombie spawn rules used during flag zombies.
    /// </summary>
    /// <returns>
    /// A list of <see cref="FlagZombieSpecialSpawn"/> defining possible special zombie spawns.
    /// </returns>
    internal static List<FlagZombieSpecialSpawn> GetFlagZombieSpawns()
    {
        List<FlagZombieSpecialSpawn> zombies = [];

        zombies.Add(new(ZombieType.Pail, 25, 9));
        zombies.Add(new(ZombieType.TrafficCone, 75, 20));

        return zombies;
    }

    /// <summary>
    /// Calculates the amount of zombies flag zombie spawns.
    /// </summary>
    internal static int FlagSpawnAmount()
    {
        return 10;
    }
}