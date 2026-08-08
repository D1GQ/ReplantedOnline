using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

[RegisterArena(ArenaType.CloudyDay)]
internal sealed class CloudyDayArena : IArena, IArenaData
{
    /// <inheritdoc/>
    public MusicTune Music => MusicTune.DayGrasswalk;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.RiseFromGround;

    /// <inheritdoc/>
    public LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-CloudyDayLevel2")!;
    }

    /// <inheritdoc/>
    public void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Day;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public CustomRecommentedFlags GetSeedTypeCustomRecommentedFlags(SeedType seedType)
    {
        return IArena.GetDefaultRecommentedFlags(seedType, ArenaType.CloudyDay);
    }

    /// <inheritdoc/>
    public void SetSeedPacketDefinition(PlantDefinition seedPacketDefinition)
    {
    }

    /// <inheritdoc/>
    public void InitializeArena(VersusMode versusMode)
    {
        IsRaining = false;
        nextWeatherChangeTime = VersusGameplayManager.GetVersusModeConfig().CloudyDayArenaConfig.SunnyPhaseTime;

        if (ReloadedLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 3, true);
        }
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode)
    {
        versusMode.m_board.mApp.BackgroundController.EnableBowlingLine(true, 515);
        UpdateWeather(versusMode.m_app.m_cloudyDayMode);
    }

    internal static bool IsRaining;
    private static float nextWeatherChangeTime;
    private static void UpdateWeather(CloudyDayMode cloudyDayMode)
    {
        if (VersusState.VersusTimeSynced > nextWeatherChangeTime - 3f)
        {
            if (!cloudyDayMode.m_wasMessageDisplayed)
            {
                cloudyDayMode.m_wasMessageDisplayed = true;
                if (!IsRaining)
                {
                    cloudyDayMode.m_app.Board.DisplayAdviceAgain(CloudyDayMode.CLOUDY_WEATHER_MESSAGE_ID, MessageStyle.BigMiddleFast, AdviceType.None);
                }
                else
                {
                    cloudyDayMode.m_app.Board.DisplayAdviceAgain(CloudyDayMode.SUNNY_WEATHER_MESSAGE_ID, MessageStyle.BigMiddleFast, AdviceType.None);
                }
                Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_FINALWAVE);
            }
        }

        if (VersusState.VersusTimeSynced > nextWeatherChangeTime)
        {
            if (!IsRaining)
            {
                SetCloudy(cloudyDayMode);
                nextWeatherChangeTime += VersusGameplayManager.GetVersusModeConfig().CloudyDayArenaConfig.RainPhaseTime;
            }
            else
            {
                SetSunny(cloudyDayMode);
                nextWeatherChangeTime += VersusGameplayManager.GetVersusModeConfig().CloudyDayArenaConfig.SunnyPhaseTime;
            }
            cloudyDayMode.m_wasMessageDisplayed = false;
        }
    }

    private static void SetCloudy(CloudyDayMode cloudyDayMode)
    {
        cloudyDayMode.m_currentWeatherChange = 1;
        cloudyDayMode.m_nextWeatherChangeWave = 0;
        CoroutineManager.Instance.StartCoroutine(CoroutineUtils.ExecuteAfterDelay(3f, () =>
        {
            IsRaining = true;
            UpdateNocturnalPlants(true);
            UpdateRefreshTimes(cloudyDayMode);
            UpdateSeedPackets(cloudyDayMode, true);
        }));
    }

    private static void SetSunny(CloudyDayMode cloudyDayMode)
    {
        cloudyDayMode.m_currentWeatherChange = 0;
        cloudyDayMode.m_nextWeatherChangeWave = -1;
        CoroutineManager.Instance.StartCoroutine(CoroutineUtils.ExecuteAfterDelay(3f, () =>
        {
            IsRaining = false;
            UpdateNocturnalPlants(false);
            UpdateRefreshTimes(cloudyDayMode);
            UpdateSeedPackets(cloudyDayMode, false);
        }));
    }

    private static void UpdateNocturnalPlants(bool awake)
    {
        foreach (var plant in Instances.GameplayActivity.Board.GetPlants())
        {
            if (!Plant.IsNocturnal(plant.mSeedType))
                continue;

            plant.SetSleeping(!awake);
        }
    }

    private static void UpdateRefreshTimes(CloudyDayMode cloudyDayMode)
    {
        foreach (var seedBank in cloudyDayMode.m_board.SeedBanks.m_values)
        {
            foreach (var seedPacket in seedBank.SeedPackets)
            {
                if (seedPacket.mPacketType == SeedType.None)
                    continue;

                if (!seedPacket.mRefreshing)
                    continue;

                seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedPacket.mPacketType);
            }

        }
    }

    private static void UpdateSeedPackets(CloudyDayMode cloudyDayMode, bool cloudy)
    {
        foreach (var seedBank in cloudyDayMode.m_board.SeedBanks.m_values)
        {
            foreach (var seedPacket in seedBank.mSeedPackets)
            {
                if (VersusGameplayManager.GetVersusModeConfig()
                    .DisabledSeedPacketsInSuddenDeath.Contains(seedPacket.PacketType)
                    && VersusState.VersusPhase == VersusPhase.SuddenDeath)
                    continue;

                if (!VersusGameplayManager.GetVersusModeConfig()
                    .CloudyDayArenaConfig.DisabledSeedPacketsInRain.Contains(seedPacket.PacketType))
                    continue;

                seedBank.SetSeedPacketDisabled(seedPacket, cloudy);
            }
        }
    }

    internal static int GetCostReduction(SeedType seedType, int cost)
    {
        var config = VersusGameplayManager.GetVersusModeConfig().CloudyDayArenaConfig;

        if (VersusState.VersusTimeSynced > 30f && IsRaining)
        {
            float reducedCost = cost * config.CostReductionMultiplier;

            if (config.CostReductionRoundStep > 0)
            {
                return (int)(Math.Round(reducedCost / config.CostReductionRoundStep, MidpointRounding.AwayFromZero) * config.CostReductionRoundStep);
            }

            return (int)Math.Round(reducedCost, MidpointRounding.AwayFromZero);
        }
        else
        {
            return cost;
        }
    }

    internal static void ApplyRefreshTimeReduction(SeedType seedType, ref int refreshTime)
    {
        if (VersusState.ArenaSynced != ArenaType.CloudyDay)
            return;

        if (VersusState.VersusTimeSynced <= 30f)
            return;

        if (!IsRaining)
            return;

        var config = VersusGameplayManager.GetVersusModeConfig().CloudyDayArenaConfig;

        refreshTime = Mathf.Min(config.RefreshTimeMaxValue, (int)(Math.Pow(refreshTime, config.RefreshTimePower) * config.RefreshTimeMultiplier));
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;
}
