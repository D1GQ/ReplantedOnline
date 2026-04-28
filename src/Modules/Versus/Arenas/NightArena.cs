using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class NightArena : IArena, IArenaData
{
    /// <inheritdoc/>
    public ArenaTypes Type => ArenaTypes.Night;

    /// <inheritdoc/>
    public MusicTune Music => MusicTune.PuzzleCerebrawl;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.RiseFromGround;

    /// <inheritdoc/>
    public LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea2Level2");
    }

    /// <inheritdoc/>
    public void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Night;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public void InitializeArena(VersusMode versusMode)
    {
        if (ReplantedLobby.AmLobbyHost())
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

        versusMode.m_board.AddAGraveStone(5, 0);
        versusMode.m_board.AddAGraveStone(5, 1);
        versusMode.m_board.AddAGraveStone(5, 2);
        versusMode.m_board.AddAGraveStone(5, 3);
        versusMode.m_board.AddAGraveStone(5, 4);
        versusMode.m_board.mEnableGraveStones = true;
    }

    /// <inheritdoc/>
    public void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets)
    {
        foreach (var seedPacket in seedPackets)
        {
            if (seedPacket.mPacketType is SeedType.Sunflower or SeedType.Sunshroom or SeedType.ZombieGravestone) continue;

            seedPacket.Deactivate();
            var time = Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            // Start at least with a 15 second cooldown 
            seedPacket.mRefreshTime = Math.Max(time, 1500);
            seedPacket.mRefreshing = true;
        }
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode) { }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;
}
