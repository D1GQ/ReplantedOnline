using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Data.Asset;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Data;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;
using System.Reflection;
using UnityEngine;
using static Il2CppReloaded.Constants;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Modules.Versus;

/// <summary>
/// Handles seed packet definition modifications including versus costs and other properties.
/// </summary>
internal static class SeedPacketDefinitions
{
    /// <summary>
    /// The SeedType used to hide seed.
    /// </summary>
    internal readonly static SeedType HiddenSeed = SeedType.SlotMachineDiamond;

    /// <summary>
    /// Collection of seed types that are disabled and cannot be used in gameplay.
    /// </summary>
    internal readonly static SeedType[] DisabledSeedTypes = [
        // Misc
        SeedType.NumSeedsInChooser,
        SeedType.NumSeedTypes,
        SeedType.LastZombieIndex,
        SeedType.None,

        // Plants
        SeedType.Lilypad,
        SeedType.Tanglekelp,
        SeedType.Seashroom,
        SeedType.Blover,
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
        SeedType.Sunshroom,

        // Zombies
        SeedType.ZombieGravestone,
        SeedType.ZombieNormal,
        SeedType.ZombieTrashCan,

        HiddenSeed
    ];

    /// <summary>
    /// Collection of seed types that should be excluded from random selection pools.
    /// </summary>
    internal readonly static SeedType[] ExcludeFromRandomSeedTypes = [
        // Plants
        SeedType.Flowerpot,
        SeedType.Marigold,
        SeedType.Plantern,
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
    internal readonly static SeedType[] SleepingPlants = [
        SeedType.Puffshroom,
        SeedType.Seashroom,
        SeedType.Scaredyshroom,
        SeedType.Sunshroom,
        SeedType.Fumeshroom,
        SeedType.Magnetshroom,
        SeedType.Hypnoshroom,
        SeedType.Iceshroom,
        SeedType.Doomshroom
    ];

    /// <summary>
    /// A lookup of the original seed packet cost.
    /// </summary>
    internal static readonly Dictionary<SeedType, int> BaseSeedVersusCost = [];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// </summary>
    internal static void Initialize()
    {
        // Replace seed packet icon for hidden seed packet
        var slotMachineDiamondDef = Instances.DataServiceActivity.Service.GetPlantDefinition(HiddenSeed);
        var assetOverride = new AssetReferenceOverride<Sprite>(slotMachineDiamondDef.m_versusImage);
        IAssetReferenceOverride.Register(assetOverride);
        assetOverride.SetOverride(Assembly.GetExecutingAssembly().LoadSpriteFromResources("ReplantedOnline.Resources.Images.Icons.Hidden-Seed-Packet.png"));

        foreach (var seedDefinition in Instances.DataServiceActivity.Service.PlantDefinitions.EnumerateIl2CppReadonlyList())
        {
            BaseSeedVersusCost[seedDefinition.SeedType] = seedDefinition.VersusCost;
        }

        foreach (var zombieDefinition in Instances.DataServiceActivity.Service.ZombieDefinitions.EnumerateIl2CppReadonlyList())
        {
            // From Versus Mode Console:
            // Buff versus body health
            if (zombieDefinition.m_versusBodyHealth == 200)
            {
                zombieDefinition.m_versusBodyHealth = 270;
            }
        }
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
    internal static (Plant Plant, PlantNetworked PlantNetworked) SpawnPlant(SeedType seedType, int gridX, int gridY, bool spawnOnNetwork)
    {
        // Create the actual plant object in the game world using the original game method
        var plant = Instances.GameplayActivity.Board.AddPlant(gridX, gridY, seedType, SeedType.None);

        // Update position visually in countdown
        plant.UpdateInternal();

        // Only create network controller if network synchronization is requested
        // This prevents creating network objects in single-player mode
        PlantNetworked plantNetworked = null;
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this plant across all clients
            plantNetworked = SpawnPlantOnNetwork(plant, gridX, gridY);
        }

        Instances.GameplayActivity.Board.m_plants.NewArrayItem(plant, plant.DataID);

        ICharacterConfig.OnPlantPlanted(plant, gridX, gridY);

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
    internal static PlantNetworked SpawnPlantOnNetwork(Plant plant, int gridX, int gridY, Action<PlantNetworked> callback = null)
    {
        var networkObj = NetworkObject.SpawnNew<PlantNetworked>(net =>
        {
            net._Plant = plant;
            net.SeedType = plant.mSeedType;
            net.GridX = gridX;
            net.GridY = gridY;
            callback?.Invoke(net);
        }, VersusState.PlantClientId);
        return networkObj;
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates with optional rising animation.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="gridX">The X grid coordinate (column) or target column for Bungee zombies.</param>
    /// <param name="gridY">The Y grid coordinate (row) or target row for Bungee zombies.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>The spawned Zombie objects, or null if spawning was prevented.</returns>
    internal static (Zombie Zombie, ZombieNetworked ZombieNetworked) SpawnZombie(ZombieType zombieType, int gridX, int gridY, bool spawnOnNetwork)
    {
        return SpawnZombie(zombieType, gridX, gridY, IArena.GetCurrentArena().GetZombieSpawnType(zombieType), spawnOnNetwork);
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
    internal static (Zombie Zombie, ZombieNetworked ZombieNetworked) SpawnZombie(ZombieType zombieType, int gridX, int gridY, SpawnType spawnType, bool spawnOnNetwork)
    {
        // Add zombie to the board at the specified position
        // Use forced X position (9) for certain zombies, otherwise use the provided gridX
        var zombie = Instances.GameplayActivity.Board.AddZombieAtCell(zombieType, spawnType is SpawnType.Background or SpawnType.BackgroundAndShakeBushes ? 9 : gridX, gridY);

        bool canRise = !VersusState.IsInCountDown;
        // If this zombie rises from ground, trigger the rising animation
        // This makes the zombie emerge from the ground rather than just appearing
        if (spawnType == SpawnType.RiseFromGround)
        {
            if (canRise)
            {
                zombie.mZombiePhase = ZombiePhase.RisingFromGrave;
                zombie.mPhaseCounter = 150;
                Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_DIRT_RISE);
            }

            var theX = Instances.GameplayActivity.Board.GridToPixelX(gridX, gridY);
            var theY = Instances.GameplayActivity.Board.GridToPixelY(gridX, gridY);

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
        ZombieNetworked zombieNetworked = null;
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this zombie across all clients
            zombieNetworked = SpawnZombieOnNetwork(zombie, gridX, gridY, spawnType);
        }

        Instances.GameplayActivity.Board.m_zombies.NewArrayItem(zombie, zombie.DataID);

        ICharacterConfig.OnZombiePlanted(zombie, gridX, gridY);

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
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, int gridX, int gridY, Action<ZombieNetworked> callback = null)
    {
        return SpawnZombieOnNetwork(zombie, gridX, gridY, IArena.GetCurrentArena().GetZombieSpawnType(zombie.mZombieType), callback);
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
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, int gridX, int gridY, SpawnType spawnType, Action<ZombieNetworked> callback = null)
    {
        var networkObj = NetworkObject.SpawnNew<ZombieNetworked>(net =>
        {
            net._Zombie = zombie;
            net.ZombieType = zombie.mZombieType;
            net.SpawnType = spawnType;
            net.GridX = gridX;
            net.GridY = gridY;
            callback?.Invoke(net);
        }, VersusState.PlantClientId);
        return networkObj;
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

        if (!ICharacterConfig.CanBePlacedAt(seedType, VersusState.Arena, gridX, gridY))
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
        if (zombieType is ZombieType.Bungee or ZombieType.Target or ZombieType.Imp or ZombieType.Bobsled)
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
        if (zombieType is ZombieType.Bobsled)
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
        if (bungee.mZombieType != ZombieType.Bungee) return;

        bungee.mBungeeTargetRenderOrder = bungee.RenderOrder;

        if (VersusState.Arena is ArenaTypes.Roof or ArenaTypes.RoofNight)
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
        if (bungee.mZombieType != ZombieType.Bungee) return;

        bungee.mController.Cast<ZombieBungeeController>().m_bungeeTargetSpriteRenderer.color = active ? Color.white : Color.white * 0f;
    }
}