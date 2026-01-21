using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Gameplay.UI;
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

        if (NetLobby.AmLobbyHost())
        {
            Utils.SpawnZombie(ZombieType.Target, 8, 0, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 1, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 2, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 3, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 4, false, true);

            Utils.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 1, true);
            Utils.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 3, true);

            Utils.SpawnZombie(ZombieType.Gravestone, 8, 1, false, true);
            Utils.SpawnZombie(ZombieType.Gravestone, 8, 3, false, true);
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
    }

    internal static void EndGame(GameObject focus, PlayerTeam winningTeam)
    {
        if (focus == null)
        {
            MelonLogger.Error("Can not end game, Focus gameobject is null!");
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