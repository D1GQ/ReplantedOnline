using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

[RegisterArena]
internal sealed class CloudyDayArena : IArena, IArenaData
{
    /// <inheritdoc/>
    public ArenaTypes Type => ArenaTypes.CloudyDay;

    /// <inheritdoc/>
    public MusicTune Music => MusicTune.MinigameLoonboon;

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
    public void InitializeArena(VersusMode versusMode)
    {
        IsRaining = false;
        nextWeatherChangeTime = 60f;

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
    private void UpdateWeather(CloudyDayMode cloudyDayMode)
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
                nextWeatherChangeTime += 30f;
            }
            else
            {
                SetSunny(cloudyDayMode);
                nextWeatherChangeTime += 60f;
            }
            cloudyDayMode.m_wasMessageDisplayed = false;
        }
    }

    private static void SetCloudy(CloudyDayMode cloudyDayMode)
    {
        cloudyDayMode.m_currentWeatherChange = 1;
        cloudyDayMode.m_nextWeatherChangeWave = 0;
        cloudyDayMode.m_app.StartCoroutine(CoroutineUtils.ExecuteAfterDelay(3f, () =>
        {
            IsRaining = true;
            UpdateNocturnalPlants(true);
            UpdateRefreshTimes();
        }));
    }

    private static void SetSunny(CloudyDayMode cloudyDayMode)
    {
        cloudyDayMode.m_currentWeatherChange = 0;
        cloudyDayMode.m_nextWeatherChangeWave = -1;
        cloudyDayMode.m_app.StartCoroutine(CoroutineUtils.ExecuteAfterDelay(3f, () =>
        {
            IsRaining = false;
            UpdateNocturnalPlants(false);
            UpdateRefreshTimes();
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

    private static void UpdateRefreshTimes()
    {
        foreach (var seedPacket in PvZRUtils.GetPlantSeedBankInfo().mSeedBank.SeedPackets)
        {
            if (!seedPacket.mRefreshing)
                continue;

            seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedPacket.mPacketType);
        }

        foreach (var seedPacket in PvZRUtils.GetZombieSeedBankInfo().mSeedBank.SeedPackets)
        {
            if (!seedPacket.mRefreshing)
                continue;

            seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedPacket.mPacketType);
        }
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;
}
