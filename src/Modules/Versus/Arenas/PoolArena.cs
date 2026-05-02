using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal class PoolArena : IArena, IArenaData, IArenaSetupSeedbank
{
    /// <inheritdoc/>
    public virtual ArenaTypes Type => ArenaTypes.Pool;

    /// <inheritdoc/>
    public virtual MusicTune Music => MusicTune.MinigameLoonboon;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.RiseFromGround;

    /// <inheritdoc/>
    public int SeedPacketCount => 7;

    /// <inheritdoc/>
    public int StartingSeedPacketCount => 2;

    /// <inheritdoc/>
    public SeedType[] QuickPlayPlants
    {
        get
        {
            field ??=
                [SeedType.Sunflower, SeedType.Lilypad, SeedType.Peashooter,
                SeedType.Potatomine, SeedType.Wallnut, SeedType.Jalapeno,
                SeedType.Squash];
            return field;
        }
    }

    /// <inheritdoc/>
    public SeedType[] QuickPlayZombies
    {
        get
        {
            field ??=
                [SeedType.ZombieGravestone, SeedType.ZombieNormal, SeedType.ZombieTrashCan,
                SeedType.ZombieTrafficCone, SeedType.ZombiePolevaulter, SeedType.ZombieFootball,
                SeedType.ZombieFlag];
            return field;
        }
    }

    /// <inheritdoc/>
    public virtual LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea3Level2");
    }

    /// <inheritdoc/>
    public virtual void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Pool;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public virtual void InitializeArena(VersusMode versusMode)
    {
        if (ReplantedLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 5, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 4, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 4, true);
        }
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode) { }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;
}
