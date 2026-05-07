using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena behavior in versus gamemode.
/// </summary>
internal interface IArena
{
    /// <summary>
    /// Gets the type identifier for this arena.
    /// </summary>
    ArenaTypes Type { get; }

    /// <summary>
    /// Gets the default spawn type for zombies in this arena.
    /// </summary>
    SpawnType DefaultZombieSpawnType { get; }

    /// <summary>
    /// Called when the versus gameplay starts.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void InitializeArena(VersusMode versusMode);

    /// <summary>
    /// Called every frame during the versus game mode's active state.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void UpdateArena(VersusMode versusMode);

    /// <summary>
    /// Determines whether the seed type can be placed at the specified grid coordinates in the given arena.
    /// </summary>
    /// <param name="seedType">The seed type being attempted to be placed</param>
    /// <param name="gridX">The X grid coordinate (column)</param>
    /// <param name="gridY">The Y grid coordinate (row)</param>
    /// <returns>True if the seed type can be placed at the specified location; otherwise, false</returns>
    bool CanBePlacedAt(SeedType seedType, int gridX, int gridY);

    /// <summary>
    /// Determines the appropriate spawn type for a given zombie type based on its characteristics.
    /// </summary>
    /// <param name="zombieType">The type of zombie to evaluate.</param>
    /// <param name="gridX">The X grid coordinate.</param>
    /// <param name="gridY">The Y grid coordinate.</param>
    /// <returns>
    /// The spawn type for the zombie:
    /// </returns>
    SpawnType GetZombieSpawnType(ZombieType zombieType, int gridX, int gridY)
    {
        if (zombieType is ZombieType.Target or ZombieType.Bungee)
        {
            return SpawnType.None;
        }

        if (zombieType is ZombieType.Gravestone or ZombieType.Digger && Instances.GameplayActivity.Board.mPlantRow[gridY] != PlantRowType.Pool)
        {
            if (zombieType == ZombieType.Gravestone && Type is ArenaTypes.Roof or ArenaTypes.RoofNight or ArenaTypes.China)
            {
                return SpawnType.FallFromSky;
            }

            return SpawnType.RiseFromGround;
        }

        var isDefault = SeedPacketDefinitions.ZombieRisesFromGround(zombieType);
        var isForceXPos = SeedPacketDefinitions.ZombieSpawnsInBack(zombieType);
        if (isDefault && !isForceXPos)
        {
            if (VersusState.Arena is ArenaTypes.Pool or ArenaTypes.PoolNight)
            {
                if (Instances.GameplayActivity.Board.IsPoolSquare(gridX, gridY))
                {
                    return SpawnType.RiseFromPool;
                }
            }

            return DefaultZombieSpawnType;
        }
        else
        {
            return SpawnType.Background;
        }
    }

    /// <summary>
    /// Retrieves the current active arena instance.
    /// </summary>
    /// <returns>The currently active IArena implementation, or null if no matching arena is found</returns>
    internal static IArena GetCurrentArena()
    {
        foreach (var arena in RegisterArena.Instances)
        {
            if (arena.Type == VersusState.Arena)
            {
                return arena;
            }
        }

        return null;
    }
}