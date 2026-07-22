using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

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
        return LevelEntries.GetLevel("Level-AdventureArea4Level2")!;
    }

    /// <inheritdoc/>
    public override void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Fog;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    private const float COLUMN_TO_FOG_UNITS = 250f;
    private const float FOG_PUSH_TIME_SURPLUS = 90f;
    private static float nextFogPushTime;
    internal static int NextFogPos;

    public override void InitializeArena(VersusMode versusMode)
    {
        base.InitializeArena(versusMode);
        nextFogPushTime = FOG_PUSH_TIME_SURPLUS;
        NextFogPos = 4;
    }

    /// <inheritdoc/>
    public override void UpdateArena(VersusMode versusMode)
    {
        base.UpdateArena(versusMode);

        if (!VersusState.AmPlantSide)
        {
            // Make fog transparent
            foreach (var row in versusMode.m_board.mApp.BackgroundController.m_fogController.m_rows)
            {
                foreach (var tile in row.tiles)
                {
                    tile.m_initialAlpha = 0.2f;
                }
            }
        }

        if (NextFogPos > 1)
        {
            if (VersusState.VersusTimeSynced > nextFogPushTime)
            {
                nextFogPushTime += FOG_PUSH_TIME_SURPLUS;
                versusMode.m_board.mApp.BackgroundController.m_fogController.ScheduleTarget(NextFogPos--, TodCurves.EaseInOut);
            }
        }
    }
}
