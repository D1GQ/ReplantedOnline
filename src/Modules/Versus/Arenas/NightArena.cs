using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class NightArena : DayArena
{
    /// <inheritdoc/>
    public override ArenaTypes Type => ArenaTypes.Night;

    /// <inheritdoc/>
    public override MusicTune Music => MusicTune.PuzzleCerebrawl;

    /// <inheritdoc/>
    public override LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea2Level2");
    }

    /// <inheritdoc/>
    public override void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Night;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public override void InitializeArena(VersusMode versusMode)
    {
        base.InitializeArena(versusMode);

        versusMode.m_board.AddAGraveStone(5, 0);
        versusMode.m_board.AddAGraveStone(5, 1);
        versusMode.m_board.AddAGraveStone(5, 2);
        versusMode.m_board.AddAGraveStone(5, 3);
        versusMode.m_board.AddAGraveStone(5, 4);
        versusMode.m_board.mEnableGraveStones = true;
    }

    /// <inheritdoc/>
    public override void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets)
    {
        foreach (var seedPacket in seedPackets)
        {
            if (SeedPacketDefinitions.IgnoreInitialCooldown.Contains(seedPacket.mPacketType)) continue;

            seedPacket.Deactivate();
            var time = Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            // Start at least with a 15 second cooldown 
            seedPacket.mRefreshTime = Math.Max(time, ReplantedOnlineMod.Constants.NIGHT_SEEDPACKET_MIN_INITIAL_COOLDOWN);
            seedPacket.mRefreshing = true;
        }
    }
}
