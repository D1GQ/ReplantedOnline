using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Data.Asset.Unity;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Data;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Il2Cpp;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;
using static Il2CppReloaded.Constants;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Modules.Reloaded.Versus;

/// <summary>
/// Handles seed packet definition modifications including versus costs, refresh times,
/// and spawning logic for both plants and zombies in versus mode.
/// </summary>
internal static class SeedPacketDefinitions
{
    /// <summary>
    /// Gets the <see cref="SeedType"/> used as a placeholder for hidden seed packets.
    /// </summary>
    internal static readonly SeedType RandomHiddenSeedType = SeedType.SlotMachineDiamond;

    /// <summary>
    /// Gets a collection of seed types that are not considered valid or real seeds.
    /// </summary>
    internal static readonly SeedType[] NoneSeedTypes = [
        SeedType.NumSeedsInChooser,
        SeedType.NumSeedTypes,
        SeedType.LastZombieIndex,
        SeedType.None
    ];

    /// <summary>
    /// Gets a collection of seed types that are disabled and cannot be used in gameplay.
    /// </summary>
    internal static readonly SeedType[] HideInChooserSeedTypes = [
        // Plants
        SeedType.Marigold
    ];

    /// <summary>
    /// Gets a collection of seed types that produce currency (sun for plants or brains for zombies).
    /// </summary>
    internal static readonly SeedType[] CurrencyProducingSeedTypes = [
        // Plants
        SeedType.Sunflower,
        SeedType.Sunshroom,

        // Zombies
        SeedType.ZombieGravestone
    ];

    /// <summary>
    /// Gets a lookup dictionary that maps an upgraded seed type back to its original seed type.
    /// </summary>
    internal static readonly Dictionary<SeedType, SeedType> UpgradeToSeedTypeLookup = new()
    {
        { SeedType.Twinsunflower, SeedType.Sunflower }
    };

    /// <summary>
    /// Gets a collection of seed types that are asleep by default and require waking.
    /// </summary>
    internal static readonly SeedType[] SleepingPlants = [.. Enum.GetValues<SeedType>().Where(Plant.IsNocturnal)];

    /// <summary>
    /// Gets a lookup of base plant definitions containing original values for cost, refresh time, and sudden death refresh time.
    /// </summary>
    internal static readonly Dictionary<SeedType, (int Cost, int RefreshTime, int SuddenDeathRefreshTime)> BasePlantDefinitions = [];

    /// <summary>
    /// Gets a lookup of base zombie definitions containing original values for body health, armor health, and easter egg chance.
    /// </summary>
    internal static readonly Dictionary<ZombieType, (int BodyHealth, int ArmorHealth, float EasterEggChance100)> BaseZombieDefinitions = [];

    /// <summary>
    /// Initializes plant and zombie definitions and applies custom modifications for versus mode.
    /// </summary>
    internal static void Initialize()
    {
        // Replace seed packet icon for hidden seed packet
        var slotMachineDiamondDef = Instances.IDataService.GetPlantDefinition(RandomHiddenSeedType);
        var slotMachineDiamondAssetOverride = new AssetReferenceOverride<Sprite>(slotMachineDiamondDef.m_versusImage);
        IAssetReferenceOverride.Register(slotMachineDiamondAssetOverride);
        slotMachineDiamondAssetOverride.SetOverride(ReplantedOnlineMod.Assets.Sprites.SeedPacket.HiddenSeedPacketIcon.Asset, ReloadedLobby.AmInLobby);

        CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieSnorkel, "SNORKEL_ZOMBIE",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.SnorkelSeedPacketIcon.Asset);

        CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieDolphinRider, "DOLPHIN_RIDER_ZOMBIE",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.DolphinriderSeedPacketIcon.Asset);

        var BackupDancerDefinition = CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieBackupDancer, "BACKUP_DANCER",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.BackupDancerSeedPacketIcon.Asset,
            "A quirky backup dancer that gives a stackable speed boost to nearby zombies.");
        BackupDancerDefinition!.m_previewSpriteScale = 0.82f;

        CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieYeti, "ZOMBIE_YETI",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.YetiSeedPacketIcon.Asset,
            "A curious creature that can be enraged!");

        foreach (var seedDefinition in Instances.IDataService.PlantDefinitions.EnumerateIl2CppReadonlyList())
        {
            BasePlantDefinitions[seedDefinition.SeedType] = (seedDefinition.VersusCost, seedDefinition.VersusBaseRefreshTime, seedDefinition.VersusSuddenDeathRefreshTime);
        }

        foreach (var zombieDefinition in Instances.IDataService.ZombieDefinitions.EnumerateIl2CppReadonlyList())
        {
            BaseZombieDefinitions[zombieDefinition.ZombieType] = (zombieDefinition.VersusBodyHealth, zombieDefinition.VersusBodyHealth, zombieDefinition.EasterEggChance);
        }
    }

    /// <summary>
    /// Updates plant and zombie definitions from the loaded configuration data for the current arena.
    /// </summary>
    /// <param name="arenaType">The current arena type, used to determine if nocturnal cost surplus should be applied.</param>
    internal static void UpdateDefinitionsFromConfigs(ArenaType arenaType = ArenaType.Day)
    {
        foreach (var seedDefinition in Instances.IDataService.PlantDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (VersusGameplayManager.GetVersusModeConfig().TryGetSeedPacketConfig(seedDefinition.SeedType, out var config))
            {
                seedDefinition.m_versusCost = config.Cost;
                if (arenaType.IsArenaAtNight() && Plant.IsNocturnal(seedDefinition.SeedType))
                {
                    seedDefinition.m_versusCost += config.NocturnalCostSurplus;
                }
                seedDefinition.m_versusBaseRefreshTime = config.RefreshTime;
                seedDefinition.m_versusSuddenDeathRefreshTime = config.SuddenDeathRefresh;
            }
        }

        foreach (var zombieDefinition in Instances.IDataService.ZombieDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (VersusGameplayManager.GetVersusModeConfig().TryGetZombieConfig(zombieDefinition.ZombieType, out var config))
            {
                zombieDefinition.m_versusBodyHealth = config.BodyHealth;
                zombieDefinition.m_versusArmorHealth = config.ArmorHealth;
                zombieDefinition.m_easterEggChance100 = 0;
            }
        }
    }

    /// <summary>
    /// Resets all plant and zombie definitions back to their base (original) values.
    /// </summary>
    internal static void ResetDefinitions()
    {
        foreach (var seedDefinition in Instances.IDataService.PlantDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (BasePlantDefinitions.TryGetValue(seedDefinition.SeedType, out var basePlantDef))
            {
                seedDefinition.m_versusCost = basePlantDef.Cost;
                seedDefinition.m_versusBaseRefreshTime = basePlantDef.RefreshTime;
                seedDefinition.m_versusSuddenDeathRefreshTime = basePlantDef.SuddenDeathRefreshTime;
            }
        }

        foreach (var zombieDefinition in Instances.IDataService.ZombieDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (BaseZombieDefinitions.TryGetValue(zombieDefinition.ZombieType, out var baseZombieDef))
            {
                zombieDefinition.m_versusBodyHealth = baseZombieDef.BodyHealth;
                zombieDefinition.m_versusArmorHealth = baseZombieDef.ArmorHealth;
                zombieDefinition.m_easterEggChance100 = baseZombieDef.EasterEggChance100;
            }
        }
    }

    /// <summary>
    /// Places a seed on the board, handling both plant and zombie seeds appropriately.
    /// </summary>
    /// <param name="seedType">The type of seed to place.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this object.</param>
    /// <returns>The spawned <see cref="ReloadedObject"/> (either Plant or Zombie).</returns>
    internal static ReloadedObject PlaceSeed(SeedType seedType, BoardUnitX boardUnitX, BoardUnitY boardUnitY, bool spawnOnNetwork)
    {
        // Check if this is a zombie seed (from I, Zombie mode)
        // Zombie seeds have special handling since they spawn zombies instead of plants
        if (Challenge.IsZombieSeedType(seedType))
        {
            // Convert seed type to actual zombie type
            var type = Challenge.IZombieSeedTypeToZombieType(seedType);

            // Delegate to zombie spawning logic
            return SpawnZombie(type, boardUnitX, boardUnitY, spawnOnNetwork).Zombie;
        }
        else
        {
            // Handle upgrading plants
            if (Plant.IsUpgrade(seedType))
            {
                var plant = Instances.GameplayActivity.Board.GetTopPlantAt(boardUnitX, boardUnitY, PlantPriority.Any);
                if (plant != null)
                {
                    var plantNetworked = plant.GetNetworked();
                    if (plantNetworked != null)
                    {
                        plantNetworked.SendUpgradeRpc(seedType);
                        return plantNetworked.Plant!;
                    }
                }
            }

            // This is a regular plant seed - delegate to plant spawning logic
            return SpawnPlant(seedType, boardUnitX, boardUnitY, spawnOnNetwork).Plant;
        }
    }

    /// <summary>
    /// Spawns a plant at the specified grid coordinates.
    /// </summary>
    /// <param name="seedType">The type of seed to spawn as a plant.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this plant.</param>
    /// <returns>A tuple containing the spawned <see cref="Plant"/> and optional <see cref="PlantNetworked"/> controller.</returns>
    internal static (Plant Plant, PlantNetworked? PlantNetworked) SpawnPlant(SeedType seedType, BoardUnitX boardUnitX, BoardUnitY boardUnitY, bool spawnOnNetwork)
    {
        return SpawnPlant(seedType, boardUnitX, boardUnitY, SpawnType.None, spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a plant at the specified grid coordinates with a specific spawn type.
    /// </summary>
    /// <param name="seedType">The type of seed to spawn as a plant.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnType">The type of spawning to use.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this plant.</param>
    /// <returns>A tuple containing the spawned <see cref="Plant"/> and optional <see cref="PlantNetworked"/> controller.</returns>
    internal static (Plant Plant, PlantNetworked? PlantNetworked) SpawnPlant(SeedType seedType, BoardUnitX boardUnitX, BoardUnitY boardUnitY, SpawnType spawnType, bool spawnOnNetwork)
    {
        // Create the actual plant object in the game world using the original game method
        var plant = Instances.GameplayActivity.Board.AddPlant(boardUnitX.Grid, boardUnitY.Grid, seedType, SeedType.None);

        // Update position visually in countdown
        plant.UpdateInternal();

        // Only create network controller if network synchronization is requested
        // This prevents creating network objects in single-player mode
        PlantNetworked? plantNetworked = null;
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this plant across all clients
            plantNetworked = SpawnPlantOnNetwork(plant, boardUnitX, boardUnitY, spawnType);
        }

        Instances.GameplayActivity.Board.m_plants.NewArrayItem(plant, plant.DataID);

        ICharacterLogic.OnPlantPlanted(plant, boardUnitX, boardUnitY);

        return (plant, plantNetworked);
    }

    /// <summary>
    /// Creates a networked controller for an existing plant to enable network synchronization.
    /// </summary>
    /// <param name="plant">The plant to create a network controller for.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned <see cref="PlantNetworked"/> controller object.</returns>
    internal static PlantNetworked SpawnPlantOnNetwork(Plant plant, BoardUnitX boardUnitX, BoardUnitY boardUnitY, Action<PlantNetworked>? callback = null)
    {
        return SpawnPlantOnNetwork(plant, boardUnitX, boardUnitY, SpawnType.None, callback);
    }

    /// <summary>
    /// Creates a networked controller for an existing plant to enable network synchronization.
    /// </summary>
    /// <param name="plant">The plant to create a network controller for.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnType">The type of spawning used for this plant.</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned <see cref="PlantNetworked"/> controller object.</returns>
    internal static PlantNetworked SpawnPlantOnNetwork(Plant plant, BoardUnitX boardUnitX, BoardUnitY boardUnitY, SpawnType spawnType, Action<PlantNetworked>? callback = null)
    {
        var networkObj = NetworkObject.SpawnNew<PlantNetworked>(net =>
        {
            net._p.SetTarget(() => plant?.mController?.m_plant);
            net.SeedType = plant.mSeedType;
            net.SpawnType = spawnType;
            net.BoardUnitX = boardUnitX;
            net.BoardUnitY = boardUnitY;
            callback?.Invoke(net);
        }, VersusState.PlantClientId);
        return networkObj!;
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>A tuple containing the spawned <see cref="Zombie"/> and optional <see cref="ZombieNetworked"/> controller.</returns>
    internal static (Zombie Zombie, ZombieNetworked? ZombieNetworked) SpawnZombie(ZombieType zombieType, BoardUnitX boardUnitX, BoardUnitY boardUnitY, bool spawnOnNetwork)
    {
        return SpawnZombie(zombieType, boardUnitX, boardUnitY, GetZombieSpawnType(zombieType, boardUnitX, boardUnitY), spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates with a specific spawn type.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnType">The type of spawning to use.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>A tuple containing the spawned <see cref="Zombie"/> and optional <see cref="ZombieNetworked"/> controller.</returns>
    internal static (Zombie Zombie, ZombieNetworked? ZombieNetworked) SpawnZombie(ZombieType zombieType, BoardUnitX boardUnitX, BoardUnitY boardUnitY, SpawnType spawnType, bool spawnOnNetwork)
    {
        // Add zombie to the board at the specified position
        bool spawnInBack = spawnType is SpawnType.Background or SpawnType.BackgroundAndShakeBushes || zombieType == ZombieType.BackupDancer;
        var zombie = Instances.GameplayActivity.Board.AddZombieAtCell(zombieType, spawnInBack ? 9 : boardUnitX.Grid, boardUnitY.Grid);

        bool canRise = !VersusState.IsInPreCountDown;

        if (spawnType == SpawnType.RiseFromGround)
        {
            if (canRise)
            {
                zombie.mZombiePhase = ZombiePhase.RisingFromGrave;
                zombie.mPhaseCounter = 150;
                Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_DIRT_RISE);
            }

            switch (zombieType)
            {
                case ZombieType.Gravestone:
                    if (canRise)
                        Instances.GameplayActivity.AddTodParticle(boardUnitX.Pos + 40, boardUnitY.Pos + 115, zombie.RenderOrder - 5, ParticleEffect.GraveStoneRise);
                    else
                        zombie.mPhaseCounter = 0;
                    zombie.mPosX = boardUnitX.Pos;
                    break;
                default:
                    if (canRise) Instances.GameplayActivity.AddTodParticle(boardUnitX.Pos + 60, boardUnitY.Pos + 115, zombie.RenderOrder - 5, ParticleEffect.ZombieRise);
                    zombie.mPosX = boardUnitX.Pos;
                    break;
            }
        }
        else if (spawnType == SpawnType.RiseFromPool)
        {
            zombie.mZombiePhase = ZombiePhase.RisingFromGrave;
            zombie.mPhaseCounter = 50;

            switch (zombieType)
            {
                case ZombieType.Gravestone:
                    // Handled in GravestoneNetworkComponent.cs
                    break;
                default:
                    zombie.mPosX = boardUnitX.Pos;
                    break;
            }
        }
        else if (spawnType is SpawnType.BungeeDropZombie or SpawnType.BungeeDropZombieNoTarget)
        {
            zombie.mPosX = boardUnitX.Pos;
        }
        else if (spawnType == SpawnType.FallFromSky)
        {
            Animations.PlayFallFromSky(zombie, boardUnitY.Grid);
            zombie.mPosX = boardUnitX.Pos;
        }
        else if (spawnType == SpawnType.BackgroundAndShakeBushes)
        {
            Instances.GameplayActivity.BackgroundController.ZombieSpawnedInRow(boardUnitY.Grid);
        }

        // Set Gravestone grid pos
        if (zombieType == ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Add(zombie);
            zombie.mGraveX = boardUnitX.Grid;
            zombie.mGraveY = boardUnitY.Grid;
        }

        // Set Bungee grid target
        if (zombieType == ZombieType.Bungee)
        {
            SetBungeeTarget(zombie, true);
            zombie.mTargetCol = boardUnitX.Grid;
            zombie.mTargetRow = boardUnitY.Grid;
        }

        // Update position visually in countdown
        zombie.UpdateReanim();

        // Only create network controller if network synchronization is requested
        ZombieNetworked? zombieNetworked = null;
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this zombie across all clients
            zombieNetworked = SpawnZombieOnNetwork(zombie, boardUnitX, boardUnitY, spawnType);
        }

        Instances.GameplayActivity.Board.m_zombies.NewArrayItem(zombie, zombie.DataID);

        ICharacterLogic.OnZombiePlanted(zombie, boardUnitX, boardUnitY);

        return (zombie, zombieNetworked);
    }

    /// <summary>
    /// Creates a networked controller for an existing zombie to enable network synchronization.
    /// </summary>
    /// <param name="zombie">The zombie to create a network controller for.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned <see cref="ZombieNetworked"/> controller object.</returns>
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY, Action<ZombieNetworked>? callback = null)
    {
        return SpawnZombieOnNetwork(zombie, boardUnitX, boardUnitY, GetZombieSpawnType(zombie.mZombieType, boardUnitX, boardUnitY), callback);
    }

    /// <summary>
    /// Creates a networked controller for an existing zombie to enable network synchronization.
    /// </summary>
    /// <param name="zombie">The zombie to create a network controller for.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <param name="spawnType">The type of spawning used for this zombie.</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned <see cref="ZombieNetworked"/> controller object.</returns>
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY, SpawnType spawnType, Action<ZombieNetworked>? callback = null)
    {
        var networkObj = NetworkObject.SpawnNew<ZombieNetworked>(net =>
        {
            net._z.SetTarget(() => zombie?.mController?.m_zombie);
            net.ZombieType = zombie.mZombieType;
            net.SpawnType = spawnType;
            net.BoardUnitX = boardUnitX;
            net.BoardUnitY = boardUnitY;
            callback?.Invoke(net);
        }, VersusState.PlantClientId);
        return networkObj!;
    }

    /// <summary>
    /// Determines the appropriate spawn type for a given zombie type based on its characteristics and current arena.
    /// </summary>
    /// <param name="zombieType">The type of zombie to evaluate.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <returns>The appropriate <see cref="SpawnType"/> for the zombie.</returns>
    internal static SpawnType GetZombieSpawnType(ZombieType zombieType, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        if (zombieType is ZombieType.Target or ZombieType.Bungee)
        {
            return SpawnType.None;
        }

        if (zombieType is ZombieType.Gravestone or ZombieType.Digger && Instances.GameplayActivity.Board.mPlantRow[boardUnitY.Grid] != PlantRowType.Pool)
        {
            if (zombieType == ZombieType.Gravestone && VersusState.ArenaSynced is ArenaType.Roof or ArenaType.RoofNight or ArenaType.China)
            {
                return SpawnType.FallFromSky;
            }

            return SpawnType.RiseFromGround;
        }

        var isDefault = ZombieRisesFromGround(zombieType);
        var isForceXPos = ZombieSpawnsInBack(zombieType);
        if (isDefault && !isForceXPos)
        {
            if (VersusState.ArenaSynced is ArenaType.Pool or ArenaType.PoolNight && boardUnitX.Grid < 9)
            {
                if (Instances.GameplayActivity.Board.IsPoolSquare(boardUnitX.Grid, boardUnitY.Grid))
                {
                    return SpawnType.RiseFromPool;
                }
            }

            return IArena.GetCurrentArena().DefaultZombieSpawnType;
        }
        else
        {
            return SpawnType.Background;
        }
    }

    /// <summary>
    /// Spawns a plant at the specified grid coordinates for debug purposes.
    /// </summary>
    /// <param name="seedType">The type of seed to spawn as a plant.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this plant.</param>
    /// <returns>A tuple containing the spawned <see cref="Plant"/> and optional <see cref="PlantNetworked"/> controller.</returns>
    internal static (Plant Plant, PlantNetworked? PlantNetworked) DebugSpawnPlant(SeedType seedType, int gridX, int gridY, bool spawnOnNetwork)
    {
        return SpawnPlant(seedType, gridX, gridY, SpawnType.None, spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates for debug purposes.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>A tuple containing the spawned <see cref="Zombie"/> and optional <see cref="ZombieNetworked"/> controller.</returns>
    internal static (Zombie Zombie, ZombieNetworked? ZombieNetworked) DebugSpawnZombie(ZombieType zombieType, int gridX, int gridY, bool spawnOnNetwork)
    {
        return SpawnZombie(zombieType, gridX, gridY, GetZombieSpawnType(zombieType, gridX, gridY), spawnOnNetwork);
    }

    /// <summary>
    /// Determines if a seed can be placed at the specified grid coordinates.
    /// </summary>
    /// <param name="seedType">The type of seed to place.</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <returns><see langword="true"/> if the seed can be placed at the specified location; otherwise, <see langword="false"/>.</returns>
    internal static bool CanPlace(SeedType seedType, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        if (VersusState.VersusPhase is not (VersusPhase.Gameplay or VersusPhase.SuddenDeath))
        {
            return false;
        }

        if (!ICharacterLogic.CanBePlacedAt(seedType, VersusState.ArenaSynced, boardUnitX, boardUnitY))
        {
            return false;
        }

        if (IArena.GetCurrentArena()?.CanBePlacedAt(seedType, boardUnitX, boardUnitY) == false)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a zombie type should rise from the ground when spawned.
    /// </summary>
    /// <param name="zombieType">The type of zombie to check.</param>
    /// <returns><see langword="true"/> if the zombie should rise from the ground; otherwise, <see langword="false"/>.</returns>
    internal static bool ZombieRisesFromGround(ZombieType zombieType)
    {
        if (zombieType is ZombieType.Bungee or ZombieType.Target or ZombieType.Bobsled or ZombieType.DolphinRider or ZombieType.Snorkel)
        {
            return false;
        }

        if (VersusMode.ZombieRisesFromGround(zombieType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a zombie type should spawn at the back of the lawn.
    /// </summary>
    /// <param name="zombieType">The type of zombie to check.</param>
    /// <returns><see langword="true"/> if the zombie should spawn at the back of the lawn; otherwise, <see langword="false"/>.</returns>
    internal static bool ZombieSpawnsInBack(ZombieType zombieType)
    {
        if (zombieType is ZombieType.Bobsled or ZombieType.Balloon or ZombieType.DolphinRider or ZombieType.Snorkel or ZombieType.Yeti)
        {
            return true;
        }

        if (!VersusMode.ZombieRisesFromGround(zombieType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets up Bungee rendering order based on the current arena.
    /// </summary>
    /// <param name="bungee">The Bungee zombie to configure.</param>
    internal static void SetBungeeRenderOrder(Zombie bungee)
    {
        if (bungee.mZombieType != ZombieType.Bungee)
            return;

        bungee.mBungeeTargetRenderOrder = bungee.RenderOrder;

        if (VersusState.ArenaSynced is ArenaType.Roof or ArenaType.RoofNight)
        {
            // Offset based off roof elevation 
            bungee.mImageOffsetY = Mathf.Lerp(80f, 0f, Mathf.Clamp01(bungee.mBoard.PixelToGridX(bungee.mPosX, bungee.mPosY) / 5f));
        }
    }

    /// <summary>
    /// Sets the Bungee's target SpriteRender active state.
    /// </summary>
    /// <param name="bungee">The Bungee zombie to configure.</param>
    /// <param name="active"><see langword="true"/> to show the target; <see langword="false"/> to hide it.</param>
    internal static void SetBungeeTarget(Zombie bungee, bool active)
    {
        if (bungee.mZombieType != ZombieType.Bungee)
            return;

        bungee.mController.Cast<ZombieBungeeController>().m_bungeeTargetSpriteRenderer.color = active ? Color.white : Color.white * 0f;
    }
}