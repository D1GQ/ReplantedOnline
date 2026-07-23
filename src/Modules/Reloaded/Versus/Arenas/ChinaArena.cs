using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

[RegisterArena(ArenaType.China)]
internal sealed class ChinaArena : IArena, IArenaData, IArenaSetupSeedbank
{
    /// <inheritdoc/>
    public MusicTune Music => MusicTune.RoofGrazetheroof;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.BungeeDropZombieNoTarget;

    /// <inheritdoc/>
    public int SeedPacketCount => 7;

    /// <inheritdoc/>
    public SeedType[] QuickPlayPlants
    {
        get
        {
            field ??=
                [SeedType.Sunflower, SeedType.Flowerpot, SeedType.Peashooter,
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
                SeedType.ZombieTrafficCone, SeedType.ZombieFootball, SeedType.ZombieBungee,
                SeedType.ZombieFlag];
            return field;
        }
    }

    /// <inheritdoc/>
    public LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-China")!;
    }

    /// <inheritdoc/>
    public Sprite GetThumbnail()
    {
        return ReplantedOnlineMod.Assets.Sprites.Arena.ChinaThumbnail;
    }

    /// <inheritdoc/>
    public void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.China;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public CustomRecommentedFlags GetSeedTypeCustomRecommentedFlags(SeedType seedType)
    {
        if (seedType == SeedType.Flowerpot)
        {
            return CustomRecommentedFlags.Required | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.Spikeweed)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        return IArenaData.GetDefaultRecommentedFlags(seedType, ArenaType.China);
    }

    /// <inheritdoc/>
    public void SetSeedPacketDefinition(PlantDefinition seedPacketDefinition)
    {
    }

    /// <inheritdoc/>
    public void InitializeArena(VersusMode versusMode)
    {
        if (ReloadedLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, true);

            for (int column = 0; column < 3; column++)
            {
                for (int row = 0; row < versusMode.m_board.GetNumRows(); row++)
                {
                    SeedPacketDefinitions.SpawnPlant(SeedType.Flowerpot, column, row, true);
                }
            }

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 3, true);
        }

        // Add bowling line
        var line = PvZRUtils.CreateBowlingLine(ReplantedOnlineMod.Assets.Sprites.Arena.ChinaBowlingline);
        line.color = new(0.2f, 0.9f, 0.8f, 0.5f);
        line.transform.localPosition = new Vector3(0f, -1008.732f, -1f);
        line.transform.localScale = new Vector3(100f, 100f, 1f);

        _pushBackEventTimer.Set(30f - ReplantedOnlineMod.Constants.Reloaded.VERSUS_PRECOUNTDOWN_TIME);
    }

    private readonly UnityTimer _pushBackEventTimer = new();
    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode)
    {
        versusMode.m_board.mApp.BackgroundController.EnableBowlingLine(true, 682);

        if (!ReloadedLobby.AmLobbyHost()) return;

        if (_pushBackEventTimer.HasElapsed(2, 00f))
        {
            _pushBackEventTimer.Reset();
            ArenaEvents.PushBackEvent();
        }
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY)
    {
        if (!Challenge.IsZombieSeedType(seedType) && seedType != SeedType.Flowerpot)
        {
            if (Instances.GameplayActivity.Board.GetFlowerPotAt(gridX, gridY) == null)
            {
                return false;
            }
        }

        return true;
    }
}
