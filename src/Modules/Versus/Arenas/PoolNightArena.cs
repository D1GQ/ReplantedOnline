using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class PoolNightArena : PoolArena
{
    /// <inheritdoc/>
    public override ArenaTypes Type => ArenaTypes.PoolNight;

    /// <inheritdoc/>
    public override MusicTune Music => MusicTune.FogRigormormist;

    /// <inheritdoc/>
    public override LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea4Level2");
    }

    /// <inheritdoc/>
    public override void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Fog;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }
}
