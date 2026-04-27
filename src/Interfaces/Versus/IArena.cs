using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Versus;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena-specific behavior in versus gamemode.
/// Implementations provide custom logic for different arena types.
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
    /// Gets the level entry data for this arena.
    /// </summary>
    /// <returns>The level entry data.</returns>
    LevelEntryData GetLevelEntryData();

    /// <summary>
    /// Sets up the versus arena for gameplay with the specified level data.
    /// </summary>
    /// <param name="versusLevelData">The level data to configure the arena with.</param>
    void SetupVersusArenaForGameplay(LevelEntryData versusLevelData);

    /// <summary>
    /// Called when the versus gameplay starts.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void InitializeArena(VersusMode versusMode);

    /// <summary>
    /// Called when seed packet cooldowns need to be initialized.
    /// </summary>
    /// <param name="seedPackets">The array of all seedpackets in play.</param>
    void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets);

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
    /// <returns>
    /// The spawn type for the zombie:
    /// <list type="bullet">
    /// <item><description><see cref="SpawnType.None"/> for Target or Bungee zombies (cannot spawn)</description></item>
    /// <item><description>The arena's <see cref="DefaultZombieSpawnType"/> for zombies that rise from the ground and don't force back spawn</description></item>
    /// <item><description><see cref="SpawnType.Back"/> for all other cases</description></item>
    /// </list>
    /// </returns>
    SpawnType GetZombieSpawnType(ZombieType zombieType)
    {
        if (zombieType is ZombieType.Target or ZombieType.Bungee)
        {
            return SpawnType.None;
        }

        var isDefault = SeedPacketDefinitions.ZombieRisesFromGround(zombieType);
        var isForceXPos = SeedPacketDefinitions.ZombieSpawnsInBack(zombieType);
        if (isDefault && !isForceXPos)
        {
            return DefaultZombieSpawnType;
        }
        else
        {
            return SpawnType.Back;
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