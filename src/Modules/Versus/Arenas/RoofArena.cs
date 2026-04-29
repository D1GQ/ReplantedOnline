using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using System.Reflection;
using UnityEngine;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal class RoofArena : IArena, IArenaData, IArenaSetupSeedbank
{
    /// <inheritdoc/>
    public virtual ArenaTypes Type => ArenaTypes.Roof;

    /// <inheritdoc/>
    public MusicTune Music => MusicTune.RoofGrazetheroof;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.ZombieWithBungee;

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
                SeedType.Kernelpult, SeedType.Tallnut, SeedType.Jalapeno,
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
                SeedType.ZombieLadder, SeedType.ZombieBungee, SeedType.ZombieGargantuar,
                SeedType.ZombieFlag];
            return field;
        }
    }

    /// <inheritdoc/>
    public virtual LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea5Level2");
    }

    /// <inheritdoc/>
    public virtual Sprite GetThumbnail()
    {
        return Assembly.GetExecutingAssembly().LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Roofday.png");
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
        if (ReplantedLobby.AmLobbyHost())
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

        for (int row = 0; row < versusMode.m_board.GetNumRows(); row++)
        {
            var lawMower = versusMode.m_board.m_lawnMowers.DataArrayAlloc();
            lawMower.LawnMowerInitialize(row, versusMode.m_board.mApp);
        }
    }

    /// <inheritdoc/>
    public virtual void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets)
    {
        foreach (var seedPacket in seedPackets)
        {
            if (SeedPacketDefinitions.IgnoreInitialCooldown.Contains(seedPacket.mPacketType)) continue;

            seedPacket.Deactivate();
            var time = Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            // Start at least with a 10 second cooldown 
            seedPacket.mRefreshTime = Math.Max(time, ReplantedOnlineMod.Constants.DAY_SEEDPACKET_MIN_INITIAL_COOLDOWN);
            seedPacket.mRefreshing = true;
        }
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode) { }

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
