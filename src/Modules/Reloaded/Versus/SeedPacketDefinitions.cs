using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Data.Asset;
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
/// Handles seed packet definition modifications including versus costs and other properties.
/// </summary>
internal static class SeedPacketDefinitions
{
    /// <summary>
    /// The SeedType used to hide seed.
    /// </summary>
    internal readonly static SeedType RandomHiddenSeed = SeedType.SlotMachineDiamond;

    /// <summary>
    /// Collection of seed types that are not real seeds.
    /// </summary>
    internal readonly static SeedType[] NoneSeedTypes = [
        SeedType.NumSeedsInChooser,
        SeedType.NumSeedTypes,
        SeedType.LastZombieIndex,
        SeedType.None
    ];

    /// <summary>
    /// Collection of seed types that are disabled and cannot be used in gameplay.
    /// </summary>
    internal readonly static SeedType[] HideInChooserSeedTypes = [
        // Plants
        SeedType.Marigold
    ];

    /// <summary>
    /// Collection of seed types that ignore the initial cooldown period and are available immediately.
    /// </summary>
    internal readonly static SeedType[] IgnoreInitialCooldownSeedTypes = [
        // Plants
        SeedType.Sunflower,
        SeedType.Peashooter,
        SeedType.Potatomine,
        SeedType.Wallnut,
        SeedType.Tallnut,
        SeedType.Puffshroom,

        // Replanted Online
        SeedType.Cabbagepult,
        SeedType.Kernelpult,
        SeedType.Sunshroom,

        // Zombies
        SeedType.ZombieGravestone,
        SeedType.ZombieNormal,
        SeedType.ZombieTrashCan,

        RandomHiddenSeed
    ];

    /// <summary>
    /// Collection of seed types that produce currency, sun or brains.
    /// </summary>
    internal readonly static SeedType[] CurrencyProducingSeedTypes = [
        // Plants
        SeedType.Sunflower,
        SeedType.Sunshroom,

        // Zombies
        SeedType.ZombieGravestone
    ];

    /// <summary>
    /// Collection of seed types that sleep by default.
    /// </summary>
    internal readonly static SeedType[] SleepingPlants = [.. Enum.GetValues<SeedType>().Where(Plant.IsNocturnal)];

    /// <summary>
    /// A lookup of the original plant definitions containing base values for cost, refresh time, and sudden death refresh time.
    /// </summary>
    internal static readonly Dictionary<SeedType, (int Cost, int RefreshTime, int SuddenDeathRefreshTime)> BasePlantDefinitions = [];

    /// <summary>
    /// A lookup of the original zombie definitions containing base values for body health, armor health, and easter egg chance.
    /// </summary>
    internal static readonly Dictionary<ZombieType, (int BodyHealth, int ArmorHealth, float EasterEggChance100)> BaseZombieDefinitions = [];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// </summary>
    internal static void Initialize()
    {
        // Replace seed packet icon for hidden seed packet
        var slotMachineDiamondDef = Instances.IDataService.GetPlantDefinition(RandomHiddenSeed);
        var slotMachineDiamondAssetOverride = new AssetReferenceOverride<Sprite>(slotMachineDiamondDef.m_versusImage);
        IAssetReferenceOverride.Register(slotMachineDiamondAssetOverride);
        slotMachineDiamondAssetOverride.SetOverride(ReplantedOnlineMod.Assets.Sprites.SeedPacket.HiddenSeedPacketIcon, ReloadedLobby.AmInLobby);

        CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieSnorkel, "SNORKEL_ZOMBIE",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.SnorkelSeedPacketIcon);

        CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieDolphinRider, "DOLPHIN_RIDER_ZOMBIE",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.DolphinriderSeedPacketIcon);

        CustomPlantDefinition
            .CreateZombieSeedPacketDefinition(CustomSeedType.ZombieYeti, "ZOMBIE_YETI",
            ReplantedOnlineMod.Assets.Sprites.SeedPacket.YetiSeedPacketIcon,
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
    /// Updates plant and zombie definitions from the loaded configuration data.
    /// </summary>
    /// <param name="arenaType">The current arena type used to determine if nocturnal cost surplus should be applied.</param>
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
    /// Resets all plant and zombie definitions back to their base values.
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
    /// Sets the versus properties of a plant definition based on its base properties.
    /// </summary>
    /// <param name="seedType">The type of seed to set properties for.</param>
    /// <param name="baseRefreshTimeX">Multiplier for the base refresh time.</param>
    /// <returns>The updated plant definition.</returns>
    private static PlantDefinition SetVersusDefinitionFromBase(SeedType seedType, float baseRefreshTimeX = 1f)
    {
        var definition = Instances.IDataService.GetPlantDefinition(seedType);
        int baseRefreshTime = Mathf.FloorToInt(definition.RefreshTime * baseRefreshTimeX);
        definition.m_versusBaseRefreshTime = baseRefreshTime;
        definition.m_versusSuddenDeathRefreshTime = baseRefreshTime / 2;
        definition.m_versusCost = definition.SeedCost;
        return definition;
    }

    /// <summary>
    /// Places a seed on the board, handling both plant and zombie seeds appropriately.
    /// </summary>
    /// <param name="seedType">The type of seed to place.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this object.</param>
    /// <returns>The spawned ReloadedObject (either Plant or Zombie).</returns>
    internal static ReloadedObject PlaceSeed(SeedType seedType, int gridX, int gridY, bool spawnOnNetwork)
    {
        // Check if this is a zombie seed (from I, Zombie mode)
        // Zombie seeds have special handling since they spawn zombies instead of plants
        if (Challenge.IsZombieSeedType(seedType))
        {
            // Convert seed type to actual zombie type
            // Example: SeedType.SEED_ZOMBIE_NORMAL -> ZombieType.ZOMBIE_NORMAL
            var type = Challenge.IZombieSeedTypeToZombieType(seedType);

            // Delegate to zombie spawning logic
            return SpawnZombie(type, gridX, gridY, spawnOnNetwork).Zombie;
        }
        else
        {
            // This is a regular plant seed - delegate to plant spawning logic
            return SpawnPlant(seedType, gridX, gridY, spawnOnNetwork).Plant;
        }
    }

    /// <summary>
    /// Spawns a plant at the specified grid coordinates.
    /// </summary>
    /// <param name="seedType">The type of seed to spawn as a plant.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this plant.</param>
    /// <returns>The spawned Plant objects.</returns>
    internal static (Plant Plant, PlantNetworked? PlantNetworked) SpawnPlant(SeedType seedType, int gridX, int gridY, bool spawnOnNetwork)
    {
        return SpawnPlant(seedType, gridX, gridY, SpawnType.None, spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a plant at the specified grid coordinates.
    /// </summary>
    /// <param name="seedType">The type of seed to spawn as a plant.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnType">The type of spawning.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this plant.</param>
    /// <returns>The spawned Plant objects.</returns>
    internal static (Plant Plant, PlantNetworked? PlantNetworked) SpawnPlant(SeedType seedType, int gridX, int gridY, SpawnType spawnType, bool spawnOnNetwork)
    {
        // Create the actual plant object in the game world using the original game method
        var plant = Instances.GameplayActivity.Board.AddPlant(gridX, gridY, seedType, SeedType.None);

        // Update position visually in countdown
        plant.UpdateInternal();

        // Only create network controller if network synchronization is requested
        // This prevents creating network objects in single-player mode
        PlantNetworked? plantNetworked = null;
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this plant across all clients
            plantNetworked = SpawnPlantOnNetwork(plant, gridX, gridY, spawnType);
        }

        Instances.GameplayActivity.Board.m_plants.NewArrayItem(plant, plant.DataID);

        ICharacterLogic.OnPlantPlanted(plant, gridX, gridY);

        return (plant, plantNetworked);
    }

    /// <summary>
    /// Creates a networked controller for an existing plant to enable network synchronization.
    /// </summary>
    /// <param name="plant">The plant to create a network controller for.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned PlantNetworked controller object.</returns>
    internal static PlantNetworked SpawnPlantOnNetwork(Plant plant, int gridX, int gridY, Action<PlantNetworked>? callback = null)
    {
        return SpawnPlantOnNetwork(plant, gridX, gridY, SpawnType.None, callback);
    }

    /// <summary>
    /// Creates a networked controller for an existing plant to enable network synchronization.
    /// </summary>
    /// <param name="plant">The plant to create a network controller for.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnType">The type of spawning.</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned PlantNetworked controller object.</returns>
    internal static PlantNetworked SpawnPlantOnNetwork(Plant plant, int gridX, int gridY, SpawnType spawnType, Action<PlantNetworked>? callback = null)
    {
        var networkObj = NetworkObject.SpawnNew<PlantNetworked>(net =>
        {
            net._p.SetTarget(() => plant?.mController?.m_plant);
            net.SeedType = plant.mSeedType;
            net.SpawnType = spawnType;
            net.GridX = gridX;
            net.GridY = gridY;
            callback?.Invoke(net);
        }, VersusState.PlantClientId);
        return networkObj!;
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates with optional rising animation.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="gridX">The X grid coordinate (column) or target column for Bungee zombies.</param>
    /// <param name="gridY">The Y grid coordinate (row) or target row for Bungee zombies.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>The spawned Zombie objects, or null if spawning was prevented.</returns>
    internal static (Zombie Zombie, ZombieNetworked? ZombieNetworked) SpawnZombie(ZombieType zombieType, int gridX, int gridY, bool spawnOnNetwork)
    {
        int truegridX = gridX;
        if (zombieType == ZombieType.BackupDancer)
        {
            truegridX = PvZRUtils.ReloadedObjectXToGridX(gridX);
        }

        return SpawnZombie(zombieType, gridX, gridY, GetZombieSpawnType(zombieType, truegridX, gridY), spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates with optional rising animation.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="gridX">The X grid coordinate (column) or target column for Bungee zombies.</param>
    /// <param name="gridY">The Y grid coordinate (row) or target row for Bungee zombies.</param>
    /// <param name="spawnType">The type of spawning.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>The spawned Zombie objects, or null if spawning was prevented.</returns>
    internal static (Zombie Zombie, ZombieNetworked? ZombieNetworked) SpawnZombie(ZombieType zombieType, int gridX, int gridY, SpawnType spawnType, bool spawnOnNetwork)
    {
        // Add zombie to the board at the specified position
        bool spawnInBack = spawnType is SpawnType.Background or SpawnType.BackgroundAndShakeBushes || zombieType == ZombieType.BackupDancer;
        var zombie = Instances.GameplayActivity.Board.AddZombieAtCell(zombieType, spawnInBack ? 9 : gridX, gridY);

        bool canRise = !VersusState.IsInPreCountDown;
        var theX = Instances.GameplayActivity.Board.GridToPixelX(gridX, gridY);
        var theY = Instances.GameplayActivity.Board.GridToPixelY(gridX, gridY);

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
                        Instances.GameplayActivity.AddTodParticle(theX + 25, theY + 75, zombie.RenderOrder - 5, ParticleEffect.GraveStoneRise);
                    else
                        zombie.mPhaseCounter = 0;
                    zombie.mPosX = theX - 25;
                    break;
                case ZombieType.BackupDancer:
                    if (canRise) Instances.GameplayActivity.AddTodParticle(gridX + 55, theY + 75, zombie.RenderOrder - 5, ParticleEffect.ZombieRise);
                    zombie.mPosX = gridX;
                    break;
                default:
                    if (canRise) Instances.GameplayActivity.AddTodParticle(theX + 35, theY + 75, zombie.RenderOrder - 5, ParticleEffect.ZombieRise);
                    zombie.mPosX = theX - 25;
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
                case ZombieType.BackupDancer:
                    zombie.mPosX = gridX;
                    break;
                default:
                    zombie.mPosX = theX - 25;
                    break;
            }
        }
        else if (spawnType is SpawnType.BungeeDropZombie or SpawnType.BungeeDropZombieNoTarget)
        {
            switch (zombieType)
            {
                case ZombieType.BackupDancer:
                    zombie.mPosX = gridX;
                    break;
                default:
                    zombie.mPosX = theX - 25;
                    break;
            }
        }
        else if (spawnType == SpawnType.FallFromSky)
        {
            Animations.PlayFallFromSky(zombie, gridY);

            switch (zombieType)
            {
                case ZombieType.BackupDancer:
                    zombie.mPosX = gridX;
                    break;
                default:
                    zombie.mPosX = theX - 25;
                    break;
            }
        }
        else if (spawnType == SpawnType.BackgroundAndShakeBushes)
        {
            Instances.GameplayActivity.BackgroundController.ZombieSpawnedInRow(gridY);
        }

        // Set Gravestone grid pos
        if (zombieType == ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Add(zombie);
            zombie.mGraveX = gridX;
            zombie.mGraveY = gridY;
        }

        // Set Bungee grid target
        if (zombieType == ZombieType.Bungee)
        {
            SetBungeeTarget(zombie, true);
            zombie.mTargetCol = gridX;
            zombie.mTargetRow = gridY;
        }

        // Update position visually in countdown
        zombie.UpdateReanim();

        // Only create network controller if network synchronization is requested
        ZombieNetworked? zombieNetworked = null;
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this zombie across all clients
            zombieNetworked = SpawnZombieOnNetwork(zombie, gridX, gridY, spawnType);
        }

        Instances.GameplayActivity.Board.m_zombies.NewArrayItem(zombie, zombie.DataID);

        ICharacterLogic.OnZombiePlanted(zombie, gridX, gridY);

        return (zombie, zombieNetworked);
    }

    /// <summary>
    /// Creates a networked controller for an existing zombie to enable network synchronization.
    /// </summary>
    /// <param name="zombie">The zombie to create a network controller for.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned ZombieNetworked controller object.</returns>
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, int gridX, int gridY, Action<ZombieNetworked>? callback = null)
    {
        return SpawnZombieOnNetwork(zombie, gridX, gridY, GetZombieSpawnType(zombie.mZombieType, gridX, gridY), callback);
    }

    /// <summary>
    /// Creates a networked controller for an existing zombie to enable network synchronization.
    /// </summary>
    /// <param name="zombie">The zombie to create a network controller for.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnType">The type of spawning.</param>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <returns>The spawned ZombieNetworked controller object.</returns>
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, int gridX, int gridY, SpawnType spawnType, Action<ZombieNetworked>? callback = null)
    {
        var networkObj = NetworkObject.SpawnNew<ZombieNetworked>(net =>
        {
            net._z.SetTarget(() => zombie?.mController?.m_zombie);
            net.ZombieType = zombie.mZombieType;
            net.SpawnType = spawnType;
            net.GridX = gridX;
            net.GridY = gridY;
            callback?.Invoke(net);
        }, VersusState.PlantClientId);
        return networkObj!;
    }

    /// <summary>
    /// Determines the appropriate spawn type for a given zombie type based on its characteristics.
    /// </summary>
    /// <param name="zombieType">The type of zombie to evaluate.</param>
    /// <param name="gridX">The X grid coordinate.</param>
    /// <param name="gridY">The Y grid coordinate.</param>
    /// <returns>
    /// The spawn type for the zombie:
    /// </returns>
    internal static SpawnType GetZombieSpawnType(ZombieType zombieType, int gridX, int gridY)
    {
        if (zombieType is ZombieType.BackupDancer)
        {
            gridX = PvZRUtils.ReloadedObjectXToGridX(gridX);
        }

        if (zombieType is ZombieType.Target or ZombieType.Bungee)
        {
            return SpawnType.None;
        }

        if (zombieType is ZombieType.Gravestone or ZombieType.Digger && Instances.GameplayActivity.Board.mPlantRow[gridY] != PlantRowType.Pool)
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
            if (VersusState.ArenaSynced is ArenaType.Pool or ArenaType.PoolNight && gridX < 9)
            {
                if (Instances.GameplayActivity.Board.IsPoolSquare(gridX, gridY))
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
    /// Determines if a seed can be placed at the specified grid coordinates.
    /// </summary>
    /// <param name="seedType">The type of seed to place.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <returns>True if the seed can be placed at the specified location, false otherwise.</returns>
    internal static bool CanPlace(SeedType seedType, int gridX, int gridY)
    {
        if (VersusState.VersusPhase is not (VersusPhase.Gameplay or VersusPhase.SuddenDeath))
        {
            return false;
        }

        if (!ICharacterLogic.CanBePlacedAt(seedType, VersusState.ArenaSynced, gridX, gridY))
        {
            return false;
        }

        if (IArena.GetCurrentArena()?.CanBePlacedAt(seedType, gridX, gridY) == false)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a zombie type should rise from the ground when spawned.
    /// </summary>
    /// <param name="zombieType">The type of zombie to check.</param>
    /// <returns>True if the zombie should rise from the ground; false if it should spawn normally.</returns>
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
    /// Determines whether a zombie type should spawn at the back of the lawn
    /// </summary>
    /// <param name="zombieType">The type of zombie to check.</param>
    /// <returns>True if the zombie should spawn at the back of the lawn; false otherwise.</returns>
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
    /// Sets up Bungee rendering.
    /// </summary>
    /// <param name="bungee">The Bungee zombie.</param>
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
    /// Sets the Bungees target SpriteRender active.
    /// </summary>
    /// <param name="bungee">The Bungee zombie.</param>
    /// <param name="active">If it should be visible.</param>
    internal static void SetBungeeTarget(Zombie bungee, bool active)
    {
        if (bungee.mZombieType != ZombieType.Bungee)
            return;

        bungee.mController.Cast<ZombieBungeeController>().m_bungeeTargetSpriteRenderer.color = active ? Color.white : Color.white * 0f;
    }
}