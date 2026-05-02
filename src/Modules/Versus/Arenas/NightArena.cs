using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;

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
}
