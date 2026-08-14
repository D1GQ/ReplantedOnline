using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Config.Reloaded;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Reloaded.Versus.Arenas;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;

namespace ReplantedOnline.Managers.Reloaded;

/// <summary>
/// Static manager class responsible for handling versus mode in lonny
/// </summary>
internal static class VersusGameplayManager
{
    /// <summary>
    /// Gets the versus mode configuration.
    /// </summary>
    /// <returns>The current <see cref="VersusModeConfig"/> instance containing all versus mode settings.</returns>
    internal static VersusModeConfig GetVersusModeConfig()
    {
        return ReloadedLobby.LobbyData!.VersusModeConfig;
    }

    /// <summary>
    /// Gets or sets the remaining number of lives for the zombies.
    /// </summary>
    internal static int ZombieLife = 3;

    /// <summary>
    /// Gets or sets a value indicating whether the dancer frame state has been synchronized for the current frame.
    /// </summary>
    internal static bool IsDancingThisFrameSynced;

    /// <summary>
    /// Called when the versus mode starts.
    /// </summary>
    /// <param name="versusMode">The versus mode instance.</param>
    internal static void OnStart(VersusMode versusMode)
    {
        ZombieLife = 3;
        IsDancingThisFrameSynced = false;
        isInSuddenDeath = false;
        List<SeedPacket> allSeedPackets =
        [
            .. Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets,
            .. Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets,
        ];

        foreach (var seedPacket in allSeedPackets)
        {
            if (GetVersusModeConfig().IgnoreInitialCooldown.Contains(seedPacket.mPacketType)
                || seedPacket.mPacketType == SeedPacketDefinitions.RandomHiddenSeedType)
                continue;

            seedPacket.Deactivate();
            var time = Instances.IDataService.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            seedPacket.mRefreshTime = time;
            seedPacket.mRefreshing = true;
        }

        // Disable inputs for starting countdown 
        InputManager.SetDeviceActive(false);

        CoroutineManager.Instance.StartCoroutine(CoroutineUtils.WaitForCondition(() => VersusState.IsInGameplay, () =>
        {
            InputManager.SetDeviceActive(true);
        }));

        ReloadedLobby.LobbyData!.ReadyForNetworkObjects = true;
    }

    private static bool isInSuddenDeath;

    /// <summary>
    /// Called every frame to update the versus mode state.
    /// </summary>
    /// <param name="versusMode">The versus mode instance.</param>
    internal static void Update(VersusMode versusMode)
    {
        if (!isInSuddenDeath)
        {
            if (VersusState.VersusTimeSynced >= VersusMode.k_suddenDeathStartTime)
            {
                isInSuddenDeath = true;
                foreach (var seedBank in versusMode.m_board.SeedBanks.m_values)
                {
                    foreach (var seedPacket in seedBank.mSeedPackets)
                    {
                        if (!GetVersusModeConfig().DisabledInSuddenDeath.Contains(seedPacket.PacketType))
                            continue;

                        seedBank.SetSeedPacketDisabled(seedPacket, true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Synchronizes versus mode states between clients.
    /// </summary>
    /// <param name="versusMode">The versus mode instance.</param>
    /// <param name="previousVersusTime">The versus time from the previous update.</param>
    /// <param name="currentVersusTime">The versus time from the current update.</param>
    internal static void SyncVersusStates(VersusMode versusMode, float previousVersusTime, float currentVersusTime)
    {
        if (!ReloadedLobby.AmLobbyHost())
        {
            versusMode.m_versusTime = previousVersusTime;
            return;
        }

        if ((int)(currentVersusTime * 2f) != (int)(previousVersusTime * 2f))
        {
            NetworkManager.Packet<SyncVersusTimePacket>.Singleton.Send(versusMode.m_versusTime);
        }

        int dancerFrame = PvZRUtils.GetDancerFrame(false);
        if (!IsDancingThisFrameSynced)
        {
            if (dancerFrame > ReplantedOnlineMod.Constants.Reloaded.DANCER_NON_DANCING_FRAME)
            {
                IsDancingThisFrameSynced = true;
                NetworkManager.Packet<SyncDancerFramePacket>.Singleton.Send(true);
            }
        }
        else
        {
            if (dancerFrame < ReplantedOnlineMod.Constants.Reloaded.DANCER_DANCING_FRAME)
            {
                IsDancingThisFrameSynced = false;
                NetworkManager.Packet<SyncDancerFramePacket>.Singleton.Send(false);
            }
        }
    }

    /// <summary>
    /// Ends the versus game with the specified winning team.
    /// </summary>
    /// <param name="focusPos">The position to focus the camera on during the end game sequence.</param>
    /// <param name="winningTeam">The team that won the match.</param>
    internal static void EndGame(Vector3 focusPos, PlayerTeam winningTeam)
    {
        IVersusGamemode.GetCurrentGamemode().OnGameplayEnd(Instances.GameplayActivity.VersusMode, winningTeam);

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
        Instances.GameplayActivity.m_messageWidgetController.Hide();
        Instances.GameplayActivity.Board.FreezeEffectsForCutscene(true);
        Instances.GameplayActivity.m_audioService.StopAllMusic();
        VersusEndGameManager.EndGame(winningTeam);
    }

    /// <summary>
    /// Gets the initial sky production rate from the versus mode configuration.
    /// </summary>
    /// <returns>The initial sky production rate value.</returns>
    internal static int GetInitSkyRate()
    {
        return GetVersusModeConfig().InitialSkyProductionRate;
    }

    /// <summary>
    /// Gets the current sky production rate from the versus mode configuration.
    /// </summary>
    /// <returns>The sky production rate value.</returns>
    internal static int GetSkyRate()
    {
        return GetVersusModeConfig().SkyProductionRate;
    }

    /// <summary>
    /// Gets a random initial production rate between the minimum and maximum configured values.
    /// </summary>
    /// <returns>A random integer between <see cref="VersusModeConfig.InitialProductionRateMin"/> and <see cref="VersusModeConfig.InitialProductionRateMax"/>.</returns>
    internal static int GetInitPlantOrGraveRate()
    {
        return Common.RandRangeInt(GetVersusModeConfig().InitialProductionRateMin, VersusGameplayManager.GetVersusModeConfig().InitialProductionRateMax);
    }

    /// <summary>
    /// Gets the current plant production rate from the versus mode configuration.
    /// </summary>
    /// <returns>The plant production rate value.</returns>
    internal static int GetPlantRate()
    {
        return GetVersusModeConfig().PlantProductionRate;
    }

    /// <summary>
    /// Gets the current zombie production rate from the versus mode configuration.
    /// </summary>
    /// <returns>The zombie production rate value.</returns>
    internal static int GetZombieRate()
    {
        return GetVersusModeConfig().ZombieProductionRate;
    }

    /// <summary>
    /// Gets the list of special zombie spawn rules used during flag zombies.
    /// </summary>
    /// <returns>A list of <see cref="FlagZombieSpecialSpawn"/> defining possible special zombie spawns.</returns>
    internal static List<FlagZombieSpecialSpawn> GetFlagZombieSpawns()
    {
        List<FlagZombieSpecialSpawn> zombies = [];

        switch (VersusState.ArenaSynced)
        {
            case ArenaType.Night:
                zombies.Add(new(ZombieType.Pail, 15, 13)); // 15% -> 2% -> 0%
                zombies.Add(new(ZombieType.Newspaper, 15, 10)); // 15% -> 5% -> 0%
                zombies.Add(new(ZombieType.TrafficCone, 25, 10)); // 25% -> 15% -> 0%
                break;
            case ArenaType.Roof:
            case ArenaType.RoofNight:
            case ArenaType.China:
                // Bungees ether takes a plant or drops another FlagZombieSpecialSpawn zombie type from this list
                zombies.Add(new(ZombieType.Bungee, 75, 35)); // 75% -> 40% -> 5% | 15% chance to spawn normal bungee in FlagZombiePatch.cs
                zombies.Add(new(ZombieType.Pail, 15, 10)); // 15% -> 5% -> 0%
                zombies.Add(new(ZombieType.TrafficCone, 25, 10)); // 25% -> 15% -> 0%
                break;
            default:
                zombies.Add(new(ZombieType.Pail, 15, 10)); // 15% -> 5% -> 0%
                zombies.Add(new(ZombieType.TrafficCone, 25, 10)); // 25% -> 15% -> 0%
                break;
        }

        return zombies;
    }

    /// <summary>
    /// Calculates the amount of zombies flag zombie spawns.
    /// </summary>
    internal static int FlagSpawnAmount()
    {
        float normalized = Mathf.Clamp01(VersusState.VersusTimeSynced / VersusMode.k_suddenDeathStartTime);
        float value = Mathf.Lerp(5f, 10f, normalized);
        return Mathf.FloorToInt(value);
    }

    /// <summary>
    /// Gets the refresh time for a seed packet in versus mode, which scales down over time to its base cooldown.
    /// During Sudden Death, the Sudden Death refresh time is used instead.
    /// </summary>
    /// <param name="seedType">The type of seed to get the refresh time for.</param>
    /// <returns>The modified refresh time in seconds after applying reductions and scaling.</returns>
    internal static int GetSeedPacketRefreshTime(SeedType seedType)
    {
        int refreshTime;
        if (GetVersusModeConfig().TryGetSeedPacketConfig(seedType, out var config))
        {
            if (VersusState.VersusPhase != VersusPhase.SuddenDeath)
            {
                refreshTime = config.RefreshTime;
            }
            else
            {
                refreshTime = config.SuddenDeathRefresh;
            }
        }
        else
        {
            refreshTime = 0;
        }

        CloudyDayArena.ApplyRefreshTimeReduction(seedType, ref refreshTime);

        if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
        {
            return refreshTime;
        }

        float normalized = Mathf.Clamp01(VersusState.VersusTimeSynced / ReplantedOnlineMod.Constants.Reloaded.X2_SEEDPACKET_COOLDOWN_TIME_END);
        float time = Mathf.Lerp(refreshTime * 2, refreshTime, normalized);
        return Mathf.FloorToInt(time);
    }
}