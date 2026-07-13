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

[RegisterArena]
internal class RoofArena : IArena, IArenaData, IArenaSetupSeedbank
{
    /// <inheritdoc/>
    public virtual ArenaTypes Type => ArenaTypes.Roof;

    /// <inheritdoc/>
    public virtual MusicTune Music => MusicTune.RoofGrazetheroof;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.BungeeDropZombieNoTarget;

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
                [SeedType.Sunflower, SeedType.Flowerpot, SeedType.Cabbagepult,
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
                [SeedType.ZombieGravestone, SeedType.ZombieNormal, SeedType.ZombieTrafficCone,
                SeedType.ZombieFootball, SeedType.ZombieCatapult, SeedType.ZombieGargantuar,
                SeedType.ZombieFlag];
            return field;
        }
    }

    /// <inheritdoc/>
    public bool IsSeedTypeAllowedInRandomGamemode(SeedType seedType)
    {
        if (seedType is SeedType.Peashooter or SeedType.Repeater or SeedType.Snowpea or SeedType.Threepeater)
        {
            return false;
        }

        if (seedType is SeedType.Puffshroom or SeedType.Fumeshroom or SeedType.Scaredyshroom)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public virtual LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea5Level2")!;
    }

    /// <inheritdoc/>
    public virtual Sprite GetThumbnail()
    {
        return ReplantedOnlineMod.Assets.Sprites.Arena.RoofDayThumbnail;
    }

    /// <inheritdoc/>
    public virtual void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Roof;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
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
        var line = PvZRUtils.CreateBowlingLine(ReplantedOnlineMod.Assets.Sprites.Arena.RoofBowlingline);
        line.transform.localPosition = new Vector3(0f, -861.1128f, -1f);
        line.transform.localScale = new Vector3(100f, 100f, 1f);
        if (!Type.IsArenaAtNight())
        {
            line.color = new(0.6f, 0f, 1f, 0.3f);
        }
        else
        {
            line.color = new(0.8f, 0f, 0f, 0.5f);
        }

        _pushBackEventTimer.Reset();
    }

    private readonly UnityTimer _pushBackEventTimer = new();
    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode)
    {
        versusMode.m_board.mApp.BackgroundController.EnableBowlingLine(true, 680);

        if (!ReloadedLobby.AmLobbyHost()) return;

        if (_pushBackEventTimer.HasElapsed(1, 30f))
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
