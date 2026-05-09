using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Patches.Gameplay.Versus.Arenas;
using UnityEngine;

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

    /// <inheritdoc/>
    public override void InitializeArena(VersusMode versusMode)
    {
        base.InitializeArena(versusMode);

        if (VersusState.AmPlantSide)
        {
            versusMode.m_app.BackgroundController.FogController.SetTargetOriginal(2, TodCurves.EaseIn, 0, 5);
        }
        else if (VersusState.AmZombieSide)
        {
            var pos = versusMode.m_app.BackgroundController.FogController.transform.localPosition;
            versusMode.m_app.BackgroundController.FogController.transform.localPosition = new Vector3(0f, pos.y, pos.z);
            versusMode.m_app.BackgroundController.FogController.transform.localScale = new Vector3(-1f, 1f, 1f);
            versusMode.m_app.BackgroundController.FogController.SetTargetOriginal(7, TodCurves.EaseIn, 0, 5);
        }
    }

    /// <inheritdoc/>
    public override void UpdateArena(VersusMode versusMode)
    {
        base.UpdateArena(versusMode);

        if (VersusState.AmPlantSide)
        {
            UpdateFogPlants();
        }
        else if (VersusState.AmZombieSide)
        {
            UpdateFogZombies();
        }
    }

    private void UpdateFogPlants()
    {

    }

    private void UpdateFogZombies()
    {

    }
}
