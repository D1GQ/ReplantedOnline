using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

[RegisterArena]
internal class PoolArena : IArena, IArenaData, IArenaSetupSeedbank
{
    /// <inheritdoc/>
    public virtual ArenaTypes Type => ArenaTypes.Pool;

    /// <inheritdoc/>
    public virtual MusicTune Music => MusicTune.PoolWaterygraves;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.RiseFromGround;

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
                [SeedType.Sunflower, SeedType.Lilypad, SeedType.Peashooter,
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
                SeedType.ZombieTrafficCone, SeedType.ZombiePolevaulter, SeedType.ZombieFootball,
                SeedType.ZombieFlag];
            return field;
        }
    }

    /// <inheritdoc/>
    public virtual LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea3Level2");
    }

    /// <inheritdoc/>
    public virtual void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Pool;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public virtual void InitializeArena(VersusMode versusMode)
    {
        if (ReloadedLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 5, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 4, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Lilypad, 0, 2, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Lilypad, 0, 3, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 4, true);

            _pushBackEventTimer.Set(30f);
        }

        // Add bowling line
        SpriteRenderer poolSpriteRenderer;
        if (!Type.IsArenaAtNight())
        {
            poolSpriteRenderer = versusMode.m_app.BackgroundController.transform.Find("Background/Area3_PoolWater_Overlay")?.GetComponent<SpriteRenderer>();
        }
        else
        {
            poolSpriteRenderer = versusMode.m_app.BackgroundController.transform.Find("Background/Area4_PoolWater_Overlay")?.GetComponent<SpriteRenderer>();
        }

        if (poolSpriteRenderer != null)
        {
            var lineSprite = ModInfo.Assembly.LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Bowlinglines.Poolline.png", 100f);

            var line = PvZRUtils.CreateBowlingLine(lineSprite, false);
            var mask = line.gameObject.AddComponent<SpriteRendererMask>();
            mask.SetMask(poolSpriteRenderer);
            mask.SetInteraction(SpriteMaskInteraction.VisibleOutsideMask);
            line.transform.localPosition = new Vector3(0f, -1004.427f, -1f);
            line.transform.localScale = new Vector3(100f, 100f, 1f);
            line.sortingLayerID = SortingLayer.NameToID(Il2CppReloaded.Constants.SortingLayer.BACKGROUND);
            line.sortingOrder = 2;
            if (Type.IsArenaAtNight())
            {
                line.color = new(0.8f, 0.7f, 1f);
            }

            var linePoolOverlayGo = new GameObject("LinePoolOverlay");
            linePoolOverlayGo.transform.SetParent(line.transform);
            linePoolOverlayGo.transform.localPosition = Vector3.zero;
            linePoolOverlayGo.transform.localScale = Vector3.one;
            var linePoolOverlay = linePoolOverlayGo.AddComponent<SpriteRenderer>();
            linePoolOverlay.sprite = lineSprite;
            var maskPoolOverlay = linePoolOverlay.gameObject.AddComponent<SpriteRendererMask>();
            maskPoolOverlay.SetMask(poolSpriteRenderer);
            maskPoolOverlay.SetInteraction(SpriteMaskInteraction.VisibleInsideMask);
            linePoolOverlay.sortingLayerID = SortingLayer.NameToID(Il2CppReloaded.Constants.SortingLayer.BACKGROUND);
            linePoolOverlay.sortingOrder = 2;
            linePoolOverlay.color = new(0.3f, 0.3f, 0.3f, 0.2f);
        }
    }

    private readonly UnityTimer _pushBackEventTimer = new();
    /// <inheritdoc/>
    public virtual void UpdateArena(VersusMode versusMode)
    {
        versusMode.m_board.mApp.BackgroundController.EnableBowlingLine(true, 515);

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
        if (Instances.GameplayActivity.Board.mPlantRow[gridY] == PlantRowType.Pool)
        {
            if (seedType is SeedType.Zomboni or SeedType.ZombieCatapult or SeedType.ZombieDigger or SeedType.ZombiePogo or SeedType.ZombiePolevaulter or SeedType.ZombieBalloon)
            {
                return false;
            }
        }

        return true;
    }
}