using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Arenas;
using UnityEngine;

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
            versusMode.m_app.BackgroundController.FogController.SetTargetOriginal(0, TodCurves.Constant, 0, 0);
        }
        else if (VersusState.AmZombieSide)
        {
            versusMode.m_app.BackgroundController.FogController.transform.localScale = new Vector3(-1f, 1f, 1f);
            versusMode.m_app.BackgroundController.FogController.SetTargetOriginal(0, TodCurves.Constant, 0, 0);
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

    private float VersusTime => Instances.GameplayActivity.VersusMode.m_versusTime;
    private static readonly float VersusTimeEnd = VersusMode.k_suddenDeathStartTime - 3f;

    private readonly float OffScreenPlantFog = 4200f;
    private readonly float StartPlantFog = 2100f;
    private readonly float EndPlantFog = 3100f;

    private void UpdateFogPlants()
    {
        if (VersusTime < 3f)
        {
            SetFogPos(Mathf.Lerp(OffScreenPlantFog, StartPlantFog, VersusTime / 3f));
        }
        else
        {
            SetFogPos(Mathf.Lerp(StartPlantFog, EndPlantFog, VersusTime / VersusTimeEnd));
        }
    }

    private readonly float OffScreenZombieFog = 0f;
    private readonly float StartZombieFog = 2393f;
    private readonly float EndZombieFog = 1041f;

    private void UpdateFogZombies()
    {
        if (VersusTime < 3f)
        {
            SetFogPos(Mathf.Lerp(OffScreenZombieFog, StartZombieFog, VersusTime / 3f));
        }
        else
        {
            SetFogPos(Mathf.Lerp(StartZombieFog, EndZombieFog, VersusTime / VersusTimeEnd));
        }
    }

    private static void SetFogPos(float pos)
    {
        var fog = Instances.GameplayActivity.BackgroundController.FogController;
        fog.transform.localPosition = new Vector3(pos, -1150f, 0f);
    }
}
