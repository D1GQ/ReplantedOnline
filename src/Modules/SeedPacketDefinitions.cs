using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Utilities;
using static Il2CppReloaded.Constants;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Modules;

/// <summary>
/// Handles seed packet definition modifications including versus costs and other properties.
/// </summary>
internal static class SeedPacketDefinitions
{
    /// <summary>
    /// Collection of seed types that are disabled and cannot be used in gameplay.
    /// Includes miscellaneous seeds, invalid seeds, and specific plants that are disabled.
    /// </summary>
    internal static SeedType[] DisabledSeedTypes = [
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
    /// Includes basic plants and zombies that should be immediately available.
    /// </summary>
    internal static SeedType[] IgnoreInitialCooldown = [
        // Plants
        SeedType.Sunflower,
        SeedType.Peashooter,
        SeedType.Potatomine,
        SeedType.Wallnut,
        SeedType.GiantWallnut,
        SeedType.Puffshroom,

        // Zombies
        SeedType.ZombieGravestone,
        SeedType.ZombieNormal,
        SeedType.ZombieTrashCan
    ];

    /// <summary>
    /// Collection of seed types that should be excluded from random selection pools.
    /// These seeds are typically special plants or zombies that shouldn't appear randomly.
    /// </summary>
    internal static SeedType[] ExcludeFromRandom = [
        // Plants
        SeedType.Sunflower,
        SeedType.Flowerpot,
        SeedType.Marigold,
        SeedType.Plantern,

        // Zombies
        SeedType.ZombieGravestone
    ];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// Currently sets custom versus mode costs for specific seeds.
    /// </summary>
    internal static void Initialize()
    {
        Instances.DataServiceActivity.Service.GetPlantDefinition(SeedType.ZombieFlag).m_versusCost = 275;
    }

    /// <summary>
    /// Determines if a seed can be placed at the specified grid coordinates.
    /// Checks board placement rules, versus game phase, and special zombie placement restrictions.
    /// </summary>
    /// <param name="seedType">The type of seed to place.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <returns>True if the seed can be placed at the specified location, false otherwise.</returns>
    internal static bool CanPlace(SeedType seedType, int gridX, int gridY)
    {
        // Check if placing a Dancer zombie - they cannot be placed in top or bottom rows (0 and 4)
        var checkDancerGrid = seedType != SeedType.ZombieDancer || gridY != 0 && gridY != 4;

        return Instances.GameplayActivity.Board.CanPlantAt(gridX, gridY, seedType) == PlantingReason.Ok
            && VersusState.VersusPhase is VersusPhase.Gameplay or VersusPhase.SuddenDeath
            && checkDancerGrid;
    }

    /// <summary>
    /// Places a seed on the board, handling both plant and zombie seeds appropriately.
    /// Automatically routes to plant spawning for regular seeds and zombie spawning for I, Zombie mode seeds.
    /// </summary>
    /// <param name="seedType">The type of seed to place.</param>
    /// <param name="imitaterType">The imitater type for plants (for imitated plants).</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this object.</param>
    /// <returns>The spawned ReloadedObject (either Plant or Zombie).</returns>
    internal static ReloadedObject PlaceSeed(SeedType seedType, SeedType imitaterType, int gridX, int gridY, bool spawnOnNetwork)
    {
        // Check if this is a zombie seed (from I, Zombie mode)
        // Zombie seeds have special handling since they spawn zombies instead of plants
        if (Challenge.IsZombieSeedType(seedType))
        {
            // Convert seed type to actual zombie type
            // Example: SeedType.SEED_ZOMBIE_NORMAL -> ZombieType.ZOMBIE_NORMAL
            var type = Challenge.IZombieSeedTypeToZombieType(seedType);

            // Delegate to zombie spawning logic
            return SpawnZombie(type, gridX, gridY, false, spawnOnNetwork);
        }
        else
        {
            // This is a regular plant seed - delegate to plant spawning logic
            return SpawnPlant(seedType, imitaterType, gridX, gridY, spawnOnNetwork);
        }
    }

    /// <summary>
    /// Spawns a plant at the specified grid coordinates.
    /// Creates the actual plant object and optionally sets up network synchronization.
    /// </summary>
    /// <param name="seedType">The type of seed to spawn as a plant.</param>
    /// <param name="imitaterType">The imitater type for imitated plants.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this plant.</param>
    /// <returns>The spawned Plant object.</returns>
    internal static Plant SpawnPlant(SeedType seedType, SeedType imitaterType, int gridX, int gridY, bool spawnOnNetwork)
    {
        // Create the actual plant object in the game world using the original game method
        var plant = Instances.GameplayActivity.Board.AddPlant(gridX, gridY, seedType, imitaterType);

        // Only create network controller if network synchronization is requested
        // This prevents creating network objects in single-player mode
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this plant across all clients
            SpawnPlantOnNetwork(plant, gridX, gridY);
        }

        Instances.GameplayActivity.Board.m_plants.NewArrayItem(plant, plant.DataID);

        return plant;
    }

    /// <summary>
    /// Creates a networked controller for an existing plant to enable network synchronization.
    /// Sets up all necessary network properties and initializes the animation controller.
    /// </summary>
    /// <param name="plant">The plant to create a network controller for.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <returns>The spawned PlantNetworked controller object.</returns>
    internal static PlantNetworked SpawnPlantOnNetwork(Plant plant, int gridX, int gridY)
    {
        var networkObj = NetworkObject.SpawnNew<PlantNetworked>(net =>
        {
            net._Plant = plant;
            net.SeedType = plant.mSeedType;
            net.ImitaterType = plant.mImitaterType;
            net.GridX = gridX;
            net.GridY = gridY;
        }, VersusState.PlantClientId);
        plant.AddNetworkedLookup(networkObj);
        networkObj.AnimationControllerNetworked.Init(plant.mController.AnimationController);
        return networkObj;
    }

    /// <summary>
    /// Spawns a zombie at the specified grid coordinates with optional rising animation.
    /// Handles special zombie types like Gravestones and Bungee zombies with custom positioning.
    /// </summary>
    /// <param name="zombieType">The type of zombie to spawn.</param>
    /// <param name="gridX">The X grid coordinate (column) or target column for Bungee zombies.</param>
    /// <param name="gridY">The Y grid coordinate (row) or target row for Bungee zombies.</param>
    /// <param name="shakeBush">Whether to trigger bush shaking animation on spawn.</param>
    /// <param name="spawnOnNetwork">Whether to create network synchronization for this zombie.</param>
    /// <returns>The spawned Zombie object, or null if spawning was prevented.</returns>
    internal static Zombie SpawnZombie(ZombieType zombieType, int gridX, int gridY, bool shakeBush, bool spawnOnNetwork)
    {
        // Determine if this zombie type rises from the ground (like grave zombies)
        // Bungee zombies are excluded from rising behavior even if they normally would
        var rise = ZombieRisesFromGround(zombieType);

        // Some zombies have forced spawn positions on the right side
        var forceXPos = ZombieSpawnsInBack(zombieType);

        // Add zombie to the board at the specified position
        // Use forced X position (9) for certain zombies, otherwise use the provided gridX
        var zombie = Instances.GameplayActivity.Board.AddZombieAtCell(zombieType, forceXPos ? 9 : gridX, gridY);

        // If this zombie rises from ground, trigger the rising animation
        // This makes the zombie emerge from the ground rather than just appearing
        if (rise && !shakeBush)
        {
            zombie.mZombiePhase = ZombiePhase.RisingFromGrave;
            zombie.mPhaseCounter = 150;
            Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_DIRT_RISE);
            var theX = Instances.GameplayActivity.Board.GridToPixelX(gridX, gridY);
            var theY = Instances.GameplayActivity.Board.GridToPixelY(gridX, gridY);
            switch (zombieType)
            {
                case ZombieType.Gravestone:
                    Instances.GameplayActivity.AddTodParticle(theX + 25, theY + 75, zombie.RenderOrder - 5, ParticleEffect.GraveStoneRise);
                    zombie.mPosX = theX - 25;
                    break;
                case ZombieType.BackupDancer:
                    Instances.GameplayActivity.AddTodParticle(gridX + 55, theY + 75, zombie.RenderOrder - 5, ParticleEffect.ZombieRise);
                    zombie.mPosX = gridX;
                    break;
                default:
                    Instances.GameplayActivity.AddTodParticle(theX + 35, theY + 75, zombie.RenderOrder - 5, ParticleEffect.ZombieRise);
                    zombie.mPosX = theX - 25;
                    break;
            }
        }
        else if (shakeBush)
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
            zombie.mTargetCol = gridX;
            zombie.mTargetRow = gridY;
        }

        // Only create network controller if network synchronization is requested
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this zombie across all clients
            SpawnZombieOnNetwork(zombie, gridX, gridY, shakeBush);
        }

        // Fix rendering issues
        if (zombieType is ZombieType.Gravestone)
        {
            zombie.RenderOrder -= 100 + 5 * (gridY + 1);
        }
        else if (zombieType is ZombieType.Target)
        {
            zombie.RenderOrder -= 200 + 10 * (gridY + 1);
        }

        Instances.GameplayActivity.Board.m_zombies.NewArrayItem(zombie, zombie.DataID);

        return zombie;
    }

    /// <summary>
    /// Creates a networked controller for an existing zombie to enable network synchronization.
    /// Sets up all necessary network properties and initializes the animation controller.
    /// </summary>
    /// <param name="zombie">The zombie to create a network controller for.</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    /// <param name="shakeBush">Whether bush shaking animation was triggered on spawn.</param>
    /// <returns>The spawned ZombieNetworked controller object.</returns>
    internal static ZombieNetworked SpawnZombieOnNetwork(Zombie zombie, int gridX, int gridY, bool shakeBush)
    {
        var networkObj = NetworkObject.SpawnNew<ZombieNetworked>(net =>
        {
            net._Zombie = zombie;
            net.ZombieType = zombie.mZombieType;
            net.ShakeBush = shakeBush;
            net.GridX = gridX;
            net.GridY = gridY;
        }, VersusState.PlantClientId);
        zombie.AddNetworkedLookup(networkObj);
        networkObj.AnimationControllerNetworked.Init(zombie.mController.AnimationController);
        return networkObj;
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
}