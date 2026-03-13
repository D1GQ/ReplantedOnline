using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus.Gamemodes;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Patches.Gameplay.UI;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Managers;

/// <summary>
/// Static manager class responsible for handling versus mode in lonny
/// </summary>
internal class VersusGameplayManager
{
    /// <summary>
    /// Gets the current game mode.
    /// </summary>
    internal static IVersusGamemode VersusGamemode { get; private set; }

    internal static IVersusGamemode SetGamemode(SelectionSet selectionSet)
    {
        return VersusGamemode = selectionSet switch
        {
            SelectionSet.QuickPlay => RegisterVersusGameMode.GetInstance<QuickplayGamemode>(),
            SelectionSet.Random => RegisterVersusGameMode.GetInstance<RandomGamemode>(),
            SelectionSet.CustomAll => RegisterVersusGameMode.GetInstance<CustomGamemode>(),
            _ => null,
        };
    }

    internal static void OnStart()
    {
        Transitions.SetFade();
        Transitions.FadeIn();
        VersusHudPatch.SetHuds();
        VersusLobbyPatch.OnGameStart();

        if (NetLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, false, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 3, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, false, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Zamboni, 8, 2, false, true);
        }

        List<SeedPacket> allSeedPackets =
        [
            .. Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets,
            .. Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets,
        ];

        // Initial cooldowns
        foreach (var seedPacket in allSeedPackets)
        {
            if (SeedPacketDefinitions.IgnoreInitialCooldown.Contains(seedPacket.mPacketType)) continue;

            seedPacket.Deactivate();
            seedPacket.mRefreshTime = Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            seedPacket.mRefreshing = true;
        }

        // Disable inputs for starting countdown 
        Instances.GameplayActivity.InputService
            .GetPlayer(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
            .Player.DeactivateInput();

        Instances.GameplayActivity.StartCoroutine(CoroutineUtils.WaitForCondition(() =>
            {
                return Instances.GameplayActivity.VersusMode.m_versusTime > 3.5f;
            }, () =>
            {
                Instances.GameplayActivity.InputService
                    .GetPlayer(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
                    .Player.ActivateInput();
            }
        ));
    }

    internal static void EndGame(GameObject focus, PlayerTeam winningTeam)
    {
        VersusGamemode.OnGameplayEnd(Instances.GameplayActivity.VersusMode, winningTeam);

        if (focus == null)
        {
            ReplantedOnlineMod.Logger.Error("Can not end game, Focus gameobject is null!");
            return;
        }

        if (winningTeam is PlayerTeam.Plants)
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.PlantsWin;
        }
        else
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ZombiesWin;
        }

        Instances.GameplayActivity.VersusMode.SetFocus(focus, Vector3.zero);
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
        int plantMultiplier = 25 * Instances.GameplayActivity.Board.GetPlants().Length;
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
                ZombieType.Target => 200,
                ZombieType.Gargantuar => 150,
                ZombieType.Gravestone => 100,
                ZombieType.Zamboni => 50,
                ZombieType.Zombatar => 50,
                ZombieType.Catapult => 30,
                ZombieType.Football => 30,
                ZombieType.Dancer => 25,
                ZombieType.Pogo => 15,
                ZombieType.Pail => 15,
                ZombieType.Polevaulter => 15,
                ZombieType.BackupDancer => 0,
                _ => 10,
            };
        }

        int plantMultiplier = 5 * Instances.GameplayActivity.Board.m_plants.m_itemLookup.Keys.Count;

        return Mathf.FloorToInt((currentCounter * 0.8f)) + zombieMultiplier - plantMultiplier;
    }
}