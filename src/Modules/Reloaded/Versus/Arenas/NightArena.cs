using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

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
        return LevelEntries.GetLevel("Level-AdventureArea2Level2")!;
    }

    /// <inheritdoc/>
    public override void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Night;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public override CustomRecommentedFlags GetSeedTypeCustomRecommentedFlags(SeedType seedType)
    {
        if (seedType == SeedType.Seashroom)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        return IArenaData.GetDefaultRecommentedFlags(seedType, Type);
    }

    /// <inheritdoc/>
    public override void SetSeedPacketDefinition(PlantDefinition seedPacketDefinition)
    {
        seedPacketDefinition.m_versusCost = SeedPacketDefinitions.BaseSeedVersusCost[seedPacketDefinition.SeedType];

        if (Plant.IsNocturnal(seedPacketDefinition.SeedType))
        {
            // Add Cost of instant coffee to balance price
            seedPacketDefinition.m_versusCost += 25;
        }
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
}
